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
        // Skip adding the default starting dice when a save restore is pending —
        // BoardSaveManager will recreate the correct dice inventory.
        if (BoardSaveManager.IsRestorePending)
            return;

        AddStartingDice();
    }

    void AddStartingDice()
    {
        Dice dice = Instantiate(startingDicePrefab);
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

    /// <summary>Returns a read-only snapshot of the current dice slots for save purposes.</summary>
    public IReadOnlyList<DiceUI> GetDiceSlots() => diceUIs;

    /// <summary>Returns the starting dice prefab so the restore system can recreate dice.</summary>
    public Dice GetStartingDicePrefab() => startingDicePrefab;

    /// <summary>Destroys all current dice UI slots. Used by the restore system before re-adding saved dice.</summary>
    public void ClearAllDice()
    {
        foreach (DiceUI ui in diceUIs)
        {
            if (ui != null)
                Destroy(ui.gameObject);
        }
        diceUIs.Clear();
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
