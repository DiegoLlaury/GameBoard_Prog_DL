using UnityEngine;

public class Dice : MonoBehaviour
{
    private int diceResult;

    public int DiceResult()
    {
        diceResult = Random.Range(1, 6);
        return diceResult;
    }
}
