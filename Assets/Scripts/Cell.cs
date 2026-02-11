using System.Collections.Generic;
using System.Security.Cryptography;
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

    [SerializeField] private ResourceData rottenFlesh;
    private bool eventTriggered = false;

    private Color originalColor;
    [SerializeField] private float colorVariationStrength = 0.12f;
    private Color colorVariation;

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

        if (activeRenderer != null)
        {
            originalColor = activeRenderer.material.color;
        }

        colorVariation = new Color(
         Random.Range(-colorVariationStrength, colorVariationStrength),
         Random.Range(-colorVariationStrength, colorVariationStrength),
         Random.Range(-colorVariationStrength, colorVariationStrength),
         0f
        );
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
                infection += 2;

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
                break;

            case ECellState.Necrosed:
                CurrentPawn.movementPoints -= 2;
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
        if (dialogueData == null || dialogueData.dialogues.Length == 0)
            return;

        if (dialogueFinished && dialogueData.choices != null && dialogueData.choices.Length > 0)
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

        bool isLastLine = dialogueIndex >= dialogueData.dialogues.Length - 1;

        if (!isLastLine)
        {
            UIManager.Instance.ShowDialogue(
                dialogueData.dialogues[dialogueIndex],
                dialogueData.characterName,
                dialogueData.characterImage,
                new DialogueDatas.DialogueChoice[0],
                this
            );

            dialogueIndex++;
        }
        else
        {
            UIManager.Instance.ShowDialogue(
                "",
                dialogueData.characterName,
                dialogueData.characterImage,
                dialogueData.choices,
                this
            );

            dialogueFinished = true;
        }
    }

    public void OnTurnPassed()
    {
        Board board = Board.Instance;

        if (board.goldenPath.TryGetValue(WorldColumnIndex, out int goldenRow))
        {
            if (gridX == goldenRow && state != ECellState.Destroyed)
            {
                return;
            }
        }

        int cellProgress = WorldColumnIndex;
        int playerProgress = board.PlayerProgressY;

        int decay = 0;

        if (cellProgress < playerProgress)
        {
            int distanceBehind = playerProgress - cellProgress;

            float t = Mathf.Clamp01((float)distanceBehind / board.MaxDecayDistance);
            float decayChance = Mathf.Lerp(board.MinDecayChance, board.MaxDecayChance, t);

            if (Random.value < decayChance)
                decay = 1;
        }
        else if (cellProgress > playerProgress && cellProgress <= playerProgress + board.MaxAheadDistance)
        {
            int distanceAhead = cellProgress - playerProgress;
            float t = Mathf.Clamp01((float)distanceAhead / board.MaxAheadDistance);

            float aheadChance = Mathf.Lerp(board.MaxAheadDecayChance, board.MinAheadDecayChance, t);

            if (Random.value < aheadChance)
                decay = 1;
        }

        decay += GetNeighborInfection();

        if (state == ECellState.Decaying && Random.value < board.DecayingBoostChance)
            decay++;

        if (state == ECellState.Necrosed && Random.value < board.NecrosedBoostChance)
            decay++;

        if (decay <= 0)
            return;

        durability -= decay;
        durability = Mathf.Clamp(durability, 0, 6);

        UpdateState();
    }


    public void OnPlayerEndTurn(Pawn pawn)
    {
        TriggerCellEffects(pawn);
    }

    void TriggerCellEffects(Pawn pawn)
    {
        // EVENT (une seule fois)
        if (contentType == ECellType.Event && !eventTriggered)
        {
            TriggerRandomEvent();
            eventTriggered = true;
        }

        // RESSOURCE sur cases pourries
        CollectRottenResource();
    }

    void CollectRottenResource()
    {
        if (rottenFlesh == null)
            return;

        int amount = 0;

        if (state == ECellState.Decaying)
            amount = Random.Range(1, 3);
        else if (state == ECellState.Necrosed)
            amount = Random.Range(2, 5);

        if (amount > 0)
        {
            ResourceManager.Instance.AddResource(rottenFlesh, amount);
            UIManager.Instance.UpdateFlesh(ResourceManager.Instance.GetResource(rottenFlesh));

            Debug.Log($"+{amount} Rotten Flesh");
        }
    }

    void TriggerRandomEvent()
    {
        if (rottenFlesh == null)
            return;

        int roll = Random.Range(0, 3);

        switch (roll)
        {
            case 0:
                GainFlesh(Random.Range(2, 5));
                Debug.Log("Event : chair récupérée");
                break;

            case 1:
                GainFlesh(Random.Range(1, 3));
                Debug.Log("Event : récupération faible");
                break;

            case 2:
                Debug.Log("Event : rien ne se passe...");
                break;
        }
    }

    void GainFlesh(int amount)
    {
        ResourceManager.Instance.AddResource(rottenFlesh, amount);
        UIManager.Instance.UpdateFlesh(
            ResourceManager.Instance.GetResource(rottenFlesh)
        );
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

            case ECellType.Event:
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
        if (contentType == ECellType.Event) 
            return;

        Color targetColor = Color.white;
        float intensity = 0.35f;

        switch (state)
        {
            case ECellState.Healthy:
                targetColor = new Color(0.85f, 0.75f, 0.6f);
                intensity = 0.5f; // aucune teinte
                break;

            case ECellState.Decaying:
                targetColor = new Color(0.2f, 1, 0.2f); // vert doux
                intensity = 0.55f;
                break;

            case ECellState.Necrosed:
                targetColor = new Color(0.1f, 0.1f, 0.1f); // noir doux
                intensity = 0.90f;
                break;
        }

        if (isInMoveRange)
        {
            targetColor = new Color(1f, 0.2f, 0.2f);
            intensity = 0.4f;
        }

        Color baseWithVariation = originalColor + colorVariation;
        activeRenderer.material.color =
            Color.Lerp(baseWithVariation, targetColor, intensity);
    }

    public void SetMoveRange(bool value)
    {
        isInMoveRange = value && isWalkable; 
        UpdateVisual();
    }
}
