using System.Data;
using UnityEngine;
using System.Collections.Generic;

public class Board : MonoBehaviour
{
    public int rows = 5;
    public int columns = 10;
    public float cellSize = 1f;
    public float baseRadius = 10f;
    public int referenceColumn = 0;
    public int PlayerProgressY { get; private set; } = 0;


    public Cell[,] cells;
    public GameObject cellPrefab;

    public List<Cell> AllCells {  get; private set; } = new List<Cell>();
    public static Board Instance;

    private void Awake()
    {
        Instance = this;
        GenerateGrid();
        PlayerProgressY = referenceColumn;
    }


    void GenerateGrid()
    {
        cells = new Cell[rows, columns];
        AllCells.Clear();



        float angleStep = 2f * Mathf.PI / columns;
        int referenceRow = rows / 2;

        float referenceRadius = cellSize / angleStep;


        for (int x = 0; x < rows; x++)
        {
            float radius = referenceRadius + (x - referenceRow) * cellSize;

            for (int y = 0; y < columns; y++)
            {
  
                float angle = y * angleStep;

                Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

                Quaternion rot = Quaternion.LookRotation(pos.normalized);

                GameObject cellObj = Instantiate(cellPrefab, pos, rot, transform);

                float scaleFactor = radius / referenceRadius;
                cellObj.transform.localScale = new Vector3(scaleFactor, 1f, 1f);

                Cell cell = cellObj.GetComponent<Cell>();

                cell.gridX = x;
                cell.gridY = y;
                int dist = Mathf.Min(Mathf.Abs(y - referenceColumn),columns - Mathf.Abs(y - referenceColumn));

                cell.durability = Random.Range(4, 5);
                cell.durability = Mathf.Clamp(cell.durability, 1, 6);

                cells[x, y] = cell;
                AllCells.Add(cell);
            }
        }
    }

    public Cell GetCell(int x, int y)
    {
        if (x < 0 || x >= rows)
            return null;

        y = (y + columns) % columns;

        return cells[x, y];
    }

    public List<Cell> GetPath(int startX, int startY, int targetX, int targetY)
    {
        List<Cell> path = new List<Cell>();

        int x = startX;
        int y = startY;

        while (x != targetX || y != targetY)
        {
            if (x < targetX) x++;
            else if (x > targetX) x--;
            //if (y < targetY) y++;
            //if (y > targetY) y--;

            if (y != targetY)
            {
                int rightDist = (targetY - y + columns) % columns;
                int leftDist = (y - targetY + columns) % columns;

                if (rightDist <= leftDist)
                    y = (y + 1) % columns;   // avancer à droite
                else
                    y = (y - 1 + columns) % columns; // avancer à gauche
            }

            Cell nextCell = GetCell(x, y);

            if (nextCell == null || !nextCell.isWalkable)
                break;

            path.Add(nextCell);
        }

        return path;
    }

  

    public int GetNextCellToMove(int cellNumber)
    {      
        return (cellNumber + 1) % cells.Length;
    }

    public void SetPlayerProgress(int y)
    {
        PlayerProgressY = (y + columns) % columns;
    }
} 
