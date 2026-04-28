using UnityEngine;
using NaughtyAttributes;

public class MapData : MonoBehaviour
{
    [Header("Team Spawn Points")]
    [Tooltip("Spawns for Team A (e.g., CT in TDM)")]
    public Transform[] teamASpawns;
    
    [Tooltip("Spawns for Team B (e.g., T in TDM)")]
    public Transform[] teamBSpawns;

    [Header("FFA Spawn Points")]
    [Tooltip("Spawns for Free-for-All or Deathmatch modes")]
    public Transform[] freeForAllSpawns;

    [Header("Environmental Settings")]
    [Tooltip("Optional: Material for the skybox when this map loads.")]
    public Material skyboxMaterial;

    [Tooltip("Ambient light color")]
    public Color ambientColor = Color.white;

    [Tooltip("Optional: Ambient light intensity for this map.")]
    public float ambientIntensity = 1.0f;

    [Header("Fog Settings")]
    [Tooltip("Enable fog for this map")]
    public bool enableFog = false;

    [Tooltip("Fog color")]
    public Color fogColor = Color.white;

    [Tooltip("Fog density for exponential fog")]
    public float fogDensity = 0.01f;

    [Tooltip("Fog start distance (for linear fog)")]
    public float fogStartDistance = 0f;

    [Tooltip("Fog end distance (for linear fog)")]
    public float fogEndDistance = 300f;

    /// <summary>
    /// Captures current scene lighting settings into this MapData component.
    /// Use this button to populate lighting fields from your scene setup.
    /// </summary>
    [Button("Capture Current Lighting Settings")]
    private void CaptureSceneLighting()
    {
        skyboxMaterial = RenderSettings.skybox;
        ambientColor = RenderSettings.ambientLight;
        ambientIntensity = RenderSettings.ambientIntensity;

        enableFog = RenderSettings.fog;
        fogColor = RenderSettings.fogColor;
        fogDensity = RenderSettings.fogDensity;
        fogStartDistance = RenderSettings.fogStartDistance;
        fogEndDistance = RenderSettings.fogEndDistance;

        Debug.Log($"[MapData] Captured lighting settings from scene.");
    }

    /// <summary>
    /// Applies this map's lighting and environmental settings to the scene.
    /// </summary>
    public void ApplyLighting()
    {
        if (skyboxMaterial != null)
        {
            RenderSettings.skybox = skyboxMaterial;
        }

        RenderSettings.ambientLight = ambientColor;
        RenderSettings.ambientIntensity = ambientIntensity;

        RenderSettings.fog = enableFog;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogDensity = fogDensity;
        RenderSettings.fogStartDistance = fogStartDistance;
        RenderSettings.fogEndDistance = fogEndDistance;

        DynamicGI.UpdateEnvironment();

        Debug.Log($"[MapData] Applied lighting settings for map: {gameObject.name}");
    }

    /// <summary>
    /// Gets a random spawn point for a specific team or FFA.
    /// </summary>
    public Transform GetRandomSpawnPoint(int teamIndex = -1)
    {
        Transform[] selectedGroup = null;

        if (teamIndex == 0) selectedGroup = teamASpawns;
        else if (teamIndex == 1) selectedGroup = teamBSpawns;

        // Fallback to FFA spawns if team-specific ones are missing
        if (selectedGroup == null || selectedGroup.Length == 0)
        {
            selectedGroup = freeForAllSpawns;
        }

        if (selectedGroup == null || selectedGroup.Length == 0)
        {
            Debug.LogWarning($"[MapData] No spawn points found for team {teamIndex} or FFA! Spawning at map root: {transform.position}");
            return transform;
        }

        Transform selected = selectedGroup[Random.Range(0, selectedGroup.Length)];
        Debug.Log($"[MapData] Selected spawn point: {selected.name} at {selected.position} (Team: {teamIndex})");
        return selected;
    }

    /// <summary>
    /// Gets a deterministic spawn point based on an index (useful for networking).
    /// </summary>
    public Transform GetSpawnPointSequential(int index, int teamIndex = -1)
    {
        Transform[] selectedGroup = null;

        if (teamIndex == 0) selectedGroup = teamASpawns;
        else if (teamIndex == 1) selectedGroup = teamBSpawns;

        if (selectedGroup == null || selectedGroup.Length == 0)
        {
            selectedGroup = freeForAllSpawns;
        }

        if (selectedGroup == null || selectedGroup.Length == 0)
        {
            Debug.LogWarning($"[MapData] No spawn points found for index {index} (Team: {teamIndex})! Spawning at map root.");
            return transform;
        }

        return selectedGroup[index % selectedGroup.Length];
    }
}
