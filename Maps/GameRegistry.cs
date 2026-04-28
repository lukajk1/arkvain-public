using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "GameRegistry", menuName = "Arkvain/Game Registry")]
public class GameRegistry : ScriptableObject
{
    [Header("1v1 Mode")]
    [SerializeField] public string oneVOneModeDisplayName = "1v1";
    [SerializeField] private GameObject oneVOneLogicPrefab;
    [SerializeField] private List<MapDefinition> oneVOneMaps = new List<MapDefinition>();

    [Header("FFA Mode")]
    [SerializeField] public string ffamodeDisplayName = "FFA Deathmatch";
    [SerializeField] private GameObject ffaLogicPrefab;
    [SerializeField] private List<MapDefinition> ffaMaps = new List<MapDefinition>();

    /// <summary>
    /// Searches all game modes to find a map definition by its internal name.
    /// Used by the MapLoader in the game scene.
    /// </summary>
    public MapDefinition FindMapByInternalName(string internalName)
    {
        if (string.IsNullOrEmpty(internalName)) return null;

        // Search 1v1 maps
        var map = oneVOneMaps.FirstOrDefault(m => m.InternalName == internalName);
        if (map != null) return map;

        // Search FFA maps
        map = ffaMaps.FirstOrDefault(m => m.InternalName == internalName);
        if (map != null) return map;

        return null;
    }

    /// <summary>
    /// Returns the game mode logic prefab for a specific game mode.
    /// </summary>
    public GameObject GetGameModeLogicPrefab(GameMode mode)
    {
        return mode switch
        {
            GameMode.OneVOne => oneVOneLogicPrefab,
            GameMode.FFA => ffaLogicPrefab,
            _ => null
        };
    }

    /// <summary>
    /// Returns the allowed maps for a specific game mode.
    /// </summary>
    public List<MapDefinition> GetAllowedMaps(GameMode mode)
    {
        return mode switch
        {
            GameMode.OneVOne => oneVOneMaps,
            GameMode.FFA => ffaMaps,
            _ => new List<MapDefinition>()
        };
    }

    /// <summary>
    /// Returns the allowed maps for a specific game mode by string (for backward compatibility).
    /// </summary>
    public List<MapDefinition> GetAllowedMaps(string modeName)
    {
        var mode = GameModeExtensions.FromString(modeName);
        return mode.HasValue ? GetAllowedMaps(mode.Value) : new List<MapDefinition>();
    }

    /// <summary>
    /// Returns the game mode logic prefab by string (for backward compatibility).
    /// </summary>
    public GameObject GetGameModeLogicPrefab(string modeName)
    {
        var mode = GameModeExtensions.FromString(modeName);
        return mode.HasValue ? GetGameModeLogicPrefab(mode.Value) : null;
    }
}
