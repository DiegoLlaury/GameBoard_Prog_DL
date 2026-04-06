using UnityEngine;
using System.Collections.Generic;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;
    [SerializeField] private Board board;
    [SerializeField] private Pawn player;

    public int currentTurn {  get; private set; } = 0;
    public bool diceUsedThisTurn { get; set; } = false;

    private List<IDurable> durables = new List<IDurable>();

    private void Awake()
    {
        Instance = this;

        // Skip the first automatic StartTurn when a save restore is pending —
        // the saved turn count will be applied by BoardSaveManager.
        if (!BoardSaveManager.IsRestorePending)
            StartTurn();
    }

    public void RegisterDurable(IDurable durable)
    {
        if (durable == null)
            return;

        if (durables.Contains(durable))
            return;

        durables.Add(durable);
        Debug.Log($"Registered durable: {durable.GetType().Name} (Total: {durables.Count})");
    }

    public void UnregisterDurable(IDurable durable)
    {
        durables.Remove(durable);
    }

    public void StartTurn()
    {
        currentTurn++;
        diceUsedThisTurn = false;
        Debug.Log($"Can roll dice ? {!diceUsedThisTurn}");
        Debug.Log($"=== TOUR {currentTurn} ===");
    }

    /// <summary>
    /// Overrides the turn counter directly. Used exclusively by the save/restore system
    /// to resume a session without replaying all previous turns.
    /// </summary>
    public void RestoreTurnCount(int savedTurn)
    {
        currentTurn = savedTurn;
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
        // Cell effects (events, resources) are now triggered by Pawn.MoveAlongPath
        // on every landing, so we only handle decay and destruction checks here.

        Cell current = board.GetCell(player.currentX, player.currentY);

        // Itérer sur une copie pour éviter une modification de la liste pendant l'itération
        List<IDurable> snapshot = new List<IDurable>(durables);
        foreach (IDurable durable in snapshot)
            durable.OnTurnPassed();

        // Avancer le front de destruction
        board.TryAdvanceDestroyedFront();

        // Vérification de mort après tous les effets
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
        player.ForceHideMovementRange();
        Debug.Log("You died");
    }

    public void Win()
    {
        Debug.Log("You win");
    }
}
