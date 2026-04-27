using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton (DontDestroyOnLoad) that drives the board save/restore cycle.
///
/// Flow:
///   1. Player lands on an Event cell → MinigameManager.ConfirmMinigame() fires OnMinigameConfirmed.
///   2. BoardSaveManager captures + writes the full board snapshot to disk.
///   3. MinigameManager loads the minigame scene.
///   4. MinigameManager.EndMinigame() reloads the board scene.
///   5. OnSceneLoaded detects HasPendingSave, waits one frame for Start() to finish,
///      then calls BoardStateSerializer.Restore().
/// </summary>
public class BoardSaveManager : MonoBehaviour
{
    public static BoardSaveManager Instance { get; private set; }

    private const string SaveFileName = "board_state.json";

    /// <summary>True while a snapshot is waiting to be applied on the next board load.</summary>
    public bool HasPendingSave { get; private set; }

    /// <summary>
    /// Resources earned during the last minigame, applied on top of the restored save state.
    /// Null when no minigame has been played or after the rewards have been applied.
    /// </summary>
    private MinigameResult pendingMinigameResult;

    /// <summary>
    /// Quick static check for other MonoBehaviours to skip their default initialization
    /// when a board restore is pending (e.g. Pawn.Start, DiceInventoryUI.Start).
    /// </summary>
    public static bool IsRestorePending =>
        Instance != null && Instance.HasPendingSave;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ── MinigameManager wiring ────────────────────────────────────────────────

    /// <summary>
    /// Called every time the board scene finishes loading so we always have a fresh reference.
    /// </summary>
    private void HookMinigameManager()
    {
        if (MinigameManager.Instance == null)
            return;

        // Guard against double-subscription across scene reloads
        MinigameManager.Instance.OnMinigameConfirmed -= OnMinigameConfirmed;
        MinigameManager.Instance.OnMinigameConfirmed += OnMinigameConfirmed;

        MinigameManager.Instance.OnMinigameEnded -= OnMinigameEnded;
        MinigameManager.Instance.OnMinigameEnded += OnMinigameEnded;
    }

    /// <summary>
    /// Fired by MinigameManager right before it loads the minigame scene.
    /// This is the last safe moment to snapshot the board.
    /// </summary>
    private void OnMinigameConfirmed() => SaveBoard();

    /// <summary>
    /// Fired by MinigameManager when the minigame ends, before the board scene reloads.
    /// Stores the result so RestoreAfterStart can apply the earned resources on top of the save.
    /// </summary>
    private void OnMinigameEnded(MinigameData data, MinigameResult result)
    {
        pendingMinigameResult = result;
    }

    // ── Scene lifecycle ───────────────────────────────────────────────────────

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsBoardScene(scene.name))
            return;

        HookMinigameManager();

        if (HasPendingSave)
            StartCoroutine(RestoreAfterStart());
    }

    /// <summary>
    /// Waits until all scene singletons are initialized before restoring.
    /// Uses an active poll with a safety timeout instead of a fixed frame wait,
    /// because Awake() execution order across DontDestroyOnLoad objects is not guaranteed.
    /// </summary>
    private IEnumerator RestoreAfterStart()
    {
        // ── CRITICAL: wait at least one frame so every Start() finishes ──────
        // SceneManager.sceneLoaded fires AFTER Awake but BEFORE Start.
        // Without this yield the restore would run synchronously (all refs are
        // already set in Awake) and then Start() methods (Pawn, DiceInventoryUI,
        // TurnManager) would overwrite the restored state.
        yield return null;

        const float TimeoutSeconds = 5f;
        float elapsed = 0f;

        Board           board;
        Pawn            pawn;
        TurnManager     turns;
        ResourceManager resources;
        DiceInventoryUI dice;

        // Poll each frame until all refs are ready or timeout is reached.
        while (true)
        {
            board     = Board.Instance;
            pawn      = FindFirstObjectByType<Pawn>();
            turns     = TurnManager.Instance;
            resources = ResourceManager.Instance;
            dice      = DiceInventoryUI.Instance;

            // Use the Unity null-check (operator==) to catch destroyed-but-not-GC'd instances
            bool allReady = (board     != null) && board
                         && (pawn      != null) && pawn
                         && (turns     != null) && turns
                         && (resources != null) && resources
                         && (dice      != null) && dice;

            if (allReady)
                break;

            elapsed += Time.unscaledDeltaTime;
            if (elapsed >= TimeoutSeconds)
            {
                Debug.LogError(
                    $"[BoardSaveManager] Restore timed out after {TimeoutSeconds}s. " +
                    $"Null refs → Board:{board == null} Pawn:{pawn == null} " +
                    $"TurnManager:{turns == null} ResourceManager:{resources == null} " +
                    $"DiceInventoryUI:{dice == null}");
                yield break;
            }

            yield return null;
        }

        BoardSaveData data = ReadFromDisk();
        if (data == null)
        {
            Debug.LogError("[BoardSaveManager] Save file missing or corrupt — starting fresh.");
            HasPendingSave = false;
            yield break;
        }

        BoardStateSerializer.Restore(data, board, pawn, turns, resources, dice);

        // Apply resources earned during the minigame on top of the restored state
        if (pendingMinigameResult != null)
        {
            foreach (var kvp in pendingMinigameResult.CollectedResources)
            {
                if (kvp.Key != null && kvp.Value > 0)
                    resources.AddResource(kvp.Key, kvp.Value);
            }

            Debug.Log($"[BoardSaveManager] Applied {pendingMinigameResult.CollectedResources.Count} minigame reward(s).");
            pendingMinigameResult = null;
        }

        HasPendingSave = false;

        Debug.Log("[BoardSaveManager] Board state restored successfully.");
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Captures and persists the current board state. Sets HasPendingSave = true.
    /// Safe to call manually (e.g. from a pause-menu save button).
    /// </summary>
    public void SaveBoard()
    {
        Board           board     = Board.Instance;
        Pawn            pawn      = FindFirstObjectByType<Pawn>();
        TurnManager     turns     = TurnManager.Instance;
        ResourceManager resources = ResourceManager.Instance;
        DiceInventoryUI dice      = DiceInventoryUI.Instance;

        if (board == null || pawn == null || turns == null || resources == null || dice == null)
        {
            Debug.LogWarning("[BoardSaveManager] SaveBoard skipped — one or more scene refs are null.");
            return;
        }

        BoardSaveData data = BoardStateSerializer.Capture(board, pawn, turns, resources, dice);
        WriteToDisk(data);
        HasPendingSave = true;

        Debug.Log($"[BoardSaveManager] Board saved ({data.cells.Count} cells, turn {data.currentTurn}).");
    }

    /// <summary>
    /// Deletes the save file and clears the pending flag.
    /// Call on new game or game-over.
    /// </summary>
    public void DeleteSave()
    {
        string path = SavePath();
        if (File.Exists(path))
            File.Delete(path);

        HasPendingSave = false;
        Debug.Log("[BoardSaveManager] Save deleted.");
    }

    // ── I/O ───────────────────────────────────────────────────────────────────

    private void WriteToDisk(BoardSaveData data) =>
        File.WriteAllText(SavePath(), JsonUtility.ToJson(data));

    private BoardSaveData ReadFromDisk()
    {
        string path = SavePath();
        if (!File.Exists(path))
            return null;

        try   { return JsonUtility.FromJson<BoardSaveData>(File.ReadAllText(path)); }
        catch (System.Exception e)
        {
            Debug.LogError($"[BoardSaveManager] Deserialization failed: {e.Message}");
            return null;
        }
    }

    private static string SavePath() =>
        Path.Combine(Application.persistentDataPath, SaveFileName);

    private static bool IsBoardScene(string sceneName) =>
        sceneName == "Dev_Map";
}

