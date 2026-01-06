using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.Experimental.GraphView.GraphView;

public class Cell : MonoBehaviour, ICellActivable, IDurable
{
    public int durability = 5;
    public ECellState state;

    public int gridX;
    public int gridY;

    public bool isWalkable = true;
    public bool isInMoveRange = false;
    public int WorldColumnIndex { get; private set; }
    public ECellType contentType = ECellType.Normal;

    [SerializeField] private Transform visualRoot;
    protected GameObject currentVisual;

    protected Renderer activeRenderer;

    public DialogueDatas dialogueData;
    public bool dialogueFinished = false;
    public int dialogueIndex = 0;

    private void Start()
    {
        TurnManager.Instance.RegisterDurable(this);
        UpdateState();
    }
    private void Awake()
    {
       
    }

    public void SetVisual(GameObject prefab)
    {
        if (currentVisual != null)
            Destroy(currentVisual);

        if (prefab == null || visualRoot == null)
            return;

        currentVisual = Instantiate(prefab, visualRoot);

        // Reset LOCAL transform (très important)
        currentVisual.transform.localPosition = Vector3.zero;
        currentVisual.transform.localRotation = Quaternion.identity;
        currentVisual.transform.localScale = Vector3.one;

        // Récupère le renderer du prefab
        activeRenderer = currentVisual.GetComponentInChildren<Renderer>();
    }

    public void SetWorldColumn(int worldIndex)
    {
        WorldColumnIndex = worldIndex;
    }

    public List<Cell> GetNeighbors()
    {
        List<Cell> neighbors = new List<Cell>();
        Board board = Board.Instance;

        // Radial intérieur
        Cell c;
        c = board.GetCell(gridX - 1, gridY);
        if (c != null) neighbors.Add(c);

        // Radial extérieur
        c = board.GetCell(gridX + 1, gridY);
        if (c != null) neighbors.Add(c);

        // Circulaire gauche
        c = board.GetCell(gridX, gridY - 1);
        if (c != null) neighbors.Add(c);

        // Circulaire droite
        c = board.GetCell(gridX, gridY + 1);
        if (c != null) neighbors.Add(c);

        return neighbors;
    }

    int GetNeighborInfection()
    {
        int infection = 0;

        foreach (Cell n in GetNeighbors())
        {
            if (n.state == ECellState.Necrosed && Random.value < 0.25f)
                infection++;

            else if (n.state == ECellState.Decaying && Random.value < 0.15f)
                infection++;
        }

        return infection;
    }

    public virtual void Activate(Pawn CurrentPawn)
    {
        if (contentType == ECellType.Dialogue && dialogueData != null)
        {
            ActivateDialogue();
            return;
        }

        switch (state)
        {
            case ECellState.Healthy:
                break;

            case ECellState.Decaying:
                CurrentPawn.movementPoints -= 1;
                CurrentPawn.MovementPointUsed();
                break;

            case ECellState.Necrosed:
                CurrentPawn.movementPoints -= 2;
                CurrentPawn.MovementPointUsed();
                break;
        }
    }

    void ActivateDialogue()
    {
        if (dialogueFinished)
        {
            UIManager.Instance.ShowDialogue(
                dialogueData.dialogues[^1],
                dialogueData.characterName,
                dialogueData.characterImage,
                new DialogueDatas.DialogueChoice[0],
                this
            );
            return;
        }

        ShowNextDialogue();
    }

    public void ShowNextDialogue()
    {
        if (dialogueIndex >= dialogueData.dialogues.Length)
        {
            dialogueFinished = true;
            dialogueIndex = dialogueData.dialogues.Length - 1;
            UIManager.Instance.CloseDialogue();
            return;
        }

        UIManager.Instance.ShowDialogue(
            dialogueData.dialogues[dialogueIndex],
            dialogueData.characterName,
            dialogueData.characterImage,
            dialogueData.choices,
            this
        );

        dialogueIndex++;
    }

    public void OnTurnPassed()
    {
        Board board = Board.Instance;

        int cellProgress = WorldColumnIndex;
        int playerProgress = board.PlayerProgressY;

        int decay = 0;

        if (cellProgress < playerProgress)
        {
            int distanceBehind = playerProgress - cellProgress;

            float t = Mathf.Clamp01((float)distanceBehind / board.MaxDecayDistance);
            float decayChance = Mathf.Lerp(board.MaxDecayChance,board.MinDecayChance,t);

            if (Random.value < decayChance)
                decay = 1;
        }

        else if (cellProgress > playerProgress && cellProgress <= playerProgress + board.MaxAheadDistance)
        {
            int distanceAhead = cellProgress - playerProgress;
            float t = Mathf.Clamp01((float)distanceAhead / board.MaxAheadDistance);

            float aheadChance = Mathf.Lerp(board.MaxAheadDecayChance, board.MinAheadDecayChance, t);

            aheadChance += GetNeighborInfection() * 0.05f;

            if (Random.value < aheadChance)
                decay = 1;
        }

        // --- Infection par voisin ---
        if (decay > 0)
        {
            decay += GetNeighborInfection();
        }

        // --- États internes (amplificateurs doux) ---
        if (state == ECellState.Decaying && Random.value < board.DecayingBoostChance)
        decay++;

        if (state == ECellState.Necrosed && Random.value < board.NecrosedBoostChance)
        decay++;

        if (decay <= 0)
            return; //  cette case survit ce tour

        durability -= decay;
        durability = Mathf.Clamp(durability, 0, 6);

        UpdateState();

    }

    public void ApplyCellType(ECellType type)
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (visualRoot != null && !visualRoot.gameObject.activeSelf)
            visualRoot.gameObject.SetActive(true);

        contentType = type;

        Board board = Board.Instance;
        if (board != null && board.TryGetPrefab(type, out GameObject prefab))
            SetVisual(prefab);

        switch (type)
        {
            case ECellType.Normal:
                durability = Random.Range(5, 6);
                isWalkable = true;
                break;

            case ECellType.Obstacle:
                isWalkable = false;
                break;

            case ECellType.Dialogue:
                durability = 6;
                isWalkable = true;
                break;

            case ECellType.End:
                durability = 999;
                isWalkable = true;
                break;
        }

        UpdateState();    
    }

    public void UpdateState()
    {
        if (durability <= 0)
        {
            state = ECellState.Destroyed;
            isWalkable = false;

            if (visualRoot != null)
                visualRoot.gameObject.SetActive(false);

            return;
        }

        if (durability >= 5)
            state = ECellState.Healthy;
        else if (durability >= 3)
            state = ECellState.Decaying;
        else
            state = ECellState.Necrosed;

        if (activeRenderer != null)
            activeRenderer.enabled = true;

        UpdateVisual();
    }

    protected virtual void UpdateVisual()
    {
        if (activeRenderer == null)
            return;

        Color baseColor;

        switch (state)
        {
            case ECellState.Healthy:
                baseColor = new Color(0.85f, 0.75f, 0.6f); // beige
                break;

            case ECellState.Decaying:
                baseColor = Color.green;
                break;

            case ECellState.Necrosed:
                baseColor = Color.black;
                break;

            default:
                baseColor = Color.white;
                break;
        }

        activeRenderer.material.color =
        isInMoveRange ? Color.red : baseColor;
    }

    public void SetMoveRange(bool value)
    {
        isInMoveRange = value && isWalkable; 
        UpdateVisual();
    }
}
