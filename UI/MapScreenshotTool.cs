using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using NaughtyAttributes;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MapScreenshotTool : MonoBehaviour
{
    public Camera targetCamera;

    private void Awake()
    {
        if (Application.isPlaying)
        {
            Destroy(gameObject);
        }
    }

    [Header("Resolution Settings")]
    public int customWidth = 1920;
    public int customHeight = 1080;
    
    [Header("Output Settings")]
    public string folderName = "Snapshots";
    [Tooltip("If empty, uses the current scene name")]
    public string fileNameOverride = "";
    [Tooltip("Automatically sets the import settings to Sprite (2D and UI) Single")]
    public bool convertToSprite = true;

    [Button("Capture Screenshot")]
    public void CaptureToResources()
    {
        if (targetCamera == null)
        {
            Debug.LogError("[MapScreenshotTool] Target Camera is not assigned!");
            return;
        }

        // 1. Determine the Filename
        string baseName = string.IsNullOrEmpty(fileNameOverride) ? SceneManager.GetActiveScene().name : fileNameOverride;
        string relativeFolderPath = Path.Combine("Assets", "Resources", folderName);
        string absoluteFolderPath = Path.GetFullPath(relativeFolderPath);
        
        if (!Directory.Exists(absoluteFolderPath)) 
            Directory.CreateDirectory(absoluteFolderPath);

        // 2. Find unique incremental name (01, 02, etc.)
        string finalFileName = baseName + "_01";
        int index = 1;
        while (File.Exists(Path.Combine(absoluteFolderPath, finalFileName + ".png")))
        {
            index++;
            finalFileName = $"{baseName}_{index:D2}";
        }

        string fullPath = Path.Combine(absoluteFolderPath, finalFileName + ".png");

        // 3. Setup Render Texture with Anti-Aliasing
        RenderTexture rt = new RenderTexture(customWidth, customHeight, 24);
        rt.antiAliasing = 8; // High quality AA
        
        targetCamera.targetTexture = rt;
        targetCamera.Render();

        RenderTexture.active = rt;
        // Using RGBA32 in case the camera is set to clear with alpha (for transparent icons)
        Texture2D tex = new Texture2D(customWidth, customHeight, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, customWidth, customHeight), 0, 0);
        tex.Apply();

        // 4. Clean up
        targetCamera.targetTexture = null;
        RenderTexture.active = null;
        
        if (Application.isPlaying)
            Destroy(rt);
        else
            DestroyImmediate(rt);

        // 5. Save to File
        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(fullPath, bytes);
        
        if (Application.isPlaying)
            Destroy(tex);
        else
            DestroyImmediate(tex);

        Debug.Log($"[MapScreenshotTool] Saved snapshot to: {fullPath}");

#if UNITY_EDITOR
        AssetDatabase.Refresh();
        
        // Configuration: Set to Sprite if enabled
        string relativeFilePath = Path.Combine(relativeFolderPath, finalFileName + ".png").Replace("\\", "/");
        
        if (convertToSprite)
        {
            TextureImporter importer = AssetImporter.GetAtPath(relativeFilePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false; // Usually not needed for sprites
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
        }

        // Optional: Highlight the file in project window
        Object asset = AssetDatabase.LoadAssetAtPath<Object>(relativeFilePath);
        if (asset != null)
        {
            EditorGUIUtility.PingObject(asset);
            Selection.activeObject = asset;
        }
#endif
    }
}