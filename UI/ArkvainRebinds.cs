using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Simplified rebinding UI for Arkvain that handles everything programmatically.
/// Automatically saves/loads bindings from PlayerPrefs.
/// </summary>
public class ArkvainRebinds : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputActionReference _actionReference;
    [SerializeField] private int _bindingIndex = 0; // Which binding to rebind (0 = first binding)

    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI _actionLabel;
    [SerializeField] private TextMeshProUGUI _bindingText;
    [SerializeField] private Button _rebindButton;
    [SerializeField] private Button _resetButton;
    [SerializeField] private GameObject _rebindOverlay;
    [SerializeField] private TextMeshProUGUI _rebindPrompt;

    [Header("Display Options")]
    [SerializeField] private InputBinding.DisplayStringOptions _displayStringOptions;

    private InputActionRebindingExtensions.RebindingOperation _rebindOperation;
    private const string REBIND_PREFS_PREFIX = "Arkvain_Rebind_";

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    private void OnEnable()
    {
        LoadBindingOverrides();
        UpdateUI();
        WireUpButtons();
    }

    private void OnDisable()
    {
        SaveBindingOverrides();
        CleanupRebind();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Update UI in editor when InputActionReference or binding index changes
        if (_actionReference?.action != null && Application.isPlaying)
        {
            UpdateUI();
        }
        else if (_actionReference?.action != null)
        {
            // In edit mode, just update the labels
            if (_actionLabel != null)
                _actionLabel.text = _actionReference.action.name;

            UpdateBindingDisplay();
        }
    }
#endif

    // -------------------------------------------------------------------------
    // UI Setup
    // -------------------------------------------------------------------------

    private void WireUpButtons()
    {
        if (_rebindButton != null)
        {
            _rebindButton.onClick.RemoveAllListeners();
            _rebindButton.onClick.AddListener(StartRebind);
        }

        if (_resetButton != null)
        {
            _resetButton.onClick.RemoveAllListeners();
            _resetButton.onClick.AddListener(ResetToDefault);
        }
    }

    private void UpdateUI()
    {
        // Update action label
        if (_actionLabel != null && _actionReference?.action != null)
        {
            _actionLabel.text = _actionReference.action.name;
        }

        // Update binding text
        UpdateBindingDisplay();
    }

    private void UpdateBindingDisplay()
    {
        if (_bindingText == null || _actionReference?.action == null)
            return;

        var action = _actionReference.action;

        // Find the first keyboard/mouse binding for this action
        int keyboardMouseIndex = FindKeyboardMouseBinding(action);

        if (keyboardMouseIndex >= 0)
        {
            var binding = action.bindings[keyboardMouseIndex];

            // If it's a composite, build display string from all parts
            if (binding.isComposite)
            {
                var directionMap = new System.Collections.Generic.Dictionary<string, string>();

                for (int i = keyboardMouseIndex + 1; i < action.bindings.Count && action.bindings[i].isPartOfComposite; i++)
                {
                    var partBinding = action.bindings[i];

                    // Only include keyboard/mouse parts
                    if (partBinding.groups.Contains("Keyboard&Mouse"))
                    {
                        // Only store first binding for each direction (skip duplicates like arrow keys)
                        if (!directionMap.ContainsKey(partBinding.name))
                        {
                            var partDisplay = action.GetBindingDisplayString(i, _displayStringOptions);
                            directionMap[partBinding.name] = partDisplay;
                        }
                    }
                }

                // Display in specific order: up/left/down/right
                var orderedParts = new System.Collections.Generic.List<string>();
                if (directionMap.ContainsKey("up")) orderedParts.Add(directionMap["up"]);
                if (directionMap.ContainsKey("left")) orderedParts.Add(directionMap["left"]);
                if (directionMap.ContainsKey("down")) orderedParts.Add(directionMap["down"]);
                if (directionMap.ContainsKey("right")) orderedParts.Add(directionMap["right"]);

                _bindingText.text = string.Join("/", orderedParts);
            }
            else
            {
                // Simple binding - just show it
                var displayString = action.GetBindingDisplayString(keyboardMouseIndex, _displayStringOptions);
                _bindingText.text = displayString;
            }
        }
        else
        {
            _bindingText.text = "No Keyboard/Mouse Binding";
        }
    }

    // -------------------------------------------------------------------------
    // Rebinding
    // -------------------------------------------------------------------------

    public void StartRebind()
    {
        if (_actionReference?.action == null)
        {
            Debug.LogError("[ArkvainRebinds] Action reference is null", this);
            return;
        }

        var action = _actionReference.action;

        // Find the keyboard/mouse binding to rebind
        int targetIndex = FindKeyboardMouseBinding(action);

        if (targetIndex == -1)
        {
            Debug.LogError($"[ArkvainRebinds] No keyboard/mouse binding found for action '{action.name}'", this);
            return;
        }

        // If composite, rebind only keyboard/mouse parts in specific order
        if (action.bindings[targetIndex].isComposite)
        {
            // Collect keyboard/mouse parts in the desired order: up, left, down, right
            var partsToRebind = new System.Collections.Generic.List<int>();
            var directionOrder = new[] { "up", "left", "down", "right" };
            var directionIndices = new System.Collections.Generic.Dictionary<string, int>();

            // First pass: find all keyboard/mouse parts
            for (int i = targetIndex + 1; i < action.bindings.Count && action.bindings[i].isPartOfComposite; i++)
            {
                var part = action.bindings[i];
                if (part.groups.Contains("Keyboard&Mouse") && !directionIndices.ContainsKey(part.name))
                {
                    directionIndices[part.name] = i;
                }
            }

            // Second pass: add in desired order
            foreach (var direction in directionOrder)
            {
                if (directionIndices.ContainsKey(direction))
                {
                    partsToRebind.Add(directionIndices[direction]);
                }
            }

            // Start rebinding with first part in ordered list
            if (partsToRebind.Count > 0)
            {
                PerformRebindComposite(action, partsToRebind, 0);
            }
        }
        else
        {
            PerformRebind(action, targetIndex);
        }
    }

    private void PerformRebindComposite(InputAction action, System.Collections.Generic.List<int> bindingIndices, int currentIndex)
    {
        if (currentIndex >= bindingIndices.Count)
            return;

        int bindingIndex = bindingIndices[currentIndex];

        // Disable action during rebind
        action.Disable();

        // Cancel any ongoing rebind
        _rebindOperation?.Cancel();

        // Show overlay
        if (_rebindOverlay != null)
            _rebindOverlay.SetActive(true);

        // Update prompt text
        if (_rebindPrompt != null)
        {
            var partName = action.bindings[bindingIndex].name;
            _rebindPrompt.text = $"{partName}: Press any key...";
        }

        // Start rebind operation - restrict to keyboard and mouse only
        _rebindOperation = action.PerformInteractiveRebinding(bindingIndex)
            .WithControlsExcluding("<Gamepad>")
            .WithControlsExcluding("<XRController>")
            .WithControlsExcluding("<Touchscreen>")
            .OnCancel(operation =>
            {
                // Hide overlay
                if (_rebindOverlay != null)
                    _rebindOverlay.SetActive(false);
                UpdateBindingDisplay();
                action.Enable();
                SaveBindingOverrides();
                if (PersistentClient.Instance?.inputManager != null)
                    InputManager.LoadBindingOverridesFromPlayerPrefs(PersistentClient.Instance.inputManager.Actions);
                _rebindOperation?.Dispose();
                _rebindOperation = null;
            })
            .OnComplete(operation =>
            {
                // Update display
                UpdateBindingDisplay();
                action.Enable();

                // Cleanup
                _rebindOperation?.Dispose();
                _rebindOperation = null;

                // Move to next part in list
                if (currentIndex + 1 < bindingIndices.Count)
                {
                    PerformRebindComposite(action, bindingIndices, currentIndex + 1);
                }
                else
                {
                    // All parts done - hide overlay and save
                    if (_rebindOverlay != null)
                        _rebindOverlay.SetActive(false);

                    SaveBindingOverrides();
                    if (PersistentClient.Instance?.inputManager != null)
                        InputManager.LoadBindingOverridesFromPlayerPrefs(PersistentClient.Instance.inputManager.Actions);
                }
            })
            .Start();
    }

    private void PerformRebind(InputAction action, int bindingIndex, bool allCompositeParts = false)
    {
        // Disable action during rebind
        action.Disable();

        // Cancel any ongoing rebind
        _rebindOperation?.Cancel();

        // Show overlay
        if (_rebindOverlay != null)
            _rebindOverlay.SetActive(true);

        // Update prompt text
        if (_rebindPrompt != null)
        {
            var partName = action.bindings[bindingIndex].isPartOfComposite
                ? $"{action.bindings[bindingIndex].name}: "
                : "";
            _rebindPrompt.text = $"{partName}Press any key...";
        }

        // Start rebind operation - restrict to keyboard and mouse only
        _rebindOperation = action.PerformInteractiveRebinding(bindingIndex)
            .WithControlsExcluding("<Gamepad>")
            .WithControlsExcluding("<XRController>")
            .WithControlsExcluding("<Touchscreen>")
            .OnCancel(operation => OnRebindComplete(allCompositeParts, action, bindingIndex))
            .OnComplete(operation => OnRebindComplete(allCompositeParts, action, bindingIndex))
            .Start();
    }

    private void OnRebindComplete(bool allCompositeParts, InputAction action, int bindingIndex)
    {
        // Hide overlay
        if (_rebindOverlay != null)
            _rebindOverlay.SetActive(false);

        // Update display
        UpdateBindingDisplay();

        // Re-enable action
        action.Enable();

        // Save immediately after rebind
        SaveBindingOverrides();

        // Reload bindings into active InputManager to apply changes immediately
        if (PersistentClient.Instance?.inputManager != null)
        {
            InputManager.LoadBindingOverridesFromPlayerPrefs(PersistentClient.Instance.inputManager.Actions);
        }

        // Cleanup
        _rebindOperation?.Dispose();
        _rebindOperation = null;

        // If composite, continue to next part
        if (allCompositeParts)
        {
            var nextBindingIndex = bindingIndex + 1;
            if (nextBindingIndex < action.bindings.Count && action.bindings[nextBindingIndex].isPartOfComposite)
            {
                PerformRebind(action, nextBindingIndex, true);
            }
        }
    }

    public void ResetToDefault()
    {
        if (_actionReference?.action == null)
        {
            Debug.LogError("[ArkvainRebinds] Action reference is null", this);
            return;
        }

        var action = _actionReference.action;

        // Find the keyboard/mouse binding to reset
        int targetIndex = FindKeyboardMouseBinding(action);

        if (targetIndex == -1)
        {
            Debug.LogError($"[ArkvainRebinds] No keyboard/mouse binding found for action '{action.name}'", this);
            return;
        }

        if (action.bindings[targetIndex].isComposite)
        {
            // Reset all composite parts
            for (var i = targetIndex + 1; i < action.bindings.Count && action.bindings[i].isPartOfComposite; ++i)
                action.RemoveBindingOverride(i);
        }
        else
        {
            action.RemoveBindingOverride(targetIndex);
        }

        UpdateBindingDisplay();
        SaveBindingOverrides();

        // Reload bindings into active InputManager to apply changes immediately
        if (PersistentClient.Instance?.inputManager != null)
        {
            InputManager.LoadBindingOverridesFromPlayerPrefs(PersistentClient.Instance.inputManager.Actions);
        }
    }

    // -------------------------------------------------------------------------
    // Utility
    // -------------------------------------------------------------------------

    private int FindKeyboardMouseBinding(InputAction action)
    {
        // Find the first keyboard/mouse binding for this action
        // Check if a composite has keyboard/mouse parts
        for (int i = 0; i < action.bindings.Count; i++)
        {
            var binding = action.bindings[i];

            // Check composites by examining their parts
            if (binding.isComposite)
            {
                // Check if first part is keyboard/mouse
                if (i + 1 < action.bindings.Count && action.bindings[i + 1].isPartOfComposite)
                {
                    var firstPart = action.bindings[i + 1];
                    if (firstPart.groups.Contains("Keyboard&Mouse"))
                    {
                        return i;
                    }
                }
            }
            else if (!binding.isPartOfComposite)
            {
                // Simple binding - check groups first, then path
                if (binding.groups.Contains("Keyboard&Mouse"))
                    return i;

                var path = binding.effectivePath;
                if (path.Contains("<Keyboard>") || path.Contains("<Mouse>"))
                    return i;
            }
        }

        // Fallback: find by checking device paths
        for (int i = 0; i < action.bindings.Count; i++)
        {
            var binding = action.bindings[i];

            // Check composites and their parts
            if (binding.isComposite)
            {
                // Check if ALL parts are keyboard/mouse (not mixed with gamepad)
                bool hasKeyboardMouse = false;
                bool hasNonKeyboardMouse = false;

                for (int j = i + 1; j < action.bindings.Count && action.bindings[j].isPartOfComposite; j++)
                {
                    var partPath = action.bindings[j].effectivePath;
                    if (partPath.Contains("<Keyboard>") || partPath.Contains("<Mouse>"))
                        hasKeyboardMouse = true;
                    else
                        hasNonKeyboardMouse = true;
                }

                // Only return this composite if it has keyboard/mouse and no other devices
                if (hasKeyboardMouse && !hasNonKeyboardMouse)
                    return i;
            }
            else if (!binding.isPartOfComposite)
            {
                // Check simple binding
                var path = binding.effectivePath;
                if (path.Contains("<Keyboard>") || path.Contains("<Mouse>"))
                    return i;
            }
        }

        return -1;
    }

    private void CleanupRebind()
    {
        _rebindOperation?.Cancel();
        _rebindOperation?.Dispose();
        _rebindOperation = null;
    }

    // -------------------------------------------------------------------------
    // PlayerPrefs Save/Load
    // -------------------------------------------------------------------------

    private void SaveBindingOverrides()
    {
        if (_actionReference?.action == null)
            return;

        var actionMap = _actionReference.action.actionMap;
        var overridesJson = actionMap.SaveBindingOverridesAsJson();

        // Save with unique key per action map
        string key = REBIND_PREFS_PREFIX + actionMap.name;
        PlayerPrefs.SetString(key, overridesJson);
        PlayerPrefs.Save();
    }

    private void LoadBindingOverrides()
    {
        if (_actionReference?.action == null)
            return;

        var actionMap = _actionReference.action.actionMap;
        string key = REBIND_PREFS_PREFIX + actionMap.name;

        if (PlayerPrefs.HasKey(key))
        {
            var overridesJson = PlayerPrefs.GetString(key);
            if (!string.IsNullOrEmpty(overridesJson))
            {
                actionMap.LoadBindingOverridesFromJson(overridesJson);
            }
        }
    }

}
