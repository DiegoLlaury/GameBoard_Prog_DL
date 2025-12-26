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

    private bool isMoving = false;

    private void Start()
    {
        if (board == null)
        {
            Debug.LogError("Board non assigné dans le Pawn");
            return;
        }

        if (board.cells == null)
        {
            Debug.LogError("La grille n'est pas encore générée");
            return;
        }

        currentX = board.rows / 2;
        currentY = board.referenceColumn % board.columns;

        Cell startCell = board.GetCell(currentX, currentY);
        currentWorldColumn = startCell.WorldColumnIndex;

        transform.position = startCell.transform.position;
        transform.rotation = startCell.transform.rotation;
    }

    private void Update()
    {
        if (isMoving)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryClickCell();
        }

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            ThrowDice();
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

    public void ThrowDice()
    {
        if (!TurnManager.Instance.CanRollDice())
            return;

        TurnManager.Instance.DiceRolled();

        movementPoints = 0;
        StartCoroutine(dice.RollDice(OnDiceFinished));       
    }

    private void OnDiceFinished(int result)
    {
        movementPoints = result;
        ShowMovementRange();
    }

    private void ShowMovementRange()
    {
        ClearMovementRange();

        foreach (Cell cell in board.AllCells)
        {
            if (!cell.isWalkable)
                continue;

            if (cell.WorldColumnIndex < currentWorldColumn - 1)
                continue;

            int worldDistance = cell.WorldColumnIndex - currentWorldColumn;

            if (worldDistance > movementPoints)
                continue;

            List<Cell> path = board.GetPath(currentX, currentY, cell.gridX, cell.gridY);

            Debug.Log($"Checking cell ({cell.gridX},{cell.gridY}) walkable={cell.isWalkable} pathCount={path.Count}");

            if (path.Count > 0 && path.Count <= movementPoints || cell.contentType == ECellType.Dialogue)
            {
                cell.SetMoveRange(true);
                highlightedCells.Add(cell);
            }
        }
    }

    private void ClearMovementRange()
    {
        foreach (Cell cell in highlightedCells)
            cell.SetMoveRange(false);

        highlightedCells.Clear();
    }

    public void MoveToCell(Cell targetCell)
    {
        if (isMoving) return;

        List<Cell> path = board.GetPath(currentX, currentY, targetCell.gridX, targetCell.gridY);

        if (path.Count == 0 || path.Count > movementPoints)
            return;


        ClearMovementRange();
        StartCoroutine(MoveAlongPath(path));
        
    }

    private IEnumerator MoveAlongPath(List<Cell> path)
    {
        isMoving = true;

        foreach (Cell cell in path)
        {
            yield return StartCoroutine(MoveToPosition(cell.transform.position, 0.25f));

            currentX = cell.gridX;
            currentY = cell.gridY;
            currentWorldColumn = cell.WorldColumnIndex;

            movementPoints--;
            yield return new WaitForSeconds(0.1f);
        }

        
        ActivateCell();
        Board.Instance.SetPlayerProgress(currentWorldColumn);

        isMoving = false;
        dice.TextUpdate(movementPoints);

        if (movementPoints > 0)
            ShowMovementRange();

        if (movementPoints <= 0)
        {
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

    public void MovementPointUsed()
    {
        dice.TextUpdate(movementPoints);
    }
}
