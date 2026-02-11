using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Dialogue UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private Image characterImage;
    [SerializeField] private TextMeshProUGUI dialogueText;

    [Header("Choices")]
    [SerializeField] private Button choiceButtonPrefab;  
    [SerializeField] private Transform choicesParent;
    private bool waitingForClose = false;

    [Header("Navigation")]
    [SerializeField] private Button nextButton;

    [Header("Ressource")]
    [SerializeField] private TextMeshProUGUI fleshNumberText;
    [SerializeField] private ResourceData fleshResource;

    private Cell currentCell;

    private void Awake() { Instance = this; }

    private void Start()
    {
        // Initialisation à l'ouverture du jeu
        RefreshFlesh();

        // Abonnement à l'event
        ResourceManager.Instance.OnResourceChanged += OnResourceChanged;
    }

    private void OnDestroy()
    {
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.OnResourceChanged -= OnResourceChanged;
    }

    private void OnResourceChanged(ResourceData data, int newAmount)
    {
        if (data == fleshResource)
            UpdateFlesh(newAmount);
    }

    private void RefreshFlesh()
    {
        if (ResourceManager.Instance == null)
        {
            Debug.LogError("ResourceManager.Instance est NULL");
            return;
        }

        if (fleshResource == null)
        {
            Debug.LogError("FleshResource n'est pas assigné dans l'UIManager");
            return;
        }

        int amount = ResourceManager.Instance.GetResource(fleshResource);
        UpdateFlesh(amount);
    }

    public void UpdateFlesh(int currentFleshNumber)
    {
        fleshNumberText.text = currentFleshNumber.ToString();

        Debug.Log(fleshNumberText);
    }

    public void ShowDialogue(string text, string charName, Sprite charSprite, DialogueDatas.DialogueChoice[] choices, Cell cell)
    {
        StopAllCoroutines();

        dialoguePanel.SetActive(true);
        characterNameText.text = charName;
        characterImage.sprite = charSprite;
        dialogueText.text = text;

        currentCell = cell;


        foreach (Transform child in choicesParent)
            Destroy(child.gameObject);

        // Gestion texte
        if (!string.IsNullOrEmpty(text))
        {
            dialogueText.gameObject.SetActive(true);
            StartCoroutine(FadeText(text));
        }
        else
        {
            dialogueText.gameObject.SetActive(false);
        }

        // Gestion choix
        bool hasChoices = choices != null && choices.Length > 0;

        choicesParent.gameObject.SetActive(hasChoices);
        nextButton.gameObject.SetActive(!hasChoices);

        if (hasChoices)
        {
            foreach (var choice in choices)
            {
                Button b = Instantiate(choiceButtonPrefab, choicesParent);
                b.GetComponentInChildren<TextMeshProUGUI>().text = choice.choiceText;

                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(() =>
                {
                    choicesParent.gameObject.SetActive(false);
                    dialogueText.gameObject.SetActive(true);
                    dialogueText.text = choice.consequenceText;

                    if (choice.effectOnPlayer != 0)
                    {
                        Pawn pawn = FindFirstObjectByType<Pawn>();
                        if (pawn != null)
                        {
                            pawn.movementPoints += choice.effectOnPlayer;
                        }
                    }

                    waitingForClose = true;
                    nextButton.gameObject.SetActive(true);
                });

            }
        }

        // Next bouton
        nextButton.onClick.RemoveAllListeners();
        nextButton.onClick.AddListener(() =>
        {
            if (waitingForClose)
            {
                waitingForClose = false;
                CloseDialogue();
                return;
            }

            currentCell.ShowNextDialogue();
        });
    }

    private IEnumerator FadeText(string fullText)
    {
        dialogueText.text = "";
        foreach (char c in fullText)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(0.02f); // vitesse du fade/typing
        }
    }    

    public void CloseDialogue()
    {
        dialoguePanel.SetActive(false);
    }
}
