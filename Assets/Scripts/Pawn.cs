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


    public int currentX;
    public int currentY;

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

        currentX = 0;
        currentY = 0;

        Cell startCell = board.GetCell(currentX, currentY);
        transform.position = startCell.transform.position;
        transform.rotation = startCell.transform.rotation;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && !isMoving)
        {
            ThrowDice();
        }
    }

    public void ThrowDice()
    {
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

        foreach (Cell cell in board.cells)
        {
            int distance = Mathf.Abs(cell.gridX - currentX) + Mathf.Abs(cell.gridY - currentY);

            if (distance <= movementPoints && cell.isWalkable)
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

        int distance = Mathf.Abs(targetCell.gridX - currentX) + Mathf.Abs(targetCell.gridY - currentY);

        if (distance > movementPoints)
            return;


        ClearMovementRange();
        StartCoroutine(MoveAlongPath(targetCell));
        
    }

    private IEnumerator MoveAlongPath(Cell targetCell)
    {
        isMoving = true;

        List<Cell> path = board.GetPath(
            currentX, currentY,
            targetCell.gridX, targetCell.gridY
        );

        foreach (Cell cell in path)
        {
            yield return StartCoroutine(MoveToPosition(cell.transform.position, 0.25f));

            transform.position = cell.transform.position;

            currentX = cell.gridX;
            currentY = cell.gridY;

            movementPoints--;
            yield return new WaitForSeconds(0.1f);
        }

        
        ActivateCell();
        Board.Instance.SetPlayerProgress(currentY);
        isMoving = false;
        dice.TextUpdate(movementPoints);

        if (movementPoints > 0)
            ShowMovementRange();
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
