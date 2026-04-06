using UnityEngine;

/// <summary>
/// Main controller for the Zombie Survival minigame.
/// Implements IMinigame for framework integration.
/// Manages collectible spawning, player health, difficulty scaling,
/// and communicates results back to MinigameManager.
/// </summary>
public class ZombieSurvivalMinigame : MonoBehaviour, IMinigame
{
    [Header("Config")]
    [SerializeField] private ZombieSurvivalConfig config;

    [Header("Scene References")]
    [SerializeField] private MinigamePlayerHealth playerHealth;
    [SerializeField] private CollectibleSpawner collectibleSpawner;
    [SerializeField] private DifficultyScaler difficultyScaler;
    [SerializeField] private ZombieIAController[] zombies;
    [SerializeField] private SightPerception[] sightPerceptions;

    private MinigameResult result;
    private float elapsedTime;
    private bool isRunning;

    private void Start()
    {
        Initialize();
        StartGame();
    }

    /// <summary>Initializes all subsystems.</summary>
    public void Initialize()
    {
        result = new MinigameResult();
        elapsedTime = 0f;

        // Auto-find components if not assigned
        if (playerHealth == null)
            playerHealth = FindAnyObjectByType<MinigamePlayerHealth>();

        if (collectibleSpawner == null)
            collectibleSpawner = FindAnyObjectByType<CollectibleSpawner>();

        if (difficultyScaler == null)
            difficultyScaler = FindAnyObjectByType<DifficultyScaler>();

        if (zombies == null || zombies.Length == 0)
            zombies = FindObjectsByType<ZombieIAController>(FindObjectsSortMode.None);

        if (sightPerceptions == null || sightPerceptions.Length == 0)
            sightPerceptions = FindObjectsByType<SightPerception>(FindObjectsSortMode.None);

        // Setup player health
        if (playerHealth != null && config != null)
        {
            playerHealth.Setup(config.maxHits, config.invincibilityDuration);
            playerHealth.OnHit += OnPlayerHit;
            playerHealth.OnGameOver += OnPlayerGameOver;
        }

        // Setup collectible spawner
        if (collectibleSpawner != null && config != null)
        {
            collectibleSpawner.Setup(config);
            collectibleSpawner.OnCollectiblePickedUp += OnCollectiblePickedUp;
        }

        // Setup difficulty scaler
        if (difficultyScaler != null && config != null)
            difficultyScaler.Setup(config, zombies, sightPerceptions);

        // Subscribe to zombie attack events
        foreach (var zombie in zombies)
        {
            if (zombie != null)
                zombie.OnAttackLanded += OnZombieAttackLanded;
        }
    }

    /// <summary>Starts the gameplay loop.</summary>
    public void StartGame()
    {
        isRunning = true;
        Debug.Log("[ZombieSurvival] Minigame started!");
    }

    /// <summary>Ends the gameplay loop and reports results.</summary>
    public void EndGame()
    {
        if (!isRunning) return;

        isRunning = false;
        result.SurvivalTime = elapsedTime;

        Debug.Log($"[ZombieSurvival] Game Over! Score: {result.Score}, Time: {elapsedTime:F1}s");

        // Stop subsystems
        if (difficultyScaler != null)
            difficultyScaler.Stop();

        if (collectibleSpawner != null)
            collectibleSpawner.Cleanup();

        // Unsubscribe from events
        if (playerHealth != null)
        {
            playerHealth.OnHit -= OnPlayerHit;
            playerHealth.OnGameOver -= OnPlayerGameOver;
        }

        if (collectibleSpawner != null)
            collectibleSpawner.OnCollectiblePickedUp -= OnCollectiblePickedUp;

        foreach (var zombie in zombies)
        {
            if (zombie != null)
                zombie.OnAttackLanded -= OnZombieAttackLanded;
        }

        // Report results to MinigameManager
        if (MinigameManager.Instance != null)
            MinigameManager.Instance.EndMinigame(result);
    }

    /// <summary>Returns the current minigame result.</summary>
    public MinigameResult GetResult() => result;

    private void Update()
    {
        if (!isRunning) return;
        elapsedTime += Time.deltaTime;
    }

    private void OnZombieAttackLanded(Transform target)
    {
        if (playerHealth != null)
            playerHealth.TakeHit();
    }

    private void OnPlayerHit(int remainingHits)
    {
        result.HitsTaken++;
        Debug.Log($"[ZombieSurvival] Player hit! Remaining: {remainingHits}");
    }

    private void OnPlayerGameOver()
    {
        Debug.Log("[ZombieSurvival] Player eliminated!");
        EndGame();
    }

    private void OnCollectiblePickedUp(ResourceData data, int amount)
    {
        result.Score += amount;
        result.TrackResource(data, amount);
        Debug.Log($"[ZombieSurvival] Collected {amount}x {data.displayName}. Total score: {result.Score}");
    }

    private void OnDestroy()
    {
        // Safety unsubscribe
        if (playerHealth != null)
        {
            playerHealth.OnHit -= OnPlayerHit;
            playerHealth.OnGameOver -= OnPlayerGameOver;
        }

        if (collectibleSpawner != null)
            collectibleSpawner.OnCollectiblePickedUp -= OnCollectiblePickedUp;

        if (zombies != null)
        {
            foreach (var zombie in zombies)
            {
                if (zombie != null)
                    zombie.OnAttackLanded -= OnZombieAttackLanded;
            }
        }
    }
}
