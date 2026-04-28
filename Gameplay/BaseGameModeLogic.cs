using PurrNet;
using UnityEngine;

public abstract class BaseGameModeLogic : MonoBehaviour
{
    public static BaseGameModeLogic Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] protected int minPlayersToStart = 2;
    public int MinPlayersToStart => minPlayersToStart;

    protected virtual void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Called when the match transition from Waiting/Spawning to Running.
    /// </summary>
    public virtual void OnMatchStarted() 
    {
    }

    /// <summary>
    /// Called by MatchSessionManager when a kill is confirmed.
    /// </summary>
    //public abstract void OnPlayerKilled(PlayerID killer, PlayerID victim);

    /// <summary>
    /// Called when the match transition to the End state.
    /// </summary>
    public virtual void OnMatchEnded() 
    {
    }
}
