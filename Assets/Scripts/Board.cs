using UnityEngine;

public class Board : MonoBehaviour
{
    [SerializeField] public Cell[] cells;

    public Cell GetCellByNumber(int number)
    {
        return cells[number];
    }

    public int GetNextCellToMove(int cellNumber)
    {      
        return (cellNumber + 1) % cells.Length;
    }
} 
