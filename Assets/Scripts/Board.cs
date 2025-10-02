using UnityEngine;

public class Board : MonoBehaviour
{
    [SerializeField] public Cell[] cells;

    public Cell GetCellByNumber(int number)
    {
        return cells[number];
    }
} 
