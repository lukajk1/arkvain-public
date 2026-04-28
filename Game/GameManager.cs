using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // this will simply house the code for restarting and leaving, to keep it out of other classes.

    [SerializeField] private SceneNameHolder mainMenuScene;

    public static GameManager Instance { get; private set; }
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
        if (PersistentClient.Instance != null) 
            PersistentClient.Instance.inputManager.Actions.Player.Enable();
    }

    public void LeaveMatch()
    {
        // remove monitor because it would otherwise give false positives
        if (ConnectionMonitor.Instance != null)
            Destroy(ConnectionMonitor.Instance.gameObject);

        // Leave Steam lobby if we are in one
        if (ArkvainLobbyData.HasValidLobby())
        {
            Debug.Log("[EscapeMenu] Leaving Steam lobby...");
            ArkvainLobbyData.CurrentLobby.Leave();
            ArkvainLobbyData.Clear();
        }

        // Return to the main menu/lobby scene
        if (mainMenuScene != null)
        {
            if (LoadingManager.Instance != null)
            {
                LoadingManager.Instance.LoadScene(mainMenuScene.sceneName);
            }
            else
            {
                SceneManager.LoadScene(mainMenuScene.sceneName);
            }
        }
        else
        {
            Debug.LogError("[EscapeMenu] _lobbyScene is not assigned!");
        }
    }

    public void RestartGame()
    {
        if (MatchSessionManager.Instance == null) return;

        string map = MatchSessionManager.Instance.MapName;
        Debug.Log($"[MatchEndedState] Authoritative restart detected. Reloading map: {map}");

        if (LoadingManager.Instance != null)
        {
            LoadingManager.Instance.LoadGame(PersistentClient.Instance.gameScene.sceneName, map);
        }
    }
}
