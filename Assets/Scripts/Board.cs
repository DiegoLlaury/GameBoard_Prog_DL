using UnityEngine;

public class Board : MonoBehaviour
{
    [SerializeField] private Cell[] cells;

    public Cell GetCellByNumber(int number)
    {
        return cells[number];
    }
} 
