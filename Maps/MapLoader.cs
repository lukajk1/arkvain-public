using PurrNet;
using PurrNet.Prediction;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MapLoader : StatelessPredictedIdentity
{
    public static MapLoader Instance { get; private set; }

    [Header("Registry")]
    [SerializeField] private GameRegistry gameRegistry;

    [Header("Settings")]
    [SerializeField] private bool loadOnStart = true;

    public event Action<MapData> OnMapLoaded;
    public MapData CurrentMapData { get; private set; }
    public bool IsLoading { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (loadOnStart)
        {
            LoadMapFromLobby();
        }
    }

    public void LoadMapFromLobby()
    {
        if (!ArkvainLobbyData.HasValidLobby())
        {
            Debug.LogWarning("[MapLoader] No valid lobby found in ArkvainLobbyData! Cannot load map.");
            return;
        }

        string mapInternalName = ArkvainLobbyData.CurrentLobby["map"];
        string gameModeName = ArkvainLobbyData.CurrentLobby["game_mode"];
        
        if (string.IsNullOrEmpty(mapInternalName))
        {
            Debug.LogWarning("[MapLoader] No map name found in lobby metadata!");
            return;
        }

        LoadMapAndMode(mapInternalName, gameModeName);
    }

    public void LoadMapAndMode(string mapInternalName, string gameModeName)
    {
        if (gameRegistry == null)
        {
            Debug.LogError("[MapLoader] GameRegistry is not assigned!");
            return;
        }

        MapDefinition mapDef = gameRegistry.FindMapByInternalName(mapInternalName);
        GameObject gameModeLogicPrefab = gameRegistry.GetGameModeLogicPrefab(gameModeName);

        if (mapDef == null)
        {
            Debug.LogError($"[MapLoader] Map '{mapInternalName}' not found in registry!");
            return;
        }

        if (IsLoading)
        {
            Debug.LogWarning("[MapLoader] Already loading!");
            return;
        }

        StartCoroutine(LoadMapAndModeRoutine(mapDef, gameModeName, gameModeLogicPrefab));
    }

    private IEnumerator LoadMapAndModeRoutine(MapDefinition mapDef, string gameModeName, GameObject gameModeLogicPrefab)
    {
        IsLoading = true;
        Debug.Log($"[MapLoader] Starting load of map: {mapDef.displayName} and mode: {gameModeName}");

        // Wait for hierarchy to be initialized (in case we're a late-joining client)
        float timeoutCounter = 0f;
        const float timeout = 10f; // 10 second timeout
        while (hierarchy == null && timeoutCounter < timeout)
        {
            yield return null;
            timeoutCounter += Time.deltaTime;
        }

        if (hierarchy == null)
        {
            Debug.LogError("[MapLoader] Timeout waiting for hierarchy to initialize! Cannot spawn game mode logic.");
        }

        // 1. Unload old map if exists
        if (CurrentMapData != null)
        {
            Scene currentScene = CurrentMapData.gameObject.scene;
            AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(currentScene);
            while (!unloadOp.isDone) yield return null;
            CurrentMapData = null;
        }

        // 2. Load the new map scene additively
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(mapDef.SceneName, LoadSceneMode.Additive);
        while (!loadOp.isDone) yield return null;

        // 3. Find MapData and apply lighting
        Scene loadedScene = SceneManager.GetSceneByName(mapDef.SceneName);
        if (loadedScene.IsValid())
        {
            GameObject[] rootObjects = loadedScene.GetRootGameObjects();
            foreach (var obj in rootObjects)
            {
                MapData data = obj.GetComponentInChildren<MapData>();
                if (data != null)
                {
                    CurrentMapData = data;

                    // Apply map lighting settings
                    data.ApplyLighting();

                    break;
                }
            }
        }

        if (CurrentMapData == null)
        {
            Debug.LogError($"[MapLoader] Could not find MapData component in scene: {mapDef.SceneName}");
        }
        else
        {
            Debug.Log($"[MapLoader] Map and Mode loaded successfully.");
            OnMapLoaded?.Invoke(CurrentMapData);
        }

        IsLoading = false;

        if (hierarchy != null)
        {
            if (gameModeLogicPrefab != null)
            {
                Debug.Log($"[MapLoader] Spawning Game Mode Logic: {gameModeName}");

                // Use Instantiate - spawns in default PurrNet scene
                hierarchy.Create(gameModeLogicPrefab);
            }
            else
            {
                Debug.LogWarning("[MapLoader] No GameModeLogic prefab found for this mode!");
            }
        }
    }

    public void LoadMap(string internalName)
    {
        // Keep for backward compatibility or direct calls
        string gameModeName = ArkvainLobbyData.HasValidLobby() ? ArkvainLobbyData.CurrentLobby["game_mode"] : "";
        LoadMapAndMode(internalName, gameModeName);
    }
}
