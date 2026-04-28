using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// A single entry in the kill feed (top right).
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class KillFeedCard : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI _killerText;
    [SerializeField] private TextMeshProUGUI _victimText;
    [SerializeField] private CanvasGroup _canvasGroup;

    [Header("Animation Settings")]
    [SerializeField] private float _scaleInDuration = 0.2f;
    [SerializeField] private float _fadeOutDuration = 0.4f;

    private void Awake()
    {
        // Initial state: hidden and flattened
        ResetState();
    }

    private void ResetState()
    {
        if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();

        transform.localScale = new Vector3(1, 0, 1);
        if (_canvasGroup != null) _canvasGroup.alpha = 0;
    }

    /// <summary>
    /// Configures the card with kill data and starts the lifetime timer.
    /// </summary>
    public void Setup(string killer, string victim, float lifetime)
    {
        // Ensure state is clean before animating
        ResetState();

        if (_killerText != null) _killerText.text = killer;
        if (_victimText != null) _victimText.text = victim;

        // 1. Initial state: flattened vertically and transparent
        _canvasGroup.alpha = 0;
        transform.localScale = new Vector3(1, 0, 1);

        // 2. Animate appearance (Match KillBroadcast style)
        LeanTween.alphaCanvas(_canvasGroup, 1f, _scaleInDuration);
        LeanTween.scaleY(gameObject, 1f, _scaleInDuration).setEaseOutExpo();

        // 3. Schedule disappearance
        Invoke(nameof(FadeOutAndDestroy), lifetime);
    }

    private void FadeOutAndDestroy()
    {
        LeanTween.alphaCanvas(_canvasGroup, 0f, _fadeOutDuration).setOnComplete(() =>
        {
            Destroy(gameObject);
        });
    }
}
