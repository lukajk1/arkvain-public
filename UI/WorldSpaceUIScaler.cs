using UnityEngine;

public class WorldSpaceUIScaler : MonoBehaviour
{
    [SerializeField] private float _baseDistance = 10f; // Distance where scale is 1
    [SerializeField] private float _baseHeight = 2.0f;   // Base local Y offset from parent
    [SerializeField] private float _minScale = 0.5f;     // Optional: Don't get too small
    [SerializeField] private float _maxScale = 5f;       // Optional: Don't get too big
    
    private Transform _cachedCameraTransform;

    private void LateUpdate()
    {
        // Cache the camera transform reference
        if (_cachedCameraTransform == null)
        {
            // Check if camera exists and is not destroyed
            if (ClientsideGameManager._mainCamera == null || !ClientsideGameManager._mainCamera)
            {
                return;
            }
            _cachedCameraTransform = ClientsideGameManager._mainCamera.transform;
        }

        // Use parent position for distance check if available, otherwise use this object's current position
        Vector3 referencePos = transform.parent != null ? transform.parent.position : transform.position;
        float distance = Vector3.Distance(referencePos, _cachedCameraTransform.position);

        // Apply scale based on distance relative to base distance
        float currentScale = distance / _baseDistance;
        
        // Clamp if needed
        currentScale = Mathf.Clamp(currentScale, _minScale, _maxScale);

        // Maintain visual size
        transform.localScale = new Vector3(currentScale, currentScale, currentScale);

        // Maintain constant screen-space height above parent
        // Don't scale Y offset - the scale adjustment already compensates for distance
        Vector3 localPos = transform.localPosition;
        localPos.y = _baseHeight;
        transform.localPosition = localPos;
    }
}
