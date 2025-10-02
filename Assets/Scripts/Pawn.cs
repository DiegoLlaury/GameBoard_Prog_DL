using UnityEngine;

public class Pawn : MonoBehaviour
{

    [SerializeField] private Board board;
    [SerializeField] private PlayerDatas playerDatas;
    private void Start()
    {
        MoveToCell();
    }
    private void MoveToCell()
    {
        Transform NewPos = board.GetCellByNumber(0).transform; // TODO : Get cell number
        transform.position = NewPos.position;
        transform.rotation = NewPos.rotation;
    }
}
