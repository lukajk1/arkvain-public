#if UNITY_EDITOR
using NaughtyAttributes;
using UnityEditor;
using UnityEngine;

public class CameraToSceneView : MonoBehaviour
{

    [Button("Set to Scene View")]
    private void CaptureSceneCamera()
    {
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView != null)
        {
            Camera sceneCamera = sceneView.camera;

            transform.position = sceneCamera.transform.position;
            transform.rotation = sceneCamera.transform.rotation;

            Debug.Log($"Captured Scene view camera - Pos: {transform.position}, Rot: {transform.rotation.eulerAngles}");
        }
        else
        {
            Debug.LogWarning("No active Scene view found!");
        }
    }
}
#endif