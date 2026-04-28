using UnityEngine;
using TMPro;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI _ammoText;
    [SerializeField] private string _ammoFormat = "{0} / {1}"; // "12 / 30" format

    [SerializeField] private TextMeshProUGUI _healthText;
    [SerializeField] private TextMeshProUGUI _healthTextDropShadow;
    [SerializeField] private Gradient _healthGradient;

    [Header("Center Display Broadcasts")]
    [SerializeField] private TextMeshProUGUI _broadcastText;
    [SerializeField] private float _broadcastRiseDistance = 80f;
    [SerializeField] private float _broadcastDuration = 1.2f;

    [Header("Weapon Iconography")]
    [SerializeField] private WeaponDataSOContainer weaponsContainer;
    [SerializeField] private Image primaryWeaponIcon;
    [SerializeField] private TextMeshProUGUI primaryWeaponHotkeyLabel;
    [SerializeField] private Image secondaryWeaponIcon;
    [SerializeField] private TextMeshProUGUI SecondaryWeaponHotkeyLabel;
    [SerializeField] private float disabledWeaponIconAlpha = 0.5f;

    private int _broadcastTweenId = -1;
    private int _broadcastAlphaTweenId = -1;
    private Vector2 _broadcastStartAnchoredPos;

    private IWeaponLogic _currentWeapon;
    private WeaponManager _weaponManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Multiple HUDManager instances detected! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (_broadcastText != null)
        {
            _broadcastStartAnchoredPos = _broadcastText.rectTransform.anchoredPosition;
            _broadcastText.alpha = 0f;
        }
    }

    /// <summary>
    /// Sets the weapon icons for both primary and secondary weapons
    /// </summary>
    public void SetWeaponIcons(WeaponType primaryWeapon, WeaponType secondaryWeapon)
    {
        if (weaponsContainer == null)
        {
            Debug.LogError("[HUDManager] WeaponsContainer is null! Cannot set weapon icons.");
            return;
        }

        // Set primary weapon icon
        var primaryData = weaponsContainer.GetWeaponData(primaryWeapon);
        if (primaryData != null && primaryWeaponIcon != null)
        {
            primaryWeaponIcon.sprite = primaryData.Icon;
            primaryWeaponIcon.enabled = true;
        }

        // Set secondary weapon icon
        var secondaryData = weaponsContainer.GetWeaponData(secondaryWeapon);
        if (secondaryData != null && secondaryWeaponIcon != null)
        {
            secondaryWeaponIcon.sprite = secondaryData.Icon;
            secondaryWeaponIcon.enabled = true;
        }

        // Set hotkey labels from input bindings
        if (PersistentClient.Instance != null && PersistentClient.Instance.inputManager != null)
        {
            if (primaryWeaponHotkeyLabel != null)
            {
                primaryWeaponHotkeyLabel.text = PersistentClient.Instance.inputManager.Player.PrimaryWeapon.GetBindingDisplayString().ToUpper();
            }

            if (SecondaryWeaponHotkeyLabel != null)
            {
                SecondaryWeaponHotkeyLabel.text = PersistentClient.Instance.inputManager.Player.SecondaryWeapon.GetBindingDisplayString().ToUpper();
            }
        }
    }

    /// <summary>
    /// Updates weapon icon alpha based on which weapon is currently equipped
    /// </summary>
    public void UpdateWeaponIconAlpha(bool isPrimaryActive)
    {
        if (primaryWeaponIcon != null)
        {
            Color primaryColor = primaryWeaponIcon.color;
            primaryColor.a = isPrimaryActive ? 1f : disabledWeaponIconAlpha;
            primaryWeaponIcon.color = primaryColor;
        }

        if (secondaryWeaponIcon != null)
        {
            Color secondaryColor = secondaryWeaponIcon.color;
            secondaryColor.a = isPrimaryActive ? disabledWeaponIconAlpha : 1f;
            secondaryWeaponIcon.color = secondaryColor;
        }
    }

    public void UpdateWeaponDisplay()
    {

    }

    public void SetHealthReadout(float currentHealth, float maxHealth)
    {
        if (_healthText != null)
        {
            int displayHealth = Mathf.CeilToInt(currentHealth);
            _healthText.text = $"{displayHealth}";
            float t = maxHealth > 0 ? currentHealth / maxHealth : 0;
            // Flip the evaluation: 0 (left) is now full health, 1 (right) is low health
            _healthText.color = _healthGradient.Evaluate(1f - t);
        }

        if (_healthTextDropShadow != null)
        {
            int displayHealth = Mathf.CeilToInt(currentHealth);
            _healthTextDropShadow.text = $"{displayHealth}";
        }
    }

    public void SetAbilityCooldown(float normalizedCooldown, float remainingSeconds)
        => AbilityHUDManager.Instance?.SetAbilityCooldown(normalizedCooldown, remainingSeconds);

    public void HideAbilityUI()
        => AbilityHUDManager.Instance?.HideAbilityUI();

    public void SetAbilityBindingName(string name)
        => AbilityHUDManager.Instance?.SetAbilityBindingName(name);

    public void BroadcastEvent(string message)
    {
        if (_broadcastText == null) return;

        if (_broadcastTweenId != -1) LeanTween.cancel(_broadcastTweenId);
        if (_broadcastAlphaTweenId != -1) LeanTween.cancel(_broadcastAlphaTweenId);

        _broadcastText.text = message;
        _broadcastText.alpha = 1f;
        _broadcastText.rectTransform.anchoredPosition = _broadcastStartAnchoredPos;

        Vector2 targetPos = _broadcastStartAnchoredPos + Vector2.up * _broadcastRiseDistance;
        _broadcastTweenId = LeanTween.move(_broadcastText.rectTransform, targetPos, _broadcastDuration)
            .setEase(LeanTweenType.easeOutCubic).id;
        _broadcastAlphaTweenId = LeanTween.value(gameObject, 1f, 0f, _broadcastDuration)
            .setEase(LeanTweenType.easeInCubic)
            .setOnUpdate((float val) => _broadcastText.alpha = val)
            .setOnComplete(() => _broadcastText.alpha = 0f).id;
    }

    private void Update()
    {
        if (_currentWeapon != null && _ammoText != null)
        {
            _ammoText.text = string.Format(_ammoFormat, _currentWeapon.CurrentAmmo, _currentWeapon.MaxAmmo);
        }
    }
}
