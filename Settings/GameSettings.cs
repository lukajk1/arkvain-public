using UnityEngine;
using System.IO;

/// <summary>
/// Window mode options
/// </summary>
public enum WindowMode
{
    Windowed = 0,
    Borderless = 1,
    Fullscreen = 2
}

/// <summary>
/// Serializable struct containing all game settings
/// </summary>
[System.Serializable]
public struct GameSettingsData
{
    // Audio
    public float masterVolume;
    public float musicVolume;
    public float sfxVolume;

    // Input
    public bool invertY;
    public int mouseDPI;
    public float cmPer360;

    // Graphics
    public bool vsyncEnabled;
    public int targetFrameRate;
    public WindowMode windowMode;
    public int resolutionWidth;
    public int resolutionHeight;
    public int FOV;
    public float adsZoomRatio;

    /// <summary>
    /// Returns default settings. In the case that the json file is not found, values populate from here.
    /// </summary>
    public static GameSettingsData GetDefaults()
    {
        return new GameSettingsData
        {
            masterVolume = 0.5f,
            musicVolume = 1.0f,
            sfxVolume = 1.0f,

            invertY = false,
            mouseDPI = 800,
            cmPer360 = 35f,
            FOV = 80,
            adsZoomRatio = 0.5f,

            vsyncEnabled = true,
            targetFrameRate = 60,
            windowMode = WindowMode.Borderless,
            resolutionWidth = 1920,
            resolutionHeight = 1080
        };
    }
}

[CreateAssetMenu(fileName = "GameSettings", menuName = "Settings/Game Settings")]
public class GameSettings : ScriptableObject
{
    private const string SETTINGS_FILENAME = "gamesettings.json";

    /// <summary>
    /// Event that fires when GameSettings is initialized and loaded
    /// </summary>
    public static event System.Action OnSettingsInitialized;

    [HideInInspector] public GameSettingsData data = GameSettingsData.GetDefaults();

    private static GameSettings _instance;
    public static GameSettings Instance
    {
        get
        {
            if (_instance == null)
            {
                Debug.Log("Loading GameSettings from Resources...");
                _instance = Resources.Load<GameSettings>("GameSettings");
                if (_instance == null)
                {
                    Debug.LogError("GameSettings asset not found in Resources folder! Create one at Assets/Resources/GameSettings.asset");
                }
                else
                {
                    Debug.Log($"GameSettings loaded successfully. Data initialized: {_instance.data.vsyncEnabled}");
                }
            }
            return _instance;
        }
    }

    private static string SettingsFilePath => Path.Combine(Application.persistentDataPath, SETTINGS_FILENAME);

    public static void Initialize()
    {
        if (Instance != null)
        {
            Instance.LoadFromFile();
            Instance.ApplySettings();
            Debug.Log("GameSettings initialized, firing OnSettingsInitialized event");
            OnSettingsInitialized?.Invoke();
        }
        else
        {
            Debug.LogError("GameSettings Instance is null during Initialize!");
        }
    }
    /// <summary>
    /// Load settings from JSON file in persistent data path
    /// </summary>
    public void LoadFromFile()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                string json = File.ReadAllText(SettingsFilePath);
                data = JsonUtility.FromJson<GameSettingsData>(json);

                // Apply defaults for any missing fields (for backward compatibility with old JSON files)
                ApplyMissingDefaults(ref data);

                Debug.Log($"Settings loaded from {SettingsFilePath}");
            }
            else
            {
                Debug.Log("No settings file found, using defaults with current screen resolution");
                data = GameSettingsData.GetDefaults();
                // Override resolution defaults with actual screen values
                data.targetFrameRate = (int)Screen.currentResolution.refreshRateRatio.value;
                data.resolutionWidth = Screen.currentResolution.width;
                data.resolutionHeight = Screen.currentResolution.height;
                SaveToFile();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load settings: {e.Message}");
            ResetToDefaults();
        }
    }

    /// <summary>
    /// Save settings to JSON file in persistent data path
    /// </summary>
    public void SaveToFile()
    {
        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SettingsFilePath, json);
            Debug.Log($"Settings saved to {SettingsFilePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save settings: {e.Message}");
        }
    }

    /// <summary>
    /// Apply settings to the game (graphics, audio, etc.)
    /// </summary>
    public void ApplySettings()
    {
        // Apply graphics settings
        QualitySettings.vSyncCount = data.vsyncEnabled ? 1 : 0;

        // Only set target frame rate if VSync is disabled
        // (VSync overrides targetFrameRate and locks to monitor refresh rate)
        if (!data.vsyncEnabled)
        {
            Application.targetFrameRate = data.targetFrameRate;
        }
        else
        {
            Application.targetFrameRate = -1; // Let VSync control frame rate
        }

        //Debug.Log($"Applied settings - VSync: {data.vsyncEnabled}, TargetFPS: {Application.targetFrameRate}, QualityVSync: {QualitySettings.vSyncCount}");

        // Apply resolution and window mode (skip in editor to avoid issues with Game View)
        #if !UNITY_EDITOR
        // Convert window mode enum to Unity's FullScreenMode
        FullScreenMode targetMode = FullScreenMode.Windowed;
        switch (data.windowMode)
        {
            case WindowMode.Windowed:
                targetMode = FullScreenMode.Windowed;
                break;
            case WindowMode.Borderless:
                targetMode = FullScreenMode.FullScreenWindow;
                break;
            case WindowMode.Fullscreen:
                targetMode = FullScreenMode.ExclusiveFullScreen;
                break;
        }

        // Set resolution and fullscreen mode together for more reliable switching
        Screen.SetResolution(data.resolutionWidth, data.resolutionHeight, targetMode);

        // Some platforms require explicit fullScreenMode assignment as well
        if (Screen.fullScreenMode != targetMode)
        {
            Screen.fullScreenMode = targetMode;
        }
        #endif

        // Apply audio settings via AudioManager
        if (AudioMixerManager.Instance != null)
        {
            AudioMixerManager.Instance.ApplyVolumeSettings();
        }

        // Apply input settings to ClientGame static fields
        PersistentClient.playerDPI = data.mouseDPI;
        PersistentClient.cm360 = data.cmPer360;

        if (ClientsideGameManager._mainCamera != null)
        {
            ClientsideGameManager._mainCamera.fieldOfView = (float)data.FOV;
        }
    }

    /// <summary>
    /// Apply default values for any fields that are missing from loaded JSON (backward compatibility)
    /// </summary>
    private void ApplyMissingDefaults(ref GameSettingsData loadedData)
    {
        var defaults = GameSettingsData.GetDefaults();

        // Check if adsZoomRatio is at default value (0) - likely missing from old JSON
        if (loadedData.adsZoomRatio == 0f)
        {
            loadedData.adsZoomRatio = defaults.adsZoomRatio;
        }

        // Clamp adsZoomRatio to valid range (0.1-1.0)
        loadedData.adsZoomRatio = Mathf.Clamp(loadedData.adsZoomRatio, 0.1f, 1f);

        // Add checks for other new fields here as needed in the future
    }

    /// <summary>
    /// Reset all settings to defaults
    /// </summary>
    public void ResetToDefaults()
    {
        data = GameSettingsData.GetDefaults();
        // Override resolution defaults with actual screen values
        data.targetFrameRate = (int)Screen.currentResolution.refreshRateRatio.value;
        data.resolutionWidth = Screen.currentResolution.width;
        data.resolutionHeight = Screen.currentResolution.height;
        SaveToFile();
        ApplySettings();
    }
}
