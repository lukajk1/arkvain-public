using PurrNet;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FFA_UI : MonoBehaviour
{
    public static FFA_UI Instance { get; private set; }

    [Header("End Canvas")]
    [SerializeField] private Canvas endCanvas;
    [SerializeField] private CanvasGroup endCanvasGroup;
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private TMP_Text winResultText;

    [SerializeField] private float fadeInEndScreenDuration = 2f;

    [SerializeField] private TMP_Text matchStatusText;
    [SerializeField] private Canvas matchStatusCanvas;
    [SerializeField] private Button leaveButton;

    [Header("End Screen Scoreboard")]
    [SerializeField] private Transform scoreboardContainer; 
    [SerializeField] private GameObject playerRowPrefab;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        endCanvas.gameObject.SetActive(false);
        endCanvasGroup.alpha = 0f;

        matchStatusCanvas.gameObject.SetActive(true);
    }

    private void OnEnable()
    {
        if (leaveButton != null) leaveButton.onClick.AddListener(LeaveButtonClicked);
    }

    private void OnDisable()
    {
        if (leaveButton != null) leaveButton.onClick.RemoveListener(LeaveButtonClicked);
    }

    private void OnDestroy()
    {
        PersistentClient.AddToCursorUnlockList(false, this);
    }

    public void UpdateStatus(string message)
    {
        matchStatusText.text = message;
    }
    public void SetRestartCountdown(string message)
    {
        countdownText.text = message;
    }

    public void ShowEndScreen(PlayerID? localPlayerId)
    {
        matchStatusCanvas.gameObject.SetActive(false);
        PersistentClient.AddToCursorUnlockList(true, this);

        endCanvas.gameObject.SetActive(true);
        winResultText.text = IsInTopHalf(localPlayerId) ? "VICTORY" : "DEFEAT";

        BuildScoreboard();

        StartCoroutine(FadeAlpha(0, 1f, fadeInEndScreenDuration));
    }
    public static bool IsInTopHalf(PlayerID? localPlayerId)
    {
        if (MatchSessionManager.Instance == null || !localPlayerId.HasValue) return false;

        var leaderboard = MatchSessionManager.Instance.GetLeaderboard();
        int totalPlayers = leaderboard.Count;
        if (totalPlayers == 0) return false;
        if (totalPlayers == 1) return true;

        int topHalfSize = totalPlayers / 2;

        PlayerID id = localPlayerId.Value; // unwrap before comparison
        int rank = leaderboard.FindIndex(p => p.playerId == id);
        if (rank == -1) return false;

        return rank < topHalfSize;
    }

    private void BuildScoreboard()
    {
        if (scoreboardContainer == null || playerRowPrefab == null || MatchSessionManager.Instance == null)
        {
            Debug.LogError("[FFA_UI] Missing scoreboard references!");
            return;
        }

        // GetLeaderboard() already returns players sorted by score/kills
        var players = MatchSessionManager.Instance.GetLeaderboard();

        foreach (var playerData in players)
        {
            GameObject rowObj = Instantiate(playerRowPrefab, scoreboardContainer);
            ScoreboardRow row = rowObj.GetComponent<ScoreboardRow>();
            if (row != null)
                row.UpdateData(playerData);
            else
                Debug.LogError("[FFA_UI] playerRowPrefab missing ScoreboardRow component!");
        }
    }
    private void LeaveButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LeaveMatch();
        }
    }

    private IEnumerator FadeAlpha(float start, float end, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            endCanvasGroup.alpha = Mathf.Lerp(start, end, elapsed / duration);
            yield return null;
        }

        endCanvasGroup.alpha = end;
    }

}
