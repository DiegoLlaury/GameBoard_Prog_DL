using UnityEngine;

public class Cell : MonoBehaviour, ICellActivable
{
    public virtual void Activate(Pawn CurrentPawn)
    {
        throw new System.NotImplementedException();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
