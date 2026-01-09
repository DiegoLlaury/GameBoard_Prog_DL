using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class DiceUI : MonoBehaviour
{
    public Dice dice;

    [SerializeField] private Image diceImage;
    [SerializeField] private TextMeshProUGUI diceText;
    [SerializeField] private Button button;

    public void Init(Dice linkedDice)
    {
        dice = linkedDice;
        Refresh();

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        if (!TurnManager.Instance.CanRollDice())
            return;

        DiceInventoryUI.Instance.UseDice(this);
    }

    public void Refresh()
    {
        diceText.text = dice.durability <= 0 ? "X" : dice.durability.ToString();
        button.interactable = true;

        switch (dice.state)
        {
            case EnumDiceState.Healthy:
                diceImage.sprite = dice.diceHealthyImg;
                break;
            case EnumDiceState.Decaying:
                diceImage.sprite = dice.diceDecayingImg;
                break;
            case EnumDiceState.Rotten:
                diceImage.sprite = dice.diceRottenImg;
                break;
            case EnumDiceState.Reduced:
                diceImage.sprite = dice.diceRottenImg;
                break;
        }
    }

    public IEnumerator RollAndUse(Pawn pawn)
    {

        dice.RollDice(result =>
        {
            pawn.StartMovement(result);
        });

        // Attendre la fin du déplacement
        while (pawn.isMoving)
            yield return null;

        Refresh();
    }
}
