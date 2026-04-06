using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton manager that orchestrates minigame lifecycle.
/// Lives on a DontDestroyOnLoad object alongside ResourceManager.
/// Two-step flow: RequestMinigame (shows entry panel) → ConfirmMinigame (loads scene).
/// </summary>
public class MinigameManager : MonoBehaviour
{
    public static MinigameManager Instance { get; private set; }

    [Header("Available Minigames")]
    [SerializeField] private MinigameData[] availableMinigames;

    /// <summary>Fired when a minigame is requested but not yet confirmed (shows entry panel).</summary>
    public event Action<MinigameData> OnMinigameRequested;

    /// <summary>
    /// Fired just before the minigame scene is loaded (player pressed Confirm on the entry panel).
    /// BoardSaveManager subscribes to this event to capture the board state at the last safe moment.
    /// </summary>
    public event Action OnMinigameConfirmed;

    /// <summary>Fired when a minigame ends and results are available.</summary>
    public event Action<MinigameData, MinigameResult> OnMinigameEnded;

    /// <summary>The minigame currently pending or in progress.</summary>
    public MinigameData CurrentMinigame { get; private set; }

    /// <summary>True while a minigame scene is active.</summary>
    public bool IsInMinigame { get; private set; }

    private string returnSceneName;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // -------------------------------------------------------------------------
    // Request flow (shows entry panel on board)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Picks a random minigame and fires OnMinigameRequested so the entry panel
    /// can display info before actually loading the scene.
    /// </summary>
    public void RequestRandomMinigame()
    {
        if (IsInMinigame || availableMinigames == null || availableMinigames.Length == 0)
        {
            Debug.LogWarning("[MinigameManager] Cannot request minigame: already in one or none available.");
            return;
        }

        int index = UnityEngine.Random.Range(0, availableMinigames.Length);
        RequestMinigame(availableMinigames[index]);
    }

    /// <summary>
    /// Fires OnMinigameRequested for the given minigame.
    /// The entry panel listens to this event and calls ConfirmMinigame or CancelMinigame.
    /// </summary>
    public void RequestMinigame(MinigameData data)
    {
        if (data == null)
        {
            Debug.LogError("[MinigameManager] Null MinigameData passed to RequestMinigame.");
            return;
        }

        CurrentMinigame = data;
        returnSceneName = SceneManager.GetActiveScene().name;

        Debug.Log($"[MinigameManager] Minigame requested: {data.displayName}");
        OnMinigameRequested?.Invoke(data);
    }

    // -------------------------------------------------------------------------
    // Confirm / Cancel (driven by entry panel buttons)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Confirmed by the entry panel — fires OnMinigameConfirmed then loads the minigame scene.
    /// </summary>
    public void ConfirmMinigame()
    {
        if (CurrentMinigame == null || string.IsNullOrEmpty(CurrentMinigame.sceneName))
        {
            Debug.LogError("[MinigameManager] No pending minigame to confirm.");
            return;
        }

        IsInMinigame = true;
        Debug.Log($"[MinigameManager] Starting: {CurrentMinigame.displayName}");

        // Fire BEFORE the scene switches so listeners (e.g. BoardSaveManager) can still access the board.
        OnMinigameConfirmed?.Invoke();

        StartCoroutine(LoadMinigameScene(CurrentMinigame.sceneName));
    }

    /// <summary>
    /// Cancelled by the entry panel — clears the pending minigame without loading.
    /// </summary>
    public void CancelMinigame()
    {
        Debug.Log("[MinigameManager] Minigame cancelled by player.");
        CurrentMinigame = null;
    }

    // -------------------------------------------------------------------------
    // End (called by the minigame controller)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Called by the active minigame when it ends.
    /// Fires OnMinigameEnded and returns to the board scene.
    /// </summary>
    public void EndMinigame(MinigameResult result)
    {
        if (!IsInMinigame)
        {
            Debug.LogWarning("[MinigameManager] No minigame is active.");
            return;
        }

        Debug.Log($"[MinigameManager] Minigame ended. Score: {result.Score}, Survival: {result.SurvivalTime:F1}s");
        OnMinigameEnded?.Invoke(CurrentMinigame, result);
        StartCoroutine(ReturnToBoard());
    }

    // -------------------------------------------------------------------------
    // Scene loading
    // -------------------------------------------------------------------------

    private IEnumerator LoadMinigameScene(string sceneName)
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        while (op != null && !op.isDone)
            yield return null;
    }

    private IEnumerator ReturnToBoard()
    {
        IsInMinigame = false;
        CurrentMinigame = null;

        if (!string.IsNullOrEmpty(returnSceneName))
        {
            AsyncOperation op = SceneManager.LoadSceneAsync(returnSceneName, LoadSceneMode.Single);
            while (op != null && !op.isDone)
                yield return null;
        }
    }
}
