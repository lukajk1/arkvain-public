using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor tool to automatically generate colliders for level geometry.
/// - Objects containing "Cube" in name get BoxColliders
/// - Everything else gets convex MeshColliders
/// - All colliders assigned the same PhysicMaterial and Layer
/// - All objects marked as static
/// - Skips gameplay objects (JumpPad, Portal, SpawnPointVisualizer, ExcludeFromColliderGeneration marker)
/// </summary>
public class LevelColliderGenerator : EditorWindow
{
    private const string PREFS_PHYSICS_MATERIAL = "LevelColliderGen_PhysicsMaterial";
    private const string PREFS_LIT_MATERIAL = "LevelColliderGen_LitMaterial";
    private const string PREFS_TARGET_LAYER = "LevelColliderGen_TargetLayer";

    [SerializeField] private PhysicsMaterial physicsMaterial;
    [SerializeField] private Material litMaterialOverride;
    [SerializeField] private int targetLayer = 0;

    private int processedCount = 0;
    private int cubeCount = 0;
    private int meshCount = 0;
    private int skippedCount = 0;
    private int materialReplacedCount = 0;

    [MenuItem("Tools/Level Colliders/Generator")]
    public static void ShowWindow()
    {
        GetWindow<LevelColliderGenerator>("Level Collider Generator");
    }

    private void OnEnable()
    {
        // Load saved preferences
        LoadPreferences();
    }

    private void LoadPreferences()
    {
        // Load PhysicsMaterial from saved asset path
        string physicsPath = EditorPrefs.GetString(PREFS_PHYSICS_MATERIAL, "");
        if (!string.IsNullOrEmpty(physicsPath))
        {
            physicsMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(physicsPath);
        }

        // Load Lit Material from saved asset path
        string litPath = EditorPrefs.GetString(PREFS_LIT_MATERIAL, "");
        if (!string.IsNullOrEmpty(litPath))
        {
            litMaterialOverride = AssetDatabase.LoadAssetAtPath<Material>(litPath);
        }

        // Load target layer
        targetLayer = EditorPrefs.GetInt(PREFS_TARGET_LAYER, 0);
    }

    private void SavePreferences()
    {
        // Save PhysicsMaterial asset path
        if (physicsMaterial != null)
        {
            string materialPath = AssetDatabase.GetAssetPath(physicsMaterial);
            EditorPrefs.SetString(PREFS_PHYSICS_MATERIAL, materialPath);
        }
        else
        {
            EditorPrefs.SetString(PREFS_PHYSICS_MATERIAL, "");
        }

        // Save Lit Material asset path
        if (litMaterialOverride != null)
        {
            string litPath = AssetDatabase.GetAssetPath(litMaterialOverride);
            EditorPrefs.SetString(PREFS_LIT_MATERIAL, litPath);
        }
        else
        {
            EditorPrefs.SetString(PREFS_LIT_MATERIAL, "");
        }

        // Save target layer
        EditorPrefs.SetInt(PREFS_TARGET_LAYER, targetLayer);
    }

    private void OnGUI()
    {
        GUILayout.Label("Level Collider Generator", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "Scans entire scene:\n" +
            "• Objects containing 'Cube' in name → BoxCollider\n" +
            "• All other objects → Convex MeshCollider\n" +
            "• Assigns PhysicMaterial, Layer, and marks Static\n" +
            "• Replaces materials named 'Lit' with override\n" +
            "• Skips gameplay objects (JumpPad, Portal, etc.)",
            MessageType.Info);

        EditorGUILayout.Space();

        EditorGUI.BeginChangeCheck();

        physicsMaterial = (PhysicsMaterial)EditorGUILayout.ObjectField(
            "Physics Material",
            physicsMaterial,
            typeof(PhysicsMaterial),
            false);

        litMaterialOverride = (Material)EditorGUILayout.ObjectField(
            "Lit Material Override",
            litMaterialOverride,
            typeof(Material),
            false);

        targetLayer = EditorGUILayout.LayerField("Target Layer", targetLayer);

        // Save preferences when values change
        if (EditorGUI.EndChangeCheck())
        {
            SavePreferences();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Generate Colliders for Entire Scene", GUILayout.Height(40)))
        {
            if (physicsMaterial == null)
            {
                EditorUtility.DisplayDialog(
                    "Missing Physics Material",
                    "Please assign a Physics Material before generating colliders.",
                    "OK");
                return;
            }

            if (EditorUtility.DisplayDialog(
                "Generate Level Colliders",
                "This will modify all GameObjects in the scene. Continue?",
                "Yes",
                "Cancel"))
            {
                GenerateAllColliders();
            }
        }

        if (processedCount > 0 || skippedCount > 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                $"Last Run Results:\n" +
                $"• Total processed: {processedCount}\n" +
                $"• Box Colliders (Cubes): {cubeCount}\n" +
                $"• Mesh Colliders: {meshCount}\n" +
                $"• Materials Replaced: {materialReplacedCount}\n" +
                $"• Skipped (gameplay objects): {skippedCount}",
                MessageType.None);
        }
    }

    private void GenerateAllColliders()
    {
        // Reset counters
        processedCount = 0;
        cubeCount = 0;
        meshCount = 0;
        skippedCount = 0;
        materialReplacedCount = 0;

        // Get all root GameObjects in scene
        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();

        // Process each root and all children
        foreach (GameObject root in rootObjects)
        {
            ProcessGameObjectRecursive(root);
        }

        Debug.Log($"[LevelColliderGenerator] Processed {processedCount} objects: {cubeCount} BoxColliders, {meshCount} MeshColliders, {materialReplacedCount} material replacements, {skippedCount} skipped");
    }

    private void ProcessGameObjectRecursive(GameObject obj)
    {
        // Check for exclusion marker component early - skip this object AND all children
        if (obj.GetComponent<ExcludeFromColliderGeneration>() != null)
        {
            skippedCount++;
            return; // Don't process this object or any children
        }

        // Process this object
        ProcessSingleObject(obj);

        // Process all children
        foreach (Transform child in obj.transform)
        {
            ProcessGameObjectRecursive(child.gameObject);
        }
    }

    private void ProcessSingleObject(GameObject obj)
    {
        // Skip known gameplay objects
        if (obj.GetComponent<JumpPad>() != null ||
            obj.GetComponent<Portal>() != null ||
            obj.GetComponent<SpawnPointVisualizer>() != null)
        {
            skippedCount++;
            return;
        }

        Undo.RegisterCompleteObjectUndo(obj, "Generate Level Collider");

        // Material replacement logic
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null && litMaterialOverride != null)
        {
            Material[] sharedMaterials = renderer.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < sharedMaterials.Length; i++)
            {
                if (sharedMaterials[i] != null && sharedMaterials[i].name == "Lit")
                {
                    sharedMaterials[i] = litMaterialOverride;
                    changed = true;
                }
            }

            if (changed)
            {
                renderer.sharedMaterials = sharedMaterials;
                materialReplacedCount++;
            }
        }

        // Check if object has a MeshFilter (visual geometry)
        MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            // No mesh to collide with, skip collider generation
            return;
        }

        processedCount++;

        // Determine collider type based on name containing "Cube" (case-sensitive)
        if (obj.name.Contains("Cube"))
        {
            GenerateBoxCollider(obj);
            cubeCount++;
        }
        else
        {
            GenerateMeshCollider(obj, meshFilter.sharedMesh);
            meshCount++;
        }

        // Set layer
        obj.layer = targetLayer;

        // Mark as static
        obj.isStatic = true;

        EditorUtility.SetDirty(obj);
    }

    private void GenerateBoxCollider(GameObject obj)
    {
        // Remove existing collider if present
        Collider existingCollider = obj.GetComponent<Collider>();
        if (existingCollider != null)
        {
            Undo.DestroyObjectImmediate(existingCollider);
        }

        // Add BoxCollider
        BoxCollider boxCollider = Undo.AddComponent<BoxCollider>(obj);
        boxCollider.material = physicsMaterial;
    }

    private void GenerateMeshCollider(GameObject obj, Mesh mesh)
    {
        // Remove existing collider if present
        Collider existingCollider = obj.GetComponent<Collider>();
        if (existingCollider != null)
        {
            Undo.DestroyObjectImmediate(existingCollider);
        }

        // Add MeshCollider
        MeshCollider meshCollider = Undo.AddComponent<MeshCollider>(obj);
        meshCollider.sharedMesh = mesh;
        meshCollider.convex = true;
        meshCollider.material = physicsMaterial;
    }

    // Context menu option for quick access
    [MenuItem("GameObject/Level Colliders/Generate", false, 0)]
    private static void GenerateFromContextMenu()
    {
        LevelColliderGenerator window = GetWindow<LevelColliderGenerator>("Level Collider Generator");
        window.Show();
    }
}
