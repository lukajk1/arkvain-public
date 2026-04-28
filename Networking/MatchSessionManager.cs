using PurrNet;
using PurrNet.Pooling;
using PurrNet.Prediction;
using QFSW.QC;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum PlayerStatus
{
    Alive,
    Dead,
    Spectating
}

public struct KillInfo
{
    public PlayerID killer;
    public PlayerID victim;
    public bool isLocalPlayerKiller;
    public bool isLocalPlayerVictim;
    public DamageResult damageResult;
    public Vector3 victimPosition;
}

/// <summary>
/// A tick-aligned, predicted manager for match session state.
/// Tracks scores, player status, and broadcasts match-critical events.
/// </summary>
public class MatchSessionManager : PredictedIdentity<MatchSessionManager.MatchState>
{
    public static MatchSessionManager Instance { get; private set; }

    [Header("Registry")]
    [SerializeField] private GameRegistry _gameRegistry;

    // Visual-only lookup for strings (not networked in state)
    private readonly Dictionary<PlayerID, string> _playerNames = new();

    // Events for UI updates (Local only)
    public event Action<PlayerMatchState> OnPlayerStatsChanged;
    public event Action<PlayerID> OnPlayerJoined;
    public event Action<PlayerID> OnPlayerLeft;

    public static bool MatchStarted;

    // Tick-aligned Death Event
    [HideInInspector] public PredictedEvent<KillInfo> OnPlayerKilled;

    // Static registry for timing-independent subscription
    private static readonly List<Action<KillInfo>> _pendingKilledListeners = new();

    public static void RegisterKilledListener(Action<KillInfo> listener)
    {
        if (Instance != null && Instance.OnPlayerKilled != null)
        {
            Instance.OnPlayerKilled.AddListener(listener);
        }
        else
        {
            if (!_pendingKilledListeners.Contains(listener))
                _pendingKilledListeners.Add(listener);
        }
    }

    public static void UnregisterKilledListener(Action<KillInfo> listener)
    {
        if (Instance != null && Instance.OnPlayerKilled != null)
        {
            Instance.OnPlayerKilled.RemoveListener(listener);
        }
        _pendingKilledListeners.Remove(listener);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    protected override void LateAwake()
    {
        base.LateAwake();

        OnPlayerKilled = new PredictedEvent<KillInfo>(predictionManager, this);

        // Apply all pending listeners
        foreach (var listener in _pendingKilledListeners)
        {
            OnPlayerKilled.AddListener(listener);
        }
        _pendingKilledListeners.Clear();
    }

    protected override void Simulate(ref MatchState state, float delta)
    {
        if (!predictionManager.isServer) return;

        // Sync local player list with PredictionManager's official list
        var currentPlayers = predictionManager.players.currentState.players;
        
        // Add new players to our tracking list
        for (int i = 0; i < currentPlayers.Count; i++)
        {
            PlayerID pid = currentPlayers[i];
            if (!HasPlayer(ref state, pid))
            {
                state.players.Add(new PlayerMatchState { playerId = pid, status = PlayerStatus.Spectating });
            }
        }
    }

    private bool HasPlayer(ref MatchState state, PlayerID pid)
    {
        for (int i = 0; i < state.players.Count; i++)
        {
            if (state.players[i].playerId == pid) return true;
        }
        return false;
    }

    public void ReportKill(PlayerID killer, PlayerID victim, DamageResult damageResult, Vector3 victimPosition)
    {
        // Compute local player context flags ONCE
        PlayerID? localPlayer = predictionManager.localPlayer;
        bool isLocalKiller = (killer == localPlayer);
        bool isLocalVictim = (victim == localPlayer);

        ref var state = ref currentState;

        for (int i = 0; i < state.players.Count; i++)
        {
            var p = state.players[i];
            if (p.playerId == killer)
            {
                p.kills++;
                state.players[i] = p;
            }
            if (p.playerId == victim)
            {
                p.deaths++;
                p.status = PlayerStatus.Dead;
                state.players[i] = p;
            }
        }

        OnPlayerKilled.Invoke(new KillInfo
        {
            killer = killer,
            victim = victim,
            isLocalPlayerKiller = isLocalKiller,
            isLocalPlayerVictim = isLocalVictim,
            damageResult = damageResult,
            victimPosition = victimPosition
        });
    }

    public void UpdatePlayerStatus(PlayerID playerId, PlayerStatus status)
    {
        if (!predictionManager.isServer) return;

        ref var state = ref currentState;
        for (int i = 0; i < state.players.Count; i++)
        {
            if (state.players[i].playerId == playerId)
            {
                var p = state.players[i];
                p.status = status;
                state.players[i] = p;
                break;
            }
        }
    }

    public void UpdatePlayerName(PlayerID id, string name)
    {
        _playerNames[id] = name;
    }

    public void UpdatePlayerSteamInfo(PlayerID playerId, ulong steamId, string steamName)
    {
        UpdatePlayerName(playerId, steamName);
        Debug.Log($"[MatchSessionManager] Updated Steam info for {playerId}: {steamName} ({steamId})");
    }

    public string GetPlayerName(PlayerID id)
    {
        if (_playerNames.TryGetValue(id, out var name)) return name;
        return $"Player {id}";
    }

    public List<PlayerMatchState> GetAllPlayers()
    {
        return currentState.players.list.ToList();
    }

    public PlayerMatchState? GetPlayerData(PlayerID id)
    {
        foreach (var p in currentState.players.list)
        {
            if (p.playerId == id) return p;
        }
        return null;
    }

    public List<PlayerMatchState> GetLeaderboard()
    {
        return currentState.players.list
            .OrderByDescending(p => CalculateScore(p))
            .ToList();
    }

    public static float CalculateScore(PlayerMatchState p)
    {
        return (p.kills * 10f) + (p.assists * 3f) - (p.deaths * 5f);
    }

    public static float GetKDA(PlayerMatchState p)
    {
        if (p.deaths == 0) return p.kills + p.assists;
        return (p.kills + p.assists) / (float)p.deaths;
    }

    public void RequestEndMatch()
    {
        if (!predictionManager.isServer) return;

        var sm = FindObjectOfType<PurrNet.Prediction.StateMachine.PredictedStateMachine>();
        if (sm != null)
        {
            var endState = sm.GetComponent<MatchEndedState>();
            if (endState != null) sm.SetState(endState);
            else sm.Next();
        }
    }

    public string MapName
    {
        get
        {
            if (_gameRegistry == null) return "Unknown";

            var allowedMaps = _gameRegistry.GetAllowedMaps(currentState.gameMode);
            int mapIdx = currentState.mapIndex;

            if (allowedMaps != null && mapIdx >= 0 && mapIdx < allowedMaps.Count)
            {
                return allowedMaps[mapIdx].InternalName;
            }
            return "Unknown";
        }
    }

    public string ModeName => currentState.gameMode.ToDisplayString();

    public int RestartCount => currentState.restartCount;

    public void SetMapAndMode(string map, string mode)
    {
        if (!predictionManager.isServer || _gameRegistry == null) return;

        var parsedMode = GameModeExtensions.FromString(mode);
        if (!parsedMode.HasValue)
        {
            Debug.LogError($"[MatchSessionManager] Failed to parse game mode: {mode}");
            return;
        }

        currentState.gameMode = parsedMode.Value;

        // Find the map index within this mode's allowed maps
        var allowedMaps = _gameRegistry.GetAllowedMaps(parsedMode.Value);
        int mapIdx = -1;

        for (int i = 0; i < allowedMaps.Count; i++)
        {
            if (allowedMaps[i].InternalName == map)
            {
                mapIdx = i;
                break;
            }
        }

        currentState.mapIndex = mapIdx;
        Debug.Log($"[MatchSessionManager] Authoritative state set - Mode: {mode}, Map: {map} (index: {mapIdx})");
    }
    [Command("hide-names")]
    public void AssignNumberedNames()
    {
        int number = 1;
        foreach (var p in currentState.players.list)
        {
            _playerNames[p.playerId] = $"PlayerTest {number}";
            number++;
        }
        Debug.Log("hid playernames");
    }

    protected override MatchState GetInitialState()
    {
        return new MatchState
        {
            players = DisposableList<PlayerMatchState>.Create(16),
            matchTimer = 0f,
            restartCount = 0,
            gameMode = GameMode.FFA,
            mapIndex = -1
        };
    }

    public struct PlayerMatchState : IPredictedData<PlayerMatchState>
    {
        public PlayerID playerId;
        public int kills;
        public int deaths;
        public int assists;
        public PlayerStatus status;

        public void Dispose() { }
    }

    public struct MatchState : IPredictedData<MatchState>
    {
        public DisposableList<PlayerMatchState> players;
        public float matchTimer;
        public GameMode gameMode;
        public int mapIndex;
        public int restartCount;

        public void Dispose()
        {
            if (players.list != null) players.Dispose();
        }
    }
}
