using System.Collections;
using TMPro;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UI;


public class Dice : MonoBehaviour, IDurable
{
    public int diceResult;
    [SerializeField] private int minResult = 4;
    [SerializeField] private int maxResult = 6;
    [SerializeField] private int animNumber = 30;
    [SerializeField] public Sprite diceHealthyImg;
    [SerializeField] public Sprite diceDecayingImg;
    [SerializeField] public Sprite diceRottenImg;

    private bool isInteractable = true;


    public int durability = 6;
    public EnumDiceState state;

    private void Start()
    {
        TurnManager.Instance.RegisterDurable(this);
        UpdateState();
    }

    public void RollDice(System.Action<int> onDiceFinished)
    {


        diceResult = RollBasedOnState();
        Debug.Log($"Le dé a fait {diceResult}");
        onDiceFinished?.Invoke(diceResult);
    }

    int RollBasedOnState()
    {
        switch (state)
        {
            case EnumDiceState.Healthy:
                return Random.Range(4, 7); // 4–6

            case EnumDiceState.Decaying:
                return Random.Range(2, 5); // 2–4

            case EnumDiceState.Rotten:
                return Random.Range(1, 3); // 1–2

            case EnumDiceState.Reduced:
                return Random.Range(1, 3); // dé brisé mais encore utilisable
        }

        return 1;
    }

    public void OnTurnPassed()
    {
        durability--;
        UpdateState();
    }

    void UpdateState()
    {
        if (durability >= 6)
            state = EnumDiceState.Healthy;
        else if (durability >= 3)
            state = EnumDiceState.Decaying;
        else if (durability >= 1)
            state = EnumDiceState.Rotten;
        else
            state = EnumDiceState.Reduced;

        Debug.Log($"Dé → {state} ({durability})");
    }
}
