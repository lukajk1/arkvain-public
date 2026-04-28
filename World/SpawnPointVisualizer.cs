using NaughtyAttributes;
using UnityEngine;

/// <summary>
/// Simple editor visualizer for spawn points.
/// Draws a pink capsule gizmo that represents the player's collision volume.
/// </summary>
public class SpawnPointVisualizer : MonoBehaviour
{
    [Header("Capsule Settings")]
    [SerializeField] private float _radius = 0.5f;
    [SerializeField] private float _height = 2f;
    [SerializeField] private Color _gizmoColor = new Color(1f, 0f, 1f, 0.8f); // Pink
    [SerializeField] private bool _showForwardArrow = true;

    [Header("Placement Settings")]
    [SerializeField] private float _groundOffset = 0f;

    private void Awake()
    {
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
        }
    }

    [ContextMenu("Place On Ground")]
    [Button("Place on Ground")]
    private void PlaceOnGround()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 30f))
        {
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(transform, "Place Spawn Point On Ground");
#endif
            Vector3 newPosition = hit.point + Vector3.up * _groundOffset;
            transform.position = newPosition;

            Debug.Log($"Placed {gameObject.name} on ground at {newPosition}");
        }
        else
        {
            Debug.LogWarning($"No ground found within 30 meters below {gameObject.name}");
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        //DrawCapsule(transform.position, transform.rotation, _radius, _height, _gizmoColor);

        // Draw forward arrow to show spawn orientation
        if (_showForwardArrow)
        {
            Gizmos.color = Color.white;
            Gizmos.matrix = Matrix4x4.identity;
            Vector3 arrowStart = transform.position + Vector3.up * (_height * 0.5f);
            Gizmos.DrawRay(arrowStart, transform.forward * 1.0f);
            
            // Small arrow head
            Vector3 arrowEnd = arrowStart + transform.forward * 1.0f;
            Gizmos.DrawLine(arrowEnd, arrowEnd - (transform.forward + transform.right) * 0.2f);
            Gizmos.DrawLine(arrowEnd, arrowEnd - (transform.forward - transform.right) * 0.2f);
        }
    }

    private void DrawCapsule(Vector3 pos, Quaternion rot, float radius, float height, Color color)
    {
        Gizmos.color = color;
        
        // Save old matrix
        Matrix4x4 oldMatrix = Gizmos.matrix;
        
        // Set matrix to object's position/rotation
        // We offset Y by half height so the capsule sits on the pivot (feet)
        Vector3 centerOffset = Vector3.up * (height * 0.5f);
        Gizmos.matrix = Matrix4x4.TRS(pos + (rot * centerOffset), rot, Vector3.one);

        float halfHeight = height * 0.5f;
        float cylinderHeight = Mathf.Max(0, halfHeight - radius);

        // Draw top and bottom spheres
        Gizmos.DrawWireSphere(Vector3.up * cylinderHeight, radius);
        Gizmos.DrawWireSphere(Vector3.down * cylinderHeight, radius);

        // Draw 4 connecting lines for the cylinder part
        Gizmos.DrawLine(new Vector3(radius, cylinderHeight, 0), new Vector3(radius, -cylinderHeight, 0));
        Gizmos.DrawLine(new Vector3(-radius, cylinderHeight, 0), new Vector3(-radius, -cylinderHeight, 0));
        Gizmos.DrawLine(new Vector3(0, cylinderHeight, radius), new Vector3(0, -cylinderHeight, radius));
        Gizmos.DrawLine(new Vector3(0, cylinderHeight, -radius), new Vector3(0, -cylinderHeight, -radius));

        // Restore matrix
        Gizmos.matrix = oldMatrix;
    }
#endif
}
