using UnityEngine;

public class DialogueCell : Cell
{
    public DialogueDatas dialogueData;

    private int dialogueIndex = 0;
    private bool dialogueFinished = false;

    protected override void UpdateVisual()
    {

        // Appelle la logique de Cell (OBLIGATOIRE)
        base.UpdateVisual();

        // Si tu veux une apparence spécifique dialogue
        if (activeRenderer == null)
            return;

        if (isInMoveRange)
            activeRenderer.material.color = Color.red;
    }

    public override void Activate(Pawn CurrentPawn)
    {

        if (dialogueData == null || dialogueData.dialogues.Length == 0)
            return;

        if (dialogueFinished)
        {
            UIManager.Instance.ShowDialogue(dialogueData.dialogues[dialogueData.dialogues.Length - 1],
                                            dialogueData.characterName,
                                            dialogueData.characterImage,
                                            new DialogueDatas.DialogueChoice[0],
                                            this);
            return;
        }

        ShowNextDialogue();
    }

    public void ShowNextDialogue()
    {
        if (dialogueIndex >= dialogueData.dialogues.Length)
        {
            dialogueFinished = true;
            dialogueIndex = dialogueData.dialogues.Length - 1;
            UIManager.Instance.CloseDialogue();
            return;
        }

        UIManager.Instance.ShowDialogue(dialogueData.dialogues[dialogueIndex],
                                       dialogueData.characterName,
                                       dialogueData.characterImage,
                                       dialogueData.choices,
                                       this);

        dialogueIndex++;
    }
}
