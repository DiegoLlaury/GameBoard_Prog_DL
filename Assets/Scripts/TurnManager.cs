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
        // 1. Effets de fin de tour sur la case actuelle du joueur (avant la décroissance)
        Cell current = board.GetCell(player.currentX, player.currentY);
        if (current != null)
            current.OnPlayerEndTurn(player);

        // 2. Itérer sur une copie pour éviter une modification de la liste pendant l'itération
        List<IDurable> snapshot = new List<IDurable>(durables);
        foreach (IDurable durable in snapshot)
            durable.OnTurnPassed();

        // 3. Avancer le front de destruction
        board.TryAdvanceDestroyedFront();

        // 4. Vérification de mort après tous les effets
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
