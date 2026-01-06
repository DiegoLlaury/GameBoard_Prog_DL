using UnityEngine;
using System.Collections.Generic;

public class DiceInventoryUI : MonoBehaviour
{
    public static DiceInventoryUI Instance;

    [SerializeField] private Transform content;
    [SerializeField] private DiceUI diceUIPrefab;
    [SerializeField] private Dice startingDicePrefab;

    public int maxDice = 5;
    private List<DiceUI> diceUIs = new();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        AddStartingDice();
    }

    void AddStartingDice()
    {
        Dice dice = Instantiate(startingDicePrefab, transform);
        AddDice(dice);
    }

    public bool CanAddDice() => diceUIs.Count < maxDice;

    public void AddDice(Dice dice)
    {
        if (!CanAddDice())
            return;

        DiceUI ui = Instantiate(diceUIPrefab, content);
        ui.Init(dice);

        diceUIs.Add(ui);
    }

    public void RemoveDice(DiceUI ui)
    {
        diceUIs.Remove(ui);
        Destroy(ui.gameObject);
    }

    public void UseDice(DiceUI ui)
    {
        if (!TurnManager.Instance.CanRollDice())
            return;

        TurnManager.Instance.DiceRolled();

        Pawn pawn = FindFirstObjectByType<Pawn>();

        ui.StartCoroutine(ui.RollAndUse(pawn));
    }
}
