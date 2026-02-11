using UnityEngine;
using System.Collections.Generic;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;
    [SerializeField] private Board board;
    [SerializeField] private Pawn player;

    public int currentTurn {  get; private set; } = 0;
    public bool diceUsedThisTurn { get; private set; } = false;

    private List<IDurable> durables = new List<IDurable>();

    private void Awake()
    {
        Instance = this;
        StartTurn();
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
        Debug.Log($"Can roll dice ? {!diceUsedThisTurn}");
        Debug.Log($"=== TOUR {currentTurn} ===");
    }

    public bool CanRollDice()
    {
        Debug.Log("IsWorking");
        return !diceUsedThisTurn;
    }

    public void DiceRolled()
    {
        diceUsedThisTurn = true;
    }

    public void EndTurn()
    {
        foreach (IDurable durable in durables)
            durable.OnTurnPassed();

        board.TryAdvanceDestroyedFront();

        Cell current = board.GetCell(player.currentX, player.currentY);
        if (current != null)
            current.OnPlayerEndTurn(player);

        if (current == null || !current.isWalkable || current.state == ECellState.Destroyed)
        {
            Die();
            return;
        }

        if (player.currentWorldColumn <= board.DestroyedUntil)
        {
            Debug.Log("Caught by decay!");
            Die();
            return;
        }
    }


    public void Die()
    {
        Debug.Log("You died");
    }

    public void Win()
    {
        Debug.Log("You win");
    }
}
