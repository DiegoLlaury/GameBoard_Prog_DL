using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// In-game HUD for the Zombie Survival minigame.
/// Displays the resource score counter and the player's remaining lives.
/// Subscribes to ZombieSurvivalMinigame events — no polling required.
/// Attach to a Canvas GameObject in Minigame_Map.
/// </summary>
public class MinigameHUD : MonoBehaviour
{
    [Header("Score / Resources")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private string scorePrefix = "Ressources : ";

    [Header("Lives")]
    [SerializeField] private Transform livesContainer;
    [SerializeField] private Sprite lifeFullSprite;
    [SerializeField] private Sprite lifeEmptySprite;

    [Header("Survival Timer (optional)")]
    [SerializeField] private TextMeshProUGUI timerText;

    private List<Image> lifeImages = new List<Image>();
    private int totalLives;
    private int currentScore;
    private float elapsedTime;
    private bool isRunning;

    private MinigamePlayerHealth playerHealth;
    private CollectibleSpawner collectibleSpawner;

    private void Start()
    {
        playerHealth      = FindAnyObjectByType<MinigamePlayerHealth>();
        collectibleSpawner = FindAnyObjectByType<CollectibleSpawner>();

        if (playerHealth != null)
        {
            playerHealth.OnHit      += OnHit;
            playerHealth.OnGameOver += OnGameOver;
        }

        if (collectibleSpawner != null)
            collectibleSpawner.OnCollectiblePickedUp += OnCollectiblePickedUp;

        // Wait one frame so MinigamePlayerHealth.Setup() has been called first
        StartCoroutine(InitAfterSetup());
    }

    private System.Collections.IEnumerator InitAfterSetup()
    {
        yield return null; // let ZombieSurvivalMinigame.Initialize() run first

        if (playerHealth != null)
        {
            totalLives = playerHealth.RemainingHits;
            BuildLivesDisplay(totalLives);
        }

        UpdateScore(0);
        isRunning = true;
    }

    private void Update()
    {
        if (!isRunning || timerText == null) return;

        elapsedTime += Time.deltaTime;
        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHit      -= OnHit;
            playerHealth.OnGameOver -= OnGameOver;
        }

        if (collectibleSpawner != null)
            collectibleSpawner.OnCollectiblePickedUp -= OnCollectiblePickedUp;
    }

    // -------------------------------------------------------------------------
    // Event handlers
    // -------------------------------------------------------------------------

    private void OnHit(int remainingHits)
    {
        UpdateLives(remainingHits);
    }

    private void OnGameOver()
    {
        isRunning = false;
    }

    private void OnCollectiblePickedUp(ResourceData data, int amount)
    {
        currentScore += amount;
        UpdateScore(currentScore);
    }

    // -------------------------------------------------------------------------
    // Display helpers
    // -------------------------------------------------------------------------

    private void UpdateScore(int score)
    {
        if (scoreText != null)
            scoreText.text = scorePrefix + score;
    }

    private void UpdateLives(int remaining)
    {
        for (int i = 0; i < lifeImages.Count; i++)
        {
            bool isFull = i < remaining;
            lifeImages[i].sprite = isFull ? lifeFullSprite : lifeEmptySprite;

            // Dim the empty icon slightly
            lifeImages[i].color = isFull ? Color.white : new Color(1f, 1f, 1f, 0.35f);
        }
    }

    /// <summary>Instantiates life icons based on max lives count.</summary>
    private void BuildLivesDisplay(int count)
    {
        if (livesContainer == null) return;

        foreach (Transform child in livesContainer)
            Destroy(child.gameObject);

        lifeImages.Clear();

        for (int i = 0; i < count; i++)
        {
            GameObject obj = new GameObject($"Life_{i}", typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(livesContainer, false);

            Image img = obj.GetComponent<Image>();
            img.sprite = lifeFullSprite;
            img.preserveAspect = true;

            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(40f, 40f);

            lifeImages.Add(img);
        }
    }
}
