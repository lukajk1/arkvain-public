using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Animates the alpha of a UI Image in a sine wave pattern using LeanTween.
/// Uses LeanTween.value for maximum reliability with UI components.
/// </summary>
[RequireComponent(typeof(Image))]
public class ImageAlphaSinWave : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("How many full cycles (fade in and out) per second.")]
    [SerializeField] private float _speed = 1f;
    [SerializeField, Range(0f, 1f)] private float _minAlpha = 0f;
    [SerializeField, Range(0f, 1f)] private float _maxAlpha = 1f;

    private Image _image;

    private void Awake()
    {
        _image = GetComponent<Image>();
    }

    private void Start()
    {
        if (_image == null) return;

        // Ensure we start at minAlpha
        SetAlpha(_minAlpha);

        // One-way duration (fade in or fade out)
        // If speed is 1 cycle/sec, duration should be 0.5s for ping and 0.5s for pong.
        float duration = _speed > 0 ? (0.5f / _speed) : 1f;

        LeanTween.value(gameObject, _minAlpha, _maxAlpha, duration)
            .setEase(LeanTweenType.easeInOutSine)
            .setLoopPingPong()
            .setOnUpdate(SetAlpha);
    }

    private void SetAlpha(float alpha)
    {
        if (_image == null) return;
        Color c = _image.color;
        c.a = alpha;
        _image.color = c;
    }

    private void OnDestroy()
    {
        // Safety: ensure the tween is cancelled if the object is destroyed
        LeanTween.cancel(gameObject);
    }
}
