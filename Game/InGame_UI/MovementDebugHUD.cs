using UnityEngine;
using TMPro;

/// <summary>
/// Displays debug information about player movement on the HUD.
/// Shows velocity and current movement state.
/// Automatically finds the local player's PlayerMovement component.
/// </summary>
public class MovementDebugHUD : MonoBehaviour
{
    [Header("Display Settings")]
    [SerializeField] private bool _showDebugInfo = false;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _velocityComponentsText;
    [SerializeField] private TextMeshProUGUI _movementStateText;

    private PlayerMovement _playerMovement;

    private void Start()
    {
        UpdateUIVisibility();
    }

    private void Update()
    {
        // Poll for local player if we don't have a reference
        if (_playerMovement == null)
        {
            FindLocalPlayerMovement();

            // If still null after search, hide UI and return
            if (_playerMovement == null)
            {
                UpdateUIVisibility();
                return;
            }
        }

        // Ensure UI is visible when we have a player and debug is enabled
        if (_showDebugInfo)
        {
            UpdateUIVisibility();

            // Update velocity components display
            if (_velocityComponentsText != null)
            {
                Vector3 velocity = _playerMovement._rigidbody.linearVelocity;
                Vector3 horizontalVel = new Vector3(velocity.x, 0, velocity.z);
                float xzSpeed = horizontalVel.magnitude;
                float ySpeed = velocity.y;
                _velocityComponentsText.text = $"XZ: {xzSpeed:F2} m/s | Y: {ySpeed:F2} m/s";
            }

            // Update movement state display
            if (_movementStateText != null)
            {
                _movementStateText.text = $"STATE: {_playerMovement.CurrentMovementState}";
            }
        }
    }

    private void FindLocalPlayerMovement()
    {
        // Find all PlayerMovement components in scene
        PlayerMovement[] allPlayerMovements = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);

        foreach (var pm in allPlayerMovements)
        {
            if (pm.isOwner)
            {
                _playerMovement = pm;
                return;
            }
        }
    }

    private void UpdateUIVisibility()
    {
        bool shouldShow = _showDebugInfo && _playerMovement != null;

        if (_velocityComponentsText != null)
            _velocityComponentsText.gameObject.SetActive(shouldShow);

        if (_movementStateText != null)
            _movementStateText.gameObject.SetActive(shouldShow);
    }

    public void SetShowDebugInfo(bool show)
    {
        _showDebugInfo = show;
        UpdateUIVisibility();
    }
}
