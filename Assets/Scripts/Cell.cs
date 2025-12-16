using UnityEngine;

public class Cell : MonoBehaviour, ICellActivable
{

    public int gridX;
    public int gridY;

    public bool isWalkable = true;
    public bool isInMoveRange = false;

    Renderer rend;

    public virtual void Activate(Pawn CurrentPawn)
    {
        
    }

    void Awake()
    {
        rend = GetComponent<Renderer>();
    }

    public void SetMoveRange(bool value)
    {
        isInMoveRange = value;
        rend.material.color = value ? Color.red : Color.white;
    }

    void OnMouseDown()
    {
        if (!isInMoveRange) return;

        Pawn player = FindFirstObjectByType<Pawn>();
        player.MoveToCell(this);
    }
}
