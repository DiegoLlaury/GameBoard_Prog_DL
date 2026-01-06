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
        diceText.text = dice.durability.ToString();

        switch (dice.state)
        {
            case EnumDiceState.Healthy:
                diceImage.sprite = dice.diceHealthyImg;
                break;
            case EnumDiceState.Decaying:
                diceImage.sprite = dice.diceDecayingImg;
                break;
            case EnumDiceState.Rotten:
            case EnumDiceState.Reduced:
                diceImage.sprite = dice.diceRottenImg;
                break;
        }
    }

    public IEnumerator RollAndUse(Pawn pawn)
    {
        // Animation visuelle
        for (int i = 0; i < 20; i++)
        {
            diceText.text = Random.Range(1, 7).ToString();
            yield return new WaitForSeconds(0.03f);
        }

        dice.Roll(result =>
        {
            diceText.text = result.ToString();
            pawn.StartMovement(result);
        });

        // Attendre la fin du déplacement
        while (pawn.IsMoving)
            yield return null;

        DiceInventoryUI.Instance.RemoveDice(this);
    }
}
