using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using System.Collections;

public class Pawn : MonoBehaviour
{

    [SerializeField] private Board board;
    [SerializeField] private PlayerDatas playerDatas;
    [SerializeField] private Dice dice;
    [SerializeField] private InputAction inputAction;

    private bool isMoving = false;

    private void Start()
    {
        playerDatas.cellNumber = 0;
        Transform newPos = board.GetCellByNumber(playerDatas.cellNumber).transform;
        transform.position = newPos.position;
        transform.rotation = newPos.rotation;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
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
        StartCoroutine(MoveStepByStep(dice.diceResult));
    }

    private IEnumerator MoveStepByStep(int steps)
    {
        isMoving = true;

        for (int i = 0; i < steps; i++)
        {
            // avancer d'une case
            playerDatas.cellNumber = board.GetNextCellToMove(playerDatas.cellNumber);
            Transform targetCell = board.GetCellByNumber(playerDatas.cellNumber).transform;

            // animation vers la case
            yield return StartCoroutine(MoveToPosition(targetCell.position, 0.3f));
            transform.rotation = targetCell.rotation;

            // petite pause entre chaque case
            yield return new WaitForSeconds(0.3f);
        }

        isMoving = false;
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
}
