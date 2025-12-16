using System.Collections;
using TMPro;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;


public class Dice : MonoBehaviour, IDurable
{
    public int diceResult;
    [SerializeField] private int minResult = 4;
    [SerializeField] private int maxResult = 6;
    [SerializeField] private int animNumber = 30;
    [SerializeField] private TextMeshProUGUI diceText;

    public int durability = 5;
    public EnumDiceState state;

    private void Start()
    {
        TurnManager.Instance.RegisterDurable(this);
        UpdateState();
    }

    public IEnumerator RollDice(System.Action<int> onDiceFinished)
    {
        if (!TurnManager.Instance.CanRollDice())
            yield break;

        TurnManager.Instance.DiceRolled();

        yield return StartCoroutine(TextDiceAnimation(animNumber));

        yield return new WaitForSeconds(0.5f);

        diceResult = RollBasedOnState();
        Debug.Log($"Le dé a fait {diceResult}");
        onDiceFinished?.Invoke(diceResult);
    }

    int RollBasedOnState()
    {
        switch (state)
        {
            case EnumDiceState.Healthy:
                return Random.Range(4, 6); // 4–6

            case EnumDiceState.Decaying:
                return Random.Range(2, 5); // 2–4

            case EnumDiceState.Rotten:
                return Random.Range(1, 3); // 1–2

            case EnumDiceState.Reduced:
                return 1; // dé brisé mais encore utilisable
        }

        return 1;
    }

    private IEnumerator TextDiceAnimation(int NumberRandom)
    {
        for (int i = 0; i < NumberRandom; i++)
        {
            int animNumber = Random.Range(minResult, maxResult + 1);
            diceText.text = animNumber.ToString();

            yield return new WaitForSeconds(0.025f);
        }

        diceText.text = diceResult.ToString();
    }

    public void OnTurnPassed()
    {
        durability--;
        UpdateState();
    }

    void UpdateState()
    {
        if (durability >= 5)
            state = EnumDiceState.Healthy;
        else if (durability >= 3)
            state = EnumDiceState.Decaying;
        else if (durability >= 1)
            state = EnumDiceState.Rotten;
        else
            state = EnumDiceState.Reduced;

        Debug.Log($"Dé → {state} ({durability})");
    }

    public void TextUpdate(int movementnumberLeft)
    {
        diceText.text = movementnumberLeft.ToString();
    }
}
