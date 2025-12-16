using UnityEngine;

public class Cell : MonoBehaviour, ICellActivable, IDurable
{
    public int durability = 6;
    public ECellState state;

    public int gridX;
    public int gridY;

    public bool isWalkable = true;
    public bool isInMoveRange = false;

    [SerializeField] private Renderer rend;

    private void Start()
    {
        TurnManager.Instance.RegisterDurable(this);
        UpdateState();
    }

    public virtual void Activate(Pawn CurrentPawn)
    {
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

    public void OnTurnPassed()
    {
        int playerY = Board.Instance.PlayerProgressY;
        int columns = Board.Instance.columns;

        // Calcul de la distance derrière et devant le joueur
        int distanceBehind = (playerY - gridY + columns) % columns; // distance dans le sens inverse du joueur
        int distanceAhead = (gridY - playerY + columns) % columns; // distance dans le sens de déplacement du joueur

        // --- Propagation organique derrière le joueur ---
        if (distanceBehind > 0)
        {
            // Plus proche = plus de chance de pourrir
            float chanceBehind = Mathf.Clamp01(1f - distanceBehind * 0.30f);
            if (Random.value < chanceBehind)
                durability--;
        }

        // --- Propagation plus faible devant le joueur ---
        if (distanceAhead > 0)
        {
            // Probabilité plus faible devant
            float chanceAhead = Mathf.Clamp01(0.3f - distanceAhead * 0.1f);
            if (Random.value < chanceAhead)
                durability--;
        }

        // Clamp pour rester entre 0 et 6
        durability = Mathf.Clamp(durability, 0, 6);

        // Met à jour l'état et le rendu visuel
        UpdateState();
    }

    public void UpdateState()
    {
        if (durability <= 0)
        {
            isWalkable = false;
            rend.enabled = false;
        }
        else
        {
            isWalkable = true;
            rend.enabled = true;
        }

        if (durability >= 4)
            state = ECellState.Healthy;
        else if (durability >= 2)
            state = ECellState.Decaying;
        else
            state = ECellState.Necrosed;

        UpdateVisual();
    }

    void UpdateVisual()
    {
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

        if (isInMoveRange)
            rend.material.color = Color.red;
        else
            rend.material.color = baseColor;

        Debug.Log($"Cell [{gridX},{gridY}] -> {state} (durability {durability})");
    }

    void Awake()
    {
        rend = GetComponent<Renderer>();
    }

    public void SetMoveRange(bool value)
    {
        isInMoveRange = value;
        UpdateVisual();
    }

    void OnMouseDown()
    {
        if (!isInMoveRange) return;

        Pawn player = FindFirstObjectByType<Pawn>();
        player.MoveToCell(this);
    }
}
