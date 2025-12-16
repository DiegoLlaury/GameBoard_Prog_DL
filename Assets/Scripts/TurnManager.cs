using UnityEngine;
using System.Collections.Generic;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;

    public int currentTurn {  get; private set; } = 0;
    public bool diceUsedThisTurn { get; private set; } = false;

    private List<IDurable> durables = new List<IDurable>();

    private void Awake()
    {
        Instance = this;
    }

    public void RegisterDurable(IDurable durable)
    {
        if (!durables.Contains(durable))
        {
            durables.Add(durable);
        }
    }

    public void StartTurn()
    {
        currentTurn++;
        diceUsedThisTurn = false;

        Debug.Log($"=== TOUR {currentTurn} ===");
    }

    public bool CanRollDice()
    {
        return !diceUsedThisTurn;
    }

    public void DiceRolled()
    {
        diceUsedThisTurn = true;
    }

    public void EndTurn()
    {
        foreach(IDurable durable in durables)
            durable.OnTurnPassed();

        StartTurn(); 
    }
}
