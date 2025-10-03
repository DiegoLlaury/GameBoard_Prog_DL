using TMPro;
using UnityEngine;
using System.Collections;


public class Dice : MonoBehaviour
{
    public int diceResult;
    [SerializeField] private int minResult = 1;
    [SerializeField] private int maxResult = 6;
    [SerializeField] private int animNumber = 30;
    [SerializeField] private TextMeshProUGUI diceText;

    public IEnumerator RollDice(System.Action<int> onDiceFinished)
    {
        diceResult = Random.Range(minResult, maxResult + 1);
        yield return StartCoroutine(TextDiceAnimation(animNumber));

        yield return new WaitForSeconds(0.5f);
        Debug.Log($"Le dé a fait {diceResult}");
        onDiceFinished?.Invoke(diceResult);
    }

    private IEnumerator TextDiceAnimation(int NumberRandom)
    {
        for (int i = 0; i < NumberRandom; i++)
        {
            int animNumber = Random.Range(minResult, maxResult + 1);
            diceText.text = animNumber.ToString();

            yield return new WaitForSeconds(0.025f);
        }

        diceText.text = diceResult.ToString();
    }
}
