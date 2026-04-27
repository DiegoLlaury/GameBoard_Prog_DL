using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Main controller for the Zombie Race minigame.
/// Implements IMinigame for framework integration.
///
/// Flow:
///   1. Scene loads → Initialize() wires all subsystems.
///   2. StartGame() plays a 3-2-1-GO countdown, then enables player movement,
///      starts the zombie pursuit and the countdown timer.
///   3. Race ends either when:
///      a. The player enters FinishZone  → victory.
///      b. The timer reaches zero        → timeout / loss.
///   4. EndGame() cleans up and reports results to MinigameManager.
/// </summary>
public class ZombieRaceMinigame : MonoBehaviour, IMinigame
{
    [Header("Config")]
    [SerializeField] private ZombieRaceConfig config;

    [Header("Scene References")]
    [SerializeField] private ZombieRacePlayerController playerController;
    [SerializeField] private FinishZone finishZone;
    [SerializeField] private ZombieIAController[] zombies;
    [SerializeField] private MinigamePlayerHealth playerHealth;
    [SerializeField] private ZombieRaceHUD hud;

    // ── Events consumed by ZombieRaceHUD ─────────────────────────────────────

    /// <summary>Fired when the race begins (after countdown). Passes the time limit.</summary>
    public event Action<float> OnRaceStarted;

    /// <summary>Fired every frame during the race. Passes remaining seconds.</summary>
    public event Action<float> OnTimerUpdated;

    /// <summary>Fired when the player reaches the finish. Passes remaining seconds.</summary>
    public event Action<float> OnPlayerWon;

    /// <summary>Fired when the timer runs out before the player finishes.</summary>
    public event Action OnPlayerTimeout;

    // ── Internal state ────────────────────────────────────────────────────────

    private MinigameResult result;
    private float remainingTime;
    private bool isRunning;

    private const float CountdownDuration = 3f;

    // ── IMinigame ─────────────────────────────────────────────────────────────

    private void Start()
    {
        Initialize();
        StartGame();
    }

    /// <summary>Wires subsystems and auto-finds references if not assigned in the Inspector.</summary>
    public void Initialize()
    {
        result = new MinigameResult();

        if (playerController == null)
            playerController = FindAnyObjectByType<ZombieRacePlayerController>();

        if (finishZone == null)
            finishZone = FindAnyObjectByType<FinishZone>();

        if (playerHealth == null)
            playerHealth = FindAnyObjectByType<MinigamePlayerHealth>();

        if (hud == null)
            hud = FindAnyObjectByType<ZombieRaceHUD>();

        if (zombies == null || zombies.Length == 0)
            zombies = FindObjectsByType<ZombieIAController>(FindObjectsSortMode.None);

        // Player health — one hit = race over (zombie catches you)
        if (playerHealth != null && config != null)
        {
            playerHealth.Setup(1, config != null ? 0f : 1.5f);
            playerHealth.OnGameOver += OnPlayerCaught;
        }

        // Finish zone
        if (finishZone != null)
            finishZone.OnPlayerFinished += OnFinishReached;

        // Player controller
        if (playerController != null && config != null)
            playerController.Setup(config);

        // Keep zombies frozen until countdown ends — activate pursuit mode immediately
        // so they don't try to patrol or look for waypoints
        SetZombiesActive(false);
        foreach (var z in zombies)
        {
            if (z != null) z.EnablePursuitMode();
        }
    }

    /// <summary>Starts the countdown coroutine then kicks off the race.</summary>
    public void StartGame()
    {
        StartCoroutine(CountdownThenRace());
    }

    /// <summary>Ends the race, cleans up and reports to MinigameManager.</summary>
    public void EndGame()
    {
        if (!isRunning) return;

        isRunning = false;

        result.SurvivalTime = (config != null ? config.timeLimit : 60f) - remainingTime;

        SetZombiesActive(false);

        foreach (var z in zombies)
        {
            if (z != null)
            {
                z.OnAttackLanded -= OnZombieAttackLanded;
                z.DisablePursuitMode();
            }
        }

        if (playerController != null)
            playerController.StopMovement();

        if (playerHealth != null)
            playerHealth.OnGameOver -= OnPlayerCaught;

        if (finishZone != null)
            finishZone.OnPlayerFinished -= OnFinishReached;

        if (MinigameManager.Instance != null)
            MinigameManager.Instance.EndMinigame(result);
    }

    /// <summary>Returns the current result snapshot.</summary>
    public MinigameResult GetResult() => result;

    // ── Race loop ─────────────────────────────────────────────────────────────

    private void Update()
    {
        if (!isRunning) return;

        remainingTime -= Time.deltaTime;
        OnTimerUpdated?.Invoke(remainingTime);

        // Ramp zombie speed from start to max over the full time limit
        if (config != null && zombies != null)
        {
            float t = 1f - Mathf.Clamp01(remainingTime / config.timeLimit);
            float speed = Mathf.Lerp(config.zombieStartSpeed, config.zombieMaxSpeed, t);
            foreach (var z in zombies)
            {
                if (z != null) z.SetMoveSpeed(speed);
            }
        }

        if (remainingTime <= 0f)
            OnTimeout();
    }

    // ── Countdown ─────────────────────────────────────────────────────────────

    private IEnumerator CountdownThenRace()
    {
        remainingTime = config != null ? config.timeLimit : 60f;

        string[] steps = { "3", "2", "1", "GO !" };
        foreach (string step in steps)
        {
            if (hud != null) hud.ShowCountdown(step);
            yield return new WaitForSeconds(1f);
        }

        if (hud != null) hud.HideCountdown();

        // Enable movement and zombie pursuit simultaneously
        if (playerController != null)
            playerController.StartMovement();

        foreach (var z in zombies)
        {
            if (z != null) z.OnAttackLanded += OnZombieAttackLanded;
        }

        SetZombiesActive(true);

        isRunning = true;
        OnRaceStarted?.Invoke(remainingTime);
    }

    // ── Outcome handlers ──────────────────────────────────────────────────────

    private void OnFinishReached()
    {
        if (!isRunning) return;

        isRunning = false;

        // Award finish reward
        if (config != null && config.finishReward != null && config.finishRewardAmount > 0)
        {
            ResourceManager.Instance?.AddResource(config.finishReward, config.finishRewardAmount);
            result.TrackResource(config.finishReward, config.finishRewardAmount);
            result.Score += config.finishRewardAmount;
        }

        // Award time bonus
        if (config != null && config.timeBonus != null && config.timeBonusInterval > 0)
        {
            int bonus = Mathf.FloorToInt(remainingTime) / config.timeBonusInterval;
            if (bonus > 0)
            {
                ResourceManager.Instance?.AddResource(config.timeBonus, bonus);
                result.TrackResource(config.timeBonus, bonus);
                result.Score += bonus;
            }
        }

        OnPlayerWon?.Invoke(remainingTime);
        StartCoroutine(EndAfterDelay(2f));
    }

    private void OnTimeout()
    {
        isRunning = false;
        remainingTime = 0f;
        OnPlayerTimeout?.Invoke();
        StartCoroutine(EndAfterDelay(2f));
    }

    private void OnPlayerCaught()
    {
        if (!isRunning) return;

        isRunning = false;
        result.HitsTaken++;
        OnPlayerTimeout?.Invoke(); // reuse the "failed" banner
        StartCoroutine(EndAfterDelay(2f));
    }

    private void OnZombieAttackLanded(Transform target)
    {
        if (playerHealth != null)
            playerHealth.TakeHit();
    }

    /// <summary>Waits for the result banner to be visible before handing back to MinigameManager.</summary>
    private IEnumerator EndAfterDelay(float delay)
    {
        SetZombiesActive(false);
        if (playerController != null) playerController.StopMovement();

        yield return new WaitForSeconds(delay);
        EndGame();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SetZombiesActive(bool active)
    {
        foreach (var z in zombies)
        {
            if (z != null) z.enabled = active;
        }
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.OnGameOver -= OnPlayerCaught;

        if (finishZone != null)
            finishZone.OnPlayerFinished -= OnFinishReached;

        foreach (var z in zombies)
        {
            if (z != null)
            {
                z.OnAttackLanded -= OnZombieAttackLanded;
                z.DisablePursuitMode();
            }
        }
    }
}
