using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;


public class Pawn : MonoBehaviour
{
    [SerializeField] private Board board;
    [SerializeField] private PlayerDatas playerDatas;
    [SerializeField] private Dice dice;
    [SerializeField] private InputAction inputAction;

    public int currentX = 3;
    public int currentY;
    public int currentWorldColumn;

    public int movementPoints;

    List<Cell> highlightedCells = new List<Cell>();

    public bool isMoving = false;
    public bool IsUsingDice { get; private set; }
    public bool IsDialogueLocked { get; set; }


    private Dice currentDice;
    private Dictionary<Cell, BFSNode> reachable;

    private void Start()
    {
        if (board == null)
        {
            Debug.LogError("Board non assign� dans le Pawn");
            return;
        }

        if (board.cells == null)
        {
            Debug.LogError("La grille n'est pas encore g�n�r�e");
            return;
        }

        currentX = board.rows / 2;
        currentY = (board.referenceColumn % board.columns) + 2;

        board.SetSpawnRow(currentX);

        Cell startCell = board.GetCell(currentX, currentY);
        currentWorldColumn = startCell.WorldColumnIndex;

        transform.position = startCell.transform.position;
        transform.rotation = startCell.transform.rotation;
    }

    private void Update()
    {
        if (isMoving || IsUsingDice || IsDialogueLocked)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryClickCell();
        }
    }

    void TryClickCell()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        Cell cell = hit.collider.GetComponentInParent<Cell>();
        if (cell == null)
            return;

        if (!cell.isInMoveRange || !cell.isWalkable)
            return;

        MoveToCell(cell);
    }

    public void UseDice(Dice dice)
    {
        if (IsUsingDice)
            return;

        IsUsingDice = true;
        currentDice = dice;

        movementPoints = 0;

    }

    public void StartMovement(int movement)
    {
        movementPoints = movement;
        ShowMovementRange();
    }

    private void OnDiceFinished(int result)
    {
        movementPoints = result;
        ShowMovementRange();
    }

    private void ShowMovementRange()
    {
        if (movementPoints <= 0)
            return;

        ClearMovementRange();

        reachable = board.GetReachableCells(
            currentX,
            currentY,
            movementPoints
        );

        foreach (var kvp in reachable)
        {
            if (kvp.Value.cost == 0)
                continue;

            kvp.Key.SetMoveRange(true);
            highlightedCells.Add(kvp.Key);
        }
    }

    private void ClearMovementRange()
    {
        foreach (Cell cell in highlightedCells)
            cell.SetMoveRange(false);

        highlightedCells.Clear();
    }

    /// <summary>
    /// Forces the removal of all move-range highlights. Call this on death or turn reset.
    /// </summary>
    public void ForceHideMovementRange()
    {
        ClearMovementRange();
        movementPoints = 0;
        IsUsingDice = false;
    }

    public void MoveToCell(Cell targetCell)
    {
        if (isMoving)
            return;

        if (!reachable.ContainsKey(targetCell))
            return;

        BFSNode targetNode = reachable[targetCell];

        // Utilise le coût BFS réel au lieu du nombre de cases
        if (targetNode.cost > movementPoints)
            return;

        List<Cell> path = board.ReconstructPath(targetNode);

        ClearMovementRange();
        StartCoroutine(MoveAlongPath(path, targetNode.cost));
    }

    private IEnumerator MoveAlongPath(List<Cell> path, int pathCost)
    {
        isMoving = true;

        foreach (Cell cell in path)
        {
            yield return StartCoroutine(MoveToPosition(cell.transform.position, 0.25f));

            currentX = cell.gridX;
            currentY = cell.gridY;
            currentWorldColumn = cell.WorldColumnIndex;

            yield return new WaitForSeconds(0.1f);
        }

        // Déduit le coût réel du déplacement BFS
        movementPoints -= pathCost;

        ActivateCell();
        Board.Instance.SetPlayerProgress(currentWorldColumn);

        isMoving = false;

        if (movementPoints > 0)
            ShowMovementRange();

        if (movementPoints <= 0)
        {
            IsUsingDice = false;

            TurnManager.Instance.EndTurn();
            TurnManager.Instance.StartTurn();
        }
    }

    private IEnumerator MoveToPosition(Vector3 target, float duration)
    {
        Vector3 start = transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // interpolation lisse
            transform.position = Vector3.Lerp(start, target, t);
            yield return null;
        }

        transform.position = target;
  
    }

    private void ActivateCell()
    {
        Cell cell = board.GetCell(currentX, currentY);
        cell.Activate(this);
    }
}
