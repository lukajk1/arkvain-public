using PurrNet.Prediction;
using UnityEngine;

/// <summary>
/// A predicted jump pad that detects players in a defined volume and launches them.
/// Uses networked state to track launch cooldown for proper prediction.
/// </summary>
public class JumpPad : PredictedIdentity<JumpPad.State>
{
    [SerializeField] private bool drawGizmo;

    [Header("Launch Settings")]
    [SerializeField] private float _launchForce = 15f;
    [SerializeField] private Vector3 _launchDirection = Vector3.up;
    [SerializeField] private float _launchCooldown = 0.8f;
    
    [Header("Detection")]
    [SerializeField] private LayerMask _playerLayer;
    [SerializeField] private Vector3 _detectionBoxSize = new Vector3(1.5f, 0.5f, 1.5f);
    [SerializeField] private Vector3 _detectionBoxOffset = new Vector3(0, 0.5f, 0);

    [Header("Network Registration")]
    [Tooltip("Manually assigned unique ID for this scene object (e.g., 1001, 1002). Must be identical on all clients.")]
    [SerializeField] private uint _manualId;

    [Header("Visuals")]
    [SerializeField] private AudioClip _bounceClip;
    [SerializeField] private ParticleSystem _particleSystem;
    [Tooltip("Additional angle (in degrees) to exaggerate the particle emission beyond the actual launch direction. Positive values tilt more upward.")]
    [SerializeField] private float _particleAngleExaggeration = 0f;

    private PredictedEvent<PlayerMovement> _onBounce;

    // Using a non-allocating array for performance in the simulation loop
    private readonly Collider[] _hitBuffer = new Collider[4];

    private bool _isRegistered = false;

    protected override void LateAwake()
    {
        base.LateAwake();

        // Reference: Portal.cs uses PredictedEvent<PlayerMovement>
        _onBounce = new PredictedEvent<PlayerMovement>(predictionManager, this);
        _onBounce.AddListener(OnBounce);

        // Update particle emission direction at runtime
        UpdateParticleEmissionDirection();
    }

    private void Update()
    {
        if (_isRegistered) return;
        if (_manualId == 0)
        {
            Debug.LogWarning($"[JumpPad] {gameObject.name} has no manual ID assigned! It will not be registered.");
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
            Debug.Log($"[JumpPad] Registered {gameObject.name} (ID: {_manualId}) to prediction world.");
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying && transform.localScale != Vector3.one)
        {
            Debug.Log($"[JumpPad] Enforcing scale (1,1,1) on {gameObject.name}");
            transform.localScale = Vector3.one;
        }

        // Update particle emission direction in editor when values change
        UpdateParticleEmissionDirection();
    }
#endif

    protected override void Simulate(ref State state, float delta)
    {
        // Decrement cooldown
        state.useCooldown -= delta;

        // Calculate the world position/rotation for the detection box
        Vector3 boxCenter = transform.TransformPoint(_detectionBoxOffset);
        Quaternion boxRotation = transform.rotation;

        // Detect players in the volume
        int hitCount = Physics.OverlapBoxNonAlloc(
            boxCenter,
            _detectionBoxSize * 0.5f, // Extents are half-size
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
                    // Determine launch direction (priority: manual launchDirection > transform.up fallback)
                    Vector3 launchDir;
                    if (_launchDirection.sqrMagnitude > 0.0001f)
                        launchDir = transform.TransformDirection(_launchDirection.normalized);
                    else
                        launchDir = transform.up;

                    // Perform the launch
                    playerMovement.Launch(launchDir * _launchForce, ForceMode.VelocityChange);

                    // Set cooldown to prevent retriggering
                    state.useCooldown = _launchCooldown;

                    // Invoke the predicted bounce event with the player as reference
                    _onBounce?.Invoke(playerMovement);

                    break;
                }
            }
        }
    }

    private void OnBounce(PlayerMovement playerMovement)
    {
        if (_bounceClip != null)
        {
            SoundManager.PlayDiegetic(_bounceClip, transform.position, varyPitch: false, varyVolume: false);
        }
    }

    private void UpdateParticleEmissionDirection()
    {
        if (_particleSystem == null) return;

        // Calculate the launch direction (same logic as in Simulate and OnDrawGizmos)
        Vector3 launchDir;
        if (_launchDirection.sqrMagnitude > 0.0001f)
            launchDir = transform.TransformDirection(_launchDirection.normalized);
        else
            launchDir = transform.up;

        // Apply angle exaggeration for visual clarity
        if (Mathf.Abs(_particleAngleExaggeration) > 0.01f)
        {
            // Find the axis perpendicular to both the launch direction and world up
            Vector3 horizontalDir = new Vector3(launchDir.x, 0f, launchDir.z);

            if (horizontalDir.sqrMagnitude > 0.0001f)
            {
                // Rotation axis is perpendicular to the horizontal direction (for pitch adjustment)
                Vector3 rotationAxis = Vector3.Cross(horizontalDir.normalized, Vector3.up);

                // Apply the exaggeration rotation
                launchDir = Quaternion.AngleAxis(_particleAngleExaggeration, rotationAxis) * launchDir;
            }
            else
            {
                // If launch is purely vertical, use forward as rotation axis
                launchDir = Quaternion.AngleAxis(_particleAngleExaggeration, Vector3.forward) * launchDir;
            }
        }

        // Get the shape module
        var shape = _particleSystem.shape;

        // Convert world-space launch direction to rotation
        Quaternion worldRotation = Quaternion.LookRotation(launchDir);

        // Convert to local space relative to particle system transform
        Quaternion localRotation = Quaternion.Inverse(_particleSystem.transform.rotation) * worldRotation;

        // Apply rotation to shape module (in euler angles)
        shape.rotation = localRotation.eulerAngles;

        // Configure renderer to make particles face upward in world space
        var renderer = _particleSystem.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.alignment = ParticleSystemRenderSpace.World;

            // Rotate particle quads 90 degrees on X-axis
            renderer.flip = new Vector3(0, 0, 0);

            // Alternative: Set pivot offset to rotate quads
            renderer.pivot = new Vector3(0, 0, 0);
        }

        // Rotate individual particles 90 degrees on X-axis using Start Rotation 3D
        var main = _particleSystem.main;
        main.startRotation3D = true;
        main.startRotationX = 90f * Mathf.Deg2Rad; // Unity uses radians
        main.startRotationY = 0f;
        main.startRotationZ = 0f;
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
        // Determine which launch direction will be used
        Vector3 launchDir;
        if (_launchDirection.sqrMagnitude > 0.0001f)
            launchDir = transform.TransformDirection(_launchDirection.normalized);
        else
            launchDir = transform.up;

        Vector3 drawOrigin = transform.position;

        // Draw the launch direction arrow (always cyan)
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(drawOrigin, launchDir * 2f);
        Vector3 arrowEnd = drawOrigin + launchDir * 2f;
        Gizmos.DrawWireSphere(arrowEnd, 0.1f);

        if (drawGizmo)
        {
            // Draw the detection box
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0f, 1f, 1f, 0.13f); // Transparent cyan
            Gizmos.DrawCube(_detectionBoxOffset, _detectionBoxSize);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(_detectionBoxOffset, _detectionBoxSize);
        }
    }
#endif
}
