using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    private InputSystem_Actions _actions;
    public InputSystem_Actions.PlayerActions Player => _actions.Player;
    public InputSystem_Actions.UIActions UI => _actions.UI;
    public InputSystem_Actions Actions => _actions;

    // Track what's locking player controls (merged from LockActionMap)
    private readonly HashSet<object> _playerControlsLocks = new HashSet<object>();

    private bool initialized;
    private const string REBIND_PREFS_PREFIX = "Arkvain_Rebind_";

    private void Awake()
    {
        // child of persistentclient -- doesn't have to check for uniqueness
        if (!initialized)
        {
            _actions = new InputSystem_Actions();

            // Load saved bindings before enabling
            LoadBindingOverrides();

            _actions.Player.Enable();
            _actions.UI.Enable();
            initialized = true;
        }
    }

    private void LoadBindingOverrides()
    {
        LoadBindingOverridesFromPlayerPrefs(_actions);
    }

    /// <summary>
    /// Loads saved binding overrides from PlayerPrefs for all action maps.
    /// Call this after changing bindings to apply them immediately.
    /// </summary>
    public static void LoadBindingOverridesFromPlayerPrefs(InputSystem_Actions actions)
    {
        if (actions == null) return;

        // Clear all existing overrides first to ensure resets work correctly
        actions.asset.RemoveAllBindingOverrides();

        // Load Player action map bindings
        string playerKey = REBIND_PREFS_PREFIX + "Player";
        if (PlayerPrefs.HasKey(playerKey))
        {
            var playerOverrides = PlayerPrefs.GetString(playerKey);
            if (!string.IsNullOrEmpty(playerOverrides))
            {
                actions.asset.LoadBindingOverridesFromJson(playerOverrides);
            }
        }

        // Load UI action map bindings
        string uiKey = REBIND_PREFS_PREFIX + "UI";
        if (PlayerPrefs.HasKey(uiKey))
        {
            var uiOverrides = PlayerPrefs.GetString(uiKey);
            if (!string.IsNullOrEmpty(uiOverrides))
            {
                actions.asset.LoadBindingOverridesFromJson(uiOverrides);
            }
        }
    }
    private void OnDestroy()
    {
        _actions?.Dispose();
    }
    public void EnablePlayerControls()
    {
        _actions.Player.Enable();
    }

    public void DisablePlayerControls()
    {
        _actions.Player.Disable();
    }

    /// <summary>
    /// Adds or removes an object from the list of things locking the player controls.
    /// Controls are only enabled when the lock list is empty.
    /// </summary>
    public void ModifyPlayerControlsLockList(bool isAdding, object lockObject)
    {
        if (isAdding)
        {
            _playerControlsLocks.Add(lockObject);
        }
        else
        {
            _playerControlsLocks.Remove(lockObject);
        }

        UpdatePlayerControlsState();
    }

    public void UnlockPlayerControls(object lockObject)
    {
        ModifyPlayerControlsLockList(false, lockObject);
    }

    public void ClearAllLocks()
    {
        _playerControlsLocks.Clear();
        UpdatePlayerControlsState();
    }

    private void UpdatePlayerControlsState()
    {
        if (_actions == null) return;

        if (_playerControlsLocks.Count > 0)
        {
            _actions.Player.Disable();
        }
        else
        {
            _actions.Player.Enable();
        }
    }
}
