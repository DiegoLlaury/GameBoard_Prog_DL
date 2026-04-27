using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD for the Zombie Race minigame.
/// Displays a countdown timer and a result banner (victory / time's up).
/// Subscribes to ZombieRaceMinigame events — no polling.
/// Attach to a Canvas GameObject in the Zombie Race scene.
/// </summary>
public class ZombieRaceHUD : MonoBehaviour
{
    [Header("Timer")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private Color timerNormalColor  = Color.white;
    [SerializeField] private Color timerUrgentColor  = Color.red;
    [Tooltip("Seconds remaining below which the timer turns red.")]
    [SerializeField] private float urgentThreshold   = 10f;

    [Header("Result Banner")]
    [SerializeField] private GameObject resultBanner;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private string victoryMessage   = "ARRIVÉE !";
    [SerializeField] private string timeoutMessage   = "TEMPS ÉCOULÉ !";

    [Header("Countdown Before Start")]
    [SerializeField] private TextMeshProUGUI countdownText;

    private ZombieRaceMinigame minigame;
    private float timeLimit;
    private bool isRunning;

    private void Start()
    {
        minigame = FindAnyObjectByType<ZombieRaceMinigame>();

        if (minigame != null)
        {
            minigame.OnRaceStarted   += HandleRaceStarted;
            minigame.OnTimerUpdated  += HandleTimerUpdated;
            minigame.OnPlayerWon     += HandleVictory;
            minigame.OnPlayerTimeout += HandleTimeout;
        }

        if (resultBanner != null) resultBanner.SetActive(false);
        if (countdownText != null) countdownText.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (minigame == null) return;

        minigame.OnRaceStarted   -= HandleRaceStarted;
        minigame.OnTimerUpdated  -= HandleTimerUpdated;
        minigame.OnPlayerWon     -= HandleVictory;
        minigame.OnPlayerTimeout -= HandleTimeout;
    }

    // -------------------------------------------------------------------------
    // Event handlers
    // -------------------------------------------------------------------------

    private void HandleRaceStarted(float limit)
    {
        timeLimit  = limit;
        isRunning  = true;

        if (countdownText != null) countdownText.gameObject.SetActive(false);
        RefreshTimer(limit);
    }

    private void HandleTimerUpdated(float remaining)
    {
        if (!isRunning) return;
        RefreshTimer(remaining);
    }

    private void HandleVictory(float remaining)
    {
        isRunning = false;
        ShowBanner(victoryMessage + $"\n+{Mathf.FloorToInt(remaining)}s restantes");
    }

    private void HandleTimeout()
    {
        isRunning = false;
        ShowBanner(timeoutMessage);
    }

    // -------------------------------------------------------------------------
    // Display helpers
    // -------------------------------------------------------------------------

    private void RefreshTimer(float remaining)
    {
        if (timerText == null) return;

        remaining = Mathf.Max(0f, remaining);
        int minutes = Mathf.FloorToInt(remaining / 60f);
        int seconds = Mathf.FloorToInt(remaining % 60f);
        timerText.text  = $"{minutes:00}:{seconds:00}";
        timerText.color = remaining <= urgentThreshold ? timerUrgentColor : timerNormalColor;
    }

    private void ShowBanner(string message)
    {
        if (resultBanner != null) resultBanner.SetActive(true);
        if (resultText   != null) resultText.text = message;
    }

    /// <summary>
    /// Displays a pre-race countdown string (e.g. "3", "2", "1", "GO !").
    /// Called by ZombieRaceMinigame during its countdown coroutine.
    /// </summary>
    public void ShowCountdown(string text)
    {
        if (countdownText == null) return;
        countdownText.gameObject.SetActive(true);
        countdownText.text = text;
    }

    /// <summary>Hides the pre-race countdown text.</summary>
    public void HideCountdown()
    {
        if (countdownText != null)
            countdownText.gameObject.SetActive(false);
    }
}
