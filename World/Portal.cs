using Mono.CSharp;
using PurrNet.Prediction;
using UnityEngine;

/// <summary>
/// A predicted portal that teleports players between locations.
/// Uses networked state to track portal usage cooldown for proper prediction.
/// </summary>
public class Portal : PredictedIdentity<Portal.State>
{
    [Header("Portal Boundary")]
    [SerializeField] private Vector3 _portalBoxSize = new Vector3(2f, 3f, 0.5f);
    [SerializeField] private Vector3 _portalBoxOffset = Vector3.zero;
     private float _exitPositionGizmoRadius = 0.15f;

    [Header("Teleport Settings")]
    [SerializeField] private Portal _linkedPortal;
    public Vector3 exitPosition;
    [SerializeField] private float _portalUseCooldown = 0.8f;

    [Header("Detection")]
    [SerializeField] private LayerMask _playerLayer;

    [Header("Network Registration")]
    [Tooltip("Manually assigned unique ID for this scene object (e.g., 2001, 2002). Must be identical on all clients.")]
    [SerializeField] private uint _manualId;

    [Header("Visuals")]
    [SerializeField] private AudioClip _onTeleportClip;

    private PredictedEvent<PlayerMovement> _onTeleport;

    // Using a non-allocating array for performance in the simulation loop
    private readonly Collider[] _hitBuffer = new Collider[4];

    private bool _isRegistered = false;

    protected override void LateAwake()
    {
        base.LateAwake();

        _onTeleport = new PredictedEvent<PlayerMovement>(predictionManager, this);
        _onTeleport.AddListener(OnTeleport);
    }
    private void Update()
    {
        if (_isRegistered) return;
        if (_manualId == 0)
        {
            Debug.LogWarning($"[Portal] {gameObject.name} has no manual ID assigned! It will not be registered.");
            return;
        }

        // Use the MatchSessionManager as the authoritative source for the prediction world
        var world = MatchSessionManager.Instance?.predictionManager;

        if (world != null)
        {
            // Register this instance manually with a unique ID
            var objectId = new PredictedObjectID(_manualId);
            world.RegisterInstance(gameObject, objectId, null, false);

            _isRegistered = true;
            Debug.Log($"[Portal] Registered {gameObject.name} (ID: {_manualId}) to prediction world.");
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying && transform.localScale != Vector3.one)
        {
            Debug.Log($"[Portal] Enforcing scale (1,1,1) on {gameObject.name}");
            transform.localScale = Vector3.one;
        }
    }
#endif

    /// <summary>
    /// [NOT CURRENTLY USED] Checks if any player is currently within the detection volume of this portal.
    /// Public so linked portals can check each other's state.
    /// </summary>
    public bool CheckPlayerInVolume(out PlayerMovement player)
    {
        player = null;
        Vector3 boxCenter = transform.TransformPoint(_portalBoxOffset);
        Quaternion boxRotation = transform.rotation;

        int hitCount = Physics.OverlapBoxNonAlloc(
            boxCenter,
            _portalBoxSize * 0.5f,
            _hitBuffer,
            boxRotation,
            _playerLayer,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < hitCount; i++)
        {
            if (_hitBuffer[i].transform.root.TryGetComponent(out PlayerMovement pm))
            {
                player = pm;
                return true;
            }
        }
        return false;
    }

    protected override void Simulate(ref State state, float delta)
    {
        // Decrement cooldown
        state.useCooldown -= delta;

        // Calculate the world position/rotation for the detection box
        Vector3 boxCenter = transform.TransformPoint(_portalBoxOffset);
        Quaternion boxRotation = transform.rotation;

        // Detect players in the volume
        int hitCount = Physics.OverlapBoxNonAlloc(
            boxCenter,
            _portalBoxSize * 0.5f, // Extents are half-size
            _hitBuffer,
            boxRotation,
            _playerLayer,
            QueryTriggerInteraction.Ignore
        );

        // If player detected and cooldown expired
        if (hitCount > 0 && state.useCooldown <= 0)
        {
            for (int i = 0; i < hitCount; i++)
            {
                // Root check to ensure we find the player's movement component
                if (_hitBuffer[i].transform.root.TryGetComponent(out PlayerMovement playerMovement))
                {
                    if (_linkedPortal != null)
                    {
                        Vector3 worldExitPosition = _linkedPortal.transform.position + _linkedPortal.exitPosition;
                        playerMovement.Teleport(worldExitPosition);

                        // Set cooldown for both portals to prevent retriggering
                        state.useCooldown = _portalUseCooldown;
                        var linkedState = _linkedPortal.currentState;
                        linkedState.useCooldown = _portalUseCooldown;
                        _linkedPortal.currentState = linkedState;

                        _onTeleport?.Invoke(playerMovement);
                    }
                    else
                    {
                        Debug.LogWarning($"[Portal] {gameObject.name} has no linked portal!");
                    }

                    break;
                }
            }
        }
    }


    private void OnTeleport(PlayerMovement playerMovement)
    {
        if (_onTeleportClip != null)
        {
            SoundManager.PlayDiegetic(_onTeleportClip, transform.position, varyPitch: false, varyVolume: false);
        }

        // Reorient camera through portal (True transformation-based reflection)
        if (playerMovement._camera != null && _linkedPortal != null)
        {
            // Get camera's current look direction in world space
            Vector3 cameraForward = playerMovement._camera.forward;

            // 1. Convert the world-space camera direction to the Entry Portal's local space
            Vector3 localDir = transform.InverseTransformDirection(cameraForward);

            // 2. Reflect the direction
            // When moving "through" a portal, we are effectively entering one face and exiting another.
            // A 180-degree rotation around the Y-axis (local up) mirrors the horizontal entry, 
            // while preserving the vertical angle (pitch).
            localDir = new Vector3(-localDir.x, localDir.y, -localDir.z);

            // 3. Convert that local direction into the Exit Portal's world space
            Vector3 newDirection = _linkedPortal.transform.TransformDirection(localDir);

            // 4. Set the camera to look in the new direction
            playerMovement._camera.SetLookDirection(newDirection);
        }
    }

    protected override State GetInitialState()
    {
        return new State
        {
            useCooldown = 0f
        };
    }

    public struct State : IPredictedData<State>
    {
        public float useCooldown;

        public void Dispose() { }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Draw the portal boundary box
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(1f, 1f, 0f, 0.13f); // Yellow with 0.13f alpha
        Gizmos.DrawCube(_portalBoxOffset, _portalBoxSize);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(_portalBoxOffset, _portalBoxSize);

        // Draw the exit position sphere (relative to portal position)
        Gizmos.matrix = Matrix4x4.identity;
        Vector3 worldExitPosition = transform.position + exitPosition;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(worldExitPosition, _exitPositionGizmoRadius);
        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        Gizmos.DrawSphere(worldExitPosition, _exitPositionGizmoRadius);
    }
#endif
}
