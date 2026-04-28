using PurrNet;
using PurrNet.Transports;
using UnityEngine;

/// <summary>
/// Monitors network connection state and displays notices when connections are lost.
/// Lives in networked scenes and cleans up automatically when scene unloads.
/// </summary>
public class ConnectionMonitor : MonoBehaviour
{
    private bool _isQuitting = false;

    public static ConnectionMonitor Instance { get; private set; }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            Debug.LogError("multiple copies of connectionmonitor in scene");
        }
    }

    private void Start()
    {
        if (NetworkManager.main != null)
        {
            NetworkManager.main.onClientConnectionState += OnClientConnectionState;
            NetworkManager.main.onServerConnectionState += OnServerConnectionState;
            Debug.Log("[ConnectionMonitor] Subscribed to network connection events");
        }
        else
        {
            Debug.LogError("[ConnectionMonitor] NetworkManager.main is null!");
        }
    }

    private void OnDisable()
    {
        if (NetworkManager.main != null)
        {
            NetworkManager.main.onClientConnectionState -= OnClientConnectionState;
            NetworkManager.main.onServerConnectionState -= OnServerConnectionState;
            Debug.Log("[ConnectionMonitor] Unsubscribed from network connection events");
        }
    }

    private void OnApplicationQuit()
    {
        _isQuitting = true;
    }

    private void OnDestroy()
    {
        // Already unsubscribed in OnDisable
    }

    private void OnClientConnectionState(ConnectionState state)
    {
        Debug.Log($"[ConnectionMonitor] Client connection state changed: {state}");

        if (state == ConnectionState.Disconnected && !_isQuitting && Application.isPlaying)
        {
            ShowConnectionLostNotice("Lost Connection", "You have been disconnected from the server.");
        }
    }

    private void OnServerConnectionState(ConnectionState state)
    {
        Debug.Log($"[ConnectionMonitor] Server connection state changed: {state}");

        if (state == ConnectionState.Disconnected && !_isQuitting && Application.isPlaying)
        {
            ShowConnectionLostNotice("Server Disconnected", "The server has shut down.");
        }
    }

    private void ShowConnectionLostNotice(string title, string message)
    {
        if (PersistentClient.Instance == null)
        {
            Debug.LogError("[ConnectionMonitor] PersistentClient.Instance is null!");
            return;
        }

        PersistentClient.Instance.CreateConfirmationDialog(
            onConfirm: () => ReturnToMainMenu(),
            onCancel: null,
            message: message,
            confirmText: "Return to Main Menu",
            cancelText: "",
            hideCancelButton: true
        );
    }

    private void ReturnToMainMenu()
    {
        if (PersistentClient.Instance == null || PersistentClient.Instance.mainMenuScene == null)
        {
            Debug.LogError("[ConnectionMonitor] Cannot return to main menu - PersistentClient or mainMenuScene is null!");
            return;
        }

        if (LoadingManager.Instance != null)
        {
            LoadingManager.Instance.LoadScene(PersistentClient.Instance.mainMenuScene.sceneName);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(PersistentClient.Instance.mainMenuScene.sceneName);
        }
    }
}
