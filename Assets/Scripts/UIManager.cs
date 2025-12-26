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

    [Header("Navigation")]
    [SerializeField] private Button nextButton;

    private DialogueCell currentCell;

    private void Awake() { Instance = this; }

    public void ShowDialogue(string text, string charName, Sprite charSprite, DialogueDatas.DialogueChoice[] choices, DialogueCell cell)
    {
        dialoguePanel.SetActive(true);
        characterNameText.text = charName;
        characterImage.sprite = charSprite;
        dialogueText.text = text;

        currentCell = cell;

        StopAllCoroutines();
        StartCoroutine(FadeText(text));

        // Supprime anciens boutons
        foreach (Transform child in choicesParent)
            Destroy(child.gameObject);

        // Crée les nouveaux boutons si choix disponibles
        foreach (var choice in choices)
        {
            Button b = Instantiate(choiceButtonPrefab, choicesParent);
            b.GetComponentInChildren<TextMeshProUGUI>().text = choice.choiceText;

            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(() =>
            {
                dialogueText.text = choice.consequenceText;
                // TODO : appliquer effet joueur
                CloseDialogue();
            });            
        }

        nextButton.onClick.RemoveAllListeners();
        nextButton.onClick.AddListener(() =>
        {
            currentCell.ShowNextDialogue();
        });

        // Activer / désactiver bouton Next si pas de choix
        nextButton.gameObject.SetActive(choices.Length == 0);
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
