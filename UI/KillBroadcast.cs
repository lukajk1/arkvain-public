using UnityEngine;
using TMPro;

/// <summary>
/// Handles a dramatic, large-scale kill notification broadcast.
/// Animates vertically (Y-scale) and fades out over time using a CanvasGroup.
/// </summary>
public class KillBroadcast : MonoBehaviour
{
    public static KillBroadcast Instance { get; private set; }

    [Header("References")]
    [SerializeField] private TextMeshProUGUI _killText;
    [SerializeField] private CanvasGroup _killBroadcastCanvasGroup;

    [Header("Kill Feed Settings")]
    [SerializeField] private KillFeedCard _killFeedCardPrefab;
    [SerializeField] private Transform _killFeedContainer;
    [SerializeField] private float _killFeedLifetime = 4f;
    [SerializeField] private int _maxKillFeedItems = 3;

    [Header("Animation Settings")]
    [SerializeField] private float _scaleInDuration = 0.2f;
    [SerializeField] private float _holdDuration = 1.5f;
    [SerializeField] private float _fadeOutDuration = 0.6f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Initial state: hidden and flattened
        ResetState();
    }

    private void Start()
    {
        MatchSessionManager.RegisterKilledListener(OnMatchPlayerKilled);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        MatchSessionManager.UnregisterKilledListener(OnMatchPlayerKilled);
    }

    private void OnMatchPlayerKilled(KillInfo info)
    {
        if (MatchSessionManager.Instance == null) return;

        string killerName = MatchSessionManager.Instance.GetPlayerName(info.killer);
        string victimName = MatchSessionManager.Instance.GetPlayerName(info.victim);

        // 1. Always update the scrolling feed for everyone
        AnimateKillFeed(killerName, victimName);

        // 2. Only show the big center banner if the local player is the killer
        if (info.isLocalPlayerKiller && !info.isLocalPlayerVictim)
        {
            AnimateKillBroadcast($"KILLED {victimName.ToUpper()}");
        }
    }

    private void ResetState()
    {
        // Reset scale of the object this script is on (the container)
        transform.localScale = new Vector3(1, 0, 1);
        
        // Reset alpha of the specific canvas group
        if (_killBroadcastCanvasGroup != null) 
            _killBroadcastCanvasGroup.alpha = 0;
    }

    /// <summary>
    /// Displays a message on the large central kill broadcast banner.
    /// </summary>
    public void AnimateKillBroadcast(string message)
    {
        // 1. Interrupt any running animations on this object
        LeanTween.cancel(gameObject);

        // 2. Set content and initial visibility
        if (_killText != null) _killText.text = message;
        
        if (_killBroadcastCanvasGroup != null) 
            _killBroadcastCanvasGroup.alpha = 1;
            
        transform.localScale = new Vector3(1, 0, 1);

        // 3. Fast In, Slow Out Scale Animation (EaseOutExpo)
        LeanTween.scaleY(gameObject, 1f, _scaleInDuration)
            .setEaseOutExpo()
            .setOnComplete(() =>
            {
                // 4. Wait, then fade away the specific canvas group
                if (_killBroadcastCanvasGroup != null)
                {
                    LeanTween.alphaCanvas(_killBroadcastCanvasGroup, 0f, _fadeOutDuration)
                        .setDelay(_holdDuration)
                        .setEaseLinear()
                        .setOnComplete(ResetState);
                }
            });
    }

    /// <summary>
    /// Adds a new entry to the kill feed (top right).
    /// </summary>
    public void AnimateKillFeed(string killerName, string victimName)
    {
        if (_killFeedCardPrefab == null || _killFeedContainer == null) return;

        // 1. Enforce max limit by removing the oldest card (the top-most one in a VerticalLayoutGroup)
        if (_killFeedContainer.childCount >= _maxKillFeedItems)
        {
            // Destroy the first child (the oldest)
            Destroy(_killFeedContainer.GetChild(0).gameObject);
        }

        // 2. Instantiate and setup the new card at the bottom of the container
        KillFeedCard newCard = Instantiate(_killFeedCardPrefab, _killFeedContainer);
        newCard.Setup(killerName, victimName, _killFeedLifetime);
    }
}
