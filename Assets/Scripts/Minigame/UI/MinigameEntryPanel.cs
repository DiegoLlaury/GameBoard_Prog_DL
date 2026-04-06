using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Entry panel shown on the board scene when a minigame is triggered.
/// Displays the minigame info and lets the player confirm or cancel.
/// Locks the Pawn while visible, following the same pattern as UIManager dialogues.
/// Attach this to the entry panel Canvas/Panel GameObject in Dev_Map.
/// </summary>
public class MinigameEntryPanel : MonoBehaviour
{
    [Header("Panel Root")]
    [SerializeField] private GameObject panelRoot;

    [Header("Info Display")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Image iconImage;

    [Header("Buttons")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private Pawn pawnRef;

    private void Awake()
    {
        pawnRef = FindFirstObjectByType<Pawn>();

        confirmButton.onClick.AddListener(OnConfirm);
        cancelButton.onClick.AddListener(OnCancel);
    }

    private void Start()
    {
        // Subscribe after all Awake() calls have run so MinigameManager.Instance is guaranteed to exist.
        if (MinigameManager.Instance != null)
            MinigameManager.Instance.OnMinigameRequested += Show;
        else
            Debug.LogWarning("[MinigameEntryPanel] MinigameManager.Instance is null in Start. Panel will not receive minigame requests.");

        // Hide the panel after subscribing so it is never shown at startup.
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (MinigameManager.Instance != null)
            MinigameManager.Instance.OnMinigameRequested -= Show;
    }

    /// <summary>
    /// Populates and shows the panel for the given minigame.
    /// Called automatically via MinigameManager.OnMinigameRequested.
    /// </summary>
    public void Show(MinigameData data)
    {
        titleText.text       = data.displayName;
        descriptionText.text = data.description;
        iconImage.sprite     = data.icon;
        iconImage.gameObject.SetActive(data.icon != null);

        panelRoot.SetActive(true);

        if (pawnRef != null)
            pawnRef.IsDialogueLocked = true;
    }

    private void OnConfirm()
    {
        Hide();
        MinigameManager.Instance?.ConfirmMinigame();
    }

    private void OnCancel()
    {
        Hide();
        MinigameManager.Instance?.CancelMinigame();
    }

    private void Hide()
    {
        panelRoot.SetActive(false);

        if (pawnRef != null)
            pawnRef.IsDialogueLocked = false;
    }
}
