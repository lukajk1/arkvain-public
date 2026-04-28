using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EscapeMenu : MonoBehaviour
{
    public static EscapeMenu Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private SceneNameHolder _lobbyScene;
    [SerializeField] private Canvas _menu;
    [SerializeField] private SettingsMenu _settingsMenu;

    [Header("Buttons")]
    [SerializeField] private Button _returnToGameButton;
    [SerializeField] private Button _settingsButton;
    [SerializeField] private Button _loadoutButton;
    [SerializeField] private Button _leaveMatchButton;
    [SerializeField] private Button _quitToDesktopButton;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        _menu.gameObject.SetActive(false);

        // Subscribe to escape key input (in Start to ensure InputManager is initialized)
        if (PersistentClient.Instance.inputManager != null)
        {
            PersistentClient.Instance.inputManager.UI.Escape.performed += OnEscapePressed;
        }

        // Subscribe to button clicks
        if (_returnToGameButton != null)
            _returnToGameButton.onClick.AddListener(OnReturnToGame);

        if (_settingsButton != null)
            _settingsButton.onClick.AddListener(OnSettings);

        if (_loadoutButton != null)
            _loadoutButton.onClick.AddListener(OnLoadoutPressed);

        if (_leaveMatchButton != null)
            _leaveMatchButton.onClick.AddListener(OnLeaveMatch);

        if (_quitToDesktopButton != null)
            _quitToDesktopButton.onClick.AddListener(OnQuitToDesktop);
    }

    public void SetState(bool value)
    {
        if (_menu != null)
        {
            _menu.gameObject.SetActive(value);
            PersistentClient.AddToCursorUnlockList(value, this);

            if (value)
            {
                PersistentClient.PushEscapeHandler(() => SetState(false));
            }
            else
            {
                PersistentClient.PopEscapeHandler();
            }
        }
    }

    private void OnEnable()
    {
        // Empty - subscriptions now in Start()
    }

    private void OnDisable()
    {
        // Unsubscribe from escape key input
        if (PersistentClient.Instance.inputManager != null)
        {
            PersistentClient.Instance.inputManager.UI.Escape.performed -= OnEscapePressed;
        }

        // Unsubscribe from button clicks
        if (_returnToGameButton != null)
            _returnToGameButton.onClick.RemoveListener(OnReturnToGame);

        if (_settingsButton != null)
            _settingsButton.onClick.RemoveListener(OnSettings);

        if (_loadoutButton != null)
            _loadoutButton.onClick.RemoveListener(OnLoadoutPressed);

        if (_leaveMatchButton != null)
            _leaveMatchButton.onClick.RemoveListener(OnLeaveMatch);

        if (_quitToDesktopButton != null)
            _quitToDesktopButton.onClick.RemoveListener(OnQuitToDesktop);
    }

    private void OnEscapePressed(InputAction.CallbackContext context)
    {
        if (_menu == null) return;

        // Try to handle escape with the stack system first
        if (PersistentClient.TryHandleEscape())
        {
            return; // Handled by something on the stack
        }

        // If nothing handled it and menu is not open, open the menu
        if (!_menu.gameObject.activeSelf)
        {
            SetState(true);
        }
    }

    private void OnReturnToGame()
    {
        SetState(false);
    }

    private void OnLoadoutPressed()
    {
        if (LoadoutManager.Instance != null)
        {
            LoadoutManager.Instance.SetState(true);
        }
    }

    private void OnSettings()
    {
        if (_settingsMenu == null)
        {
            Debug.LogWarning("Settings menu not assigned!");
            return;
        }

        _settingsMenu.SetState(true);
    }

    private void OnLeaveMatch()
    {
        SetState(false); // Clean up cursor lock modification

        if (GameManager.Instance != null)
            GameManager.Instance.LeaveMatch();
    }

    private void OnQuitToDesktop()
    {
        Application.Quit();

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
