using UnityEngine;
using UnityEngine.UI;
using System;

public class HitmarkerManager : MonoBehaviour
{

    [Header("Hitmarker")]
    [SerializeField] private Image _hitmarker;
    [SerializeField] private float _animationDuration = 0.3f;
    [SerializeField] private float _scaleMultiplier = 1.3f; // Scale up by 30%
    [SerializeField] private float _headshotScaleMultiplier = 1.2f; // additional scale on top

    [SerializeField] private Color _bodyHitColor = Color.white;
    [SerializeField] private Color _headshotColor = Color.red;

    [SerializeField] private AudioClip _bodyShotClip;
    [SerializeField] private AudioClip _headShotClip;
    [SerializeField] private AudioClip _noDamageHitmarkerClip;

    [Header("Kill Feddback")]
    [SerializeField] private Image _killIcon;
    [SerializeField] private float _killIconDuration = 0.3f;
    [SerializeField] private float _killIconInitialRelativeScale = 1.3f;
    [SerializeField] private float _killIconDelay = 0.3f;
    [SerializeField] private AudioClip _killSfx;

    private Vector3 _initialScale;
    private Vector3 _killIconInitialScale;
    private int _currentTweenId = -1;
    private int _killIconTweenId = -1;

    public static HitmarkerManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;

        if (_hitmarker != null)
        {
            _initialScale = _hitmarker.transform.localScale;
            SetHitmarkerAlpha(0f);
        }

        if (_killIcon != null)
        {
            _killIconInitialScale = _killIcon.transform.localScale;
            _killIcon.color = _headshotColor;
            SetKillIconAlpha(0f);
        }
    }

    private void OnEnable()
    {
        MatchSessionManager.RegisterKilledListener(OnPlayerKilled);
    }

    private void OnDisable()
    {
        MatchSessionManager.UnregisterKilledListener(OnPlayerKilled);

        if (_currentTweenId != -1) LeanTween.cancel(_currentTweenId);
        if (_killIconTweenId != -1) LeanTween.cancel(_killIconTweenId);
    }

    private void OnPlayerKilled(KillInfo info)
    {
        if (info.isLocalPlayerKiller)
        {
            PlayKillFeedback();
        }
    }

    /// <summary>
    /// Reports a hit from a weapon. Should be called by the local player's weapon logic.
    /// </summary>
    public void ReportHit(DamageResult damageResult)
    {
        switch (damageResult)
        {
            case DamageResult.HeadshotConfirmed:
                AnimateHitmarker(isHeadshot: true);
                if (_headShotClip != null)
                    SoundManager.PlayNonDiegetic(_headShotClip);
                break;

            case DamageResult.BodyshotConfirmed:
                AnimateHitmarker(isHeadshot: false);
                if (_bodyShotClip != null)
                    SoundManager.PlayNonDiegetic(_bodyShotClip);
                break;

            case DamageResult.BlockedInvulnerable:
                AnimateHitmarker(isHeadshot: false);
                if (_noDamageHitmarkerClip != null)
                    SoundManager.PlayNonDiegetic(_noDamageHitmarkerClip);
                break;

            case DamageResult.BlockedAlreadyDead:
                // No feedback for hitting corpses
                break;
        }
    }

    public void PlayKillFeedback()
    {
        AnimateKillIcon();
        if (_killSfx != null)
            SoundManager.PlayNonDiegetic(_killSfx);
    }

    private void AnimateHitmarker(bool isHeadshot)
    {
        LeanTween.cancel(_hitmarker.gameObject);

        _hitmarker.transform.localScale = _initialScale;
        Color targetColor = isHeadshot ? _headshotColor : _bodyHitColor;
        targetColor.a = 1f;
        _hitmarker.color = targetColor;

        float finalScaleMultiplier = _scaleMultiplier;
        if (isHeadshot) finalScaleMultiplier *= _headshotScaleMultiplier;

        LeanTween.scale(_hitmarker.gameObject, _initialScale * finalScaleMultiplier, _animationDuration)
            .setEase(LeanTweenType.easeOutCubic);

        _currentTweenId = LeanTween.alpha(_hitmarker.rectTransform, 0f, _animationDuration)
            .setEase(LeanTweenType.easeOutCubic)
            .id;
    }

    private void SetHitmarkerAlpha(float alpha)
    {
        Color color = _hitmarker.color;
        color.a = alpha;
        _hitmarker.color = color;
    }

    private void AnimateKillIcon()
    {
        if (_killIcon == null) return;
        LeanTween.cancel(_killIcon.gameObject);

        SetKillIconAlpha(0f);

        LeanTween.delayedCall(_killIcon.gameObject, _killIconDelay, () =>
        {
            _killIcon.transform.localScale = _killIconInitialScale;
            SetKillIconAlpha(1f);

            LeanTween.scale(_killIcon.gameObject, _killIconInitialScale * _killIconInitialRelativeScale, _killIconDuration)
                .setEase(LeanTweenType.easeOutCubic);

            _killIconTweenId = LeanTween.alpha(_killIcon.rectTransform, 0f, _killIconDuration)
                .setEase(LeanTweenType.easeOutCubic)
                .id;
        });
    }

    private void SetKillIconAlpha(float alpha)
    {
        Color color = _killIcon.color;
        color.a = alpha;
        _killIcon.color = color;
    }
}
