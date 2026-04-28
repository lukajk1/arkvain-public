using Heathen.SteamworksIntegration;
using PurrLobby;
using PurrNet;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadoutManager : MonoBehaviour
{
    [SerializeField] private Canvas canvas;

    [SerializeField] private TMP_Dropdown heroDropdown;
    [SerializeField] private TMP_Dropdown weapon1Dropdown;
    [SerializeField] private TMP_Dropdown weapon2Dropdown;
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text respawnNoticeText;

    public static LoadoutSelection CurrentLoadout = new LoadoutSelection
    {
        Hero = HeroType.Richter,
        Weapon1 = WeaponType.Crossbow,
        Weapon2 = WeaponType.Revolver
    };
    private static LoadoutSelection _appliedLoadout;
    private static bool _hasSpawnedOnce;

    public static LoadoutManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        SetState(false);
        if (respawnNoticeText != null) respawnNoticeText.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        if (closeButton != null) closeButton.onClick.AddListener(CloseClicked);
        GameEvents.OnPlayerSpawned += OnPlayerSpawned;
    }

    void OnDisable()
    {
        if (closeButton != null) closeButton.onClick.RemoveListener(CloseClicked);
        GameEvents.OnPlayerSpawned -= OnPlayerSpawned;

        // Save loadout when disabled (game closing, scene change, etc.)
        SaveToPlayerPrefs();
    }

    private void OnPlayerSpawned(PlayerID player)
    {
        if (NetworkManager.main != null && player == NetworkManager.main.localPlayer)
        {
            _appliedLoadout = CurrentLoadout;
            _hasSpawnedOnce = true;
            UpdateRespawnNotice();
        }
    }

    void Start()
    {
        SetState(false);

        // Try to load from PlayerPrefs
        LoadFromPlayerPrefs();

        if (heroDropdown != null)
        {
            heroDropdown.onValueChanged.AddListener(OnHeroChanged);
            // Set dropdown value from loaded CurrentLoadout
            heroDropdown.value = (int)CurrentLoadout.Hero;
        }

        if (weapon1Dropdown != null)
        {
            weapon1Dropdown.onValueChanged.AddListener(OnWeapon1Changed);
            // Set dropdown value from loaded CurrentLoadout
            weapon1Dropdown.value = (int)CurrentLoadout.Weapon1;
        }

        if (weapon2Dropdown != null)
        {
            weapon2Dropdown.onValueChanged.AddListener(OnWeapon2Changed);
            // Set dropdown value from loaded CurrentLoadout
            weapon2Dropdown.value = (int)CurrentLoadout.Weapon2;
        }

        // Sync to PersistentClient immediately
        PersistentClient.currentLoadout = CurrentLoadout;
    }

    public void SetState(bool value)
    {
        if (value) respawnNoticeText.gameObject.SetActive(false);

        canvas.gameObject.SetActive(value);
        if (value)
        {
            UpdateRespawnNotice();
            PersistentClient.PushEscapeHandler(() => SetState(false));
        }
        else
        {
            PersistentClient.PopEscapeHandler();
        }
    }

    private void CloseClicked()
    {
        SetState(false);
        SaveToPlayerPrefs();
    }

    private void OnHeroChanged(int index)
    {
        CurrentLoadout.Hero = (HeroType)index;
        PersistentClient.currentLoadout = CurrentLoadout;
        UpdateRespawnNotice();
    }

    private void OnWeapon1Changed(int index)
    {
        WeaponType oldWeapon1 = CurrentLoadout.Weapon1;
        CurrentLoadout.Weapon1 = (WeaponType)index;

        // If both weapons are now the same, swap them
        if (CurrentLoadout.Weapon1 == CurrentLoadout.Weapon2)
        {
            CurrentLoadout.Weapon2 = oldWeapon1;
            if (weapon2Dropdown != null)
            {
                weapon2Dropdown.SetValueWithoutNotify((int)CurrentLoadout.Weapon2);
            }
        }

        PersistentClient.currentLoadout = CurrentLoadout;
        UpdateRespawnNotice();
    }

    private void OnWeapon2Changed(int index)
    {
        WeaponType oldWeapon2 = CurrentLoadout.Weapon2;
        CurrentLoadout.Weapon2 = (WeaponType)index;

        // If both weapons are now the same, swap them
        if (CurrentLoadout.Weapon1 == CurrentLoadout.Weapon2)
        {
            CurrentLoadout.Weapon1 = oldWeapon2;
            if (weapon1Dropdown != null)
            {
                weapon1Dropdown.SetValueWithoutNotify((int)CurrentLoadout.Weapon1);
            }
        }

        PersistentClient.currentLoadout = CurrentLoadout;
        UpdateRespawnNotice();
    }

    private void UpdateRespawnNotice()
    {
        if (respawnNoticeText == null) return;
        if (PersistentClient.Instance.gameScene.sceneName != SceneManager.GetActiveScene().name) return;

        bool isDifferent = CurrentLoadout.Hero != _appliedLoadout.Hero ||
                        CurrentLoadout.Weapon1 != _appliedLoadout.Weapon1 ||
                        CurrentLoadout.Weapon2 != _appliedLoadout.Weapon2;

        // Only show if we've actually spawned once and it's different
        respawnNoticeText.gameObject.SetActive(_hasSpawnedOnce && isDifferent);
    }

    private void LoadFromPlayerPrefs()
    {
        // Load Hero
        if (PlayerPrefs.HasKey("Loadout_Hero"))
        {
            CurrentLoadout.Hero = (HeroType)PlayerPrefs.GetInt("Loadout_Hero");
        }

        // Load Weapon1
        if (PlayerPrefs.HasKey("Loadout_Weapon1"))
        {
            CurrentLoadout.Weapon1 = (WeaponType)PlayerPrefs.GetInt("Loadout_Weapon1");
        }

        // Load Weapon2
        if (PlayerPrefs.HasKey("Loadout_Weapon2"))
        {
            CurrentLoadout.Weapon2 = (WeaponType)PlayerPrefs.GetInt("Loadout_Weapon2");
        }

        Debug.Log($"[LoadoutManager] Loaded loadout from PlayerPrefs: Hero={CurrentLoadout.Hero}, Weapon1={CurrentLoadout.Weapon1}, Weapon2={CurrentLoadout.Weapon2}");
    }

    private void SaveToPlayerPrefs()
    {
        PlayerPrefs.SetInt("Loadout_Hero", (int)CurrentLoadout.Hero);
        PlayerPrefs.SetInt("Loadout_Weapon1", (int)CurrentLoadout.Weapon1);
        PlayerPrefs.SetInt("Loadout_Weapon2", (int)CurrentLoadout.Weapon2);
        PlayerPrefs.Save();

        Debug.Log($"[LoadoutManager] Saved loadout to PlayerPrefs: Hero={CurrentLoadout.Hero}, Weapon1={CurrentLoadout.Weapon1}, Weapon2={CurrentLoadout.Weapon2}");
    }

    void Update()
    {

    }
}
