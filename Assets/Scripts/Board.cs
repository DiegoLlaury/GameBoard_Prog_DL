using System.Collections.Generic;
using System.Data;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class BFSNode
{
    public Cell cell;
    public int cost;
    public BFSNode parent;

    public BFSNode(Cell cell, int cost, BFSNode parent)
    {
        this.cell = cell;
        this.cost = cost;
        this.parent = parent;
    }
}

public class Board : MonoBehaviour
{
    #region Grid configuration
    [Header("Grid")]
    public int rows = 5;
    public int columns = 10;
    public float cellSize = 1f;
    public GameObject cellPrefab;
    #endregion

    #region Level generation
    [Header("Level")]
    public int referenceColumn = 0;
    [SerializeField] private int LevelLength = 50;

    [SerializeField] private int firstDialogueColumn = 8;
    [SerializeField] private int minDialogueGap = 10;
    [SerializeField] private int maxDialogueGap = 15;
    private int nextDialogueColumn = -1;

    [SerializeField] private int firstEventColumn = 5;
    [SerializeField] private int minEventGap = 4;
    [SerializeField] private int maxEventGap = 7;
    private int nextEventColumn = -1;
    private bool generationStopped = false;
    public Dictionary<int, int> goldenPath = new Dictionary<int, int>();
    #endregion

    private int safeRow = -1;
    [SerializeField] private int safeRowChangeChance = 30; // %

    #region Coordinates & world indices
    private bool endPlaced = false;

    [Header("Coord Reference")]
    public int PlayerProgressY { get; private set; } = 0;
    public int StartColumn { get; private set; } = 0;
    public int DestroyedUntil { get; private set; }
    public int[] WorldColumnIndex;
    private int highestGeneratedWorldColumn;
    #endregion

    #region Pools & balancing
    [Header("Data Pool")]
    [SerializeField] private DialogueDatas[] dialoguePool;

    [Header("Decay Balancing")]

    public int MaxDecayDistance = 15;     // largeur de la vague
    public float MinDecayChance = 0.4f;  // chance minimale (pr�s du joueur)
    public float MaxDecayChance = 0.9f;   // chance max (loin derri�re)

    public float DecayingBoostChance = 0.75f;
    public float NecrosedBoostChance = 0.95f;

    public float NeighborDecayChance = 0.6f;

    public int MaxAheadDistance = 6;          // jusqu�o� devant le joueur �a pourrit
    public float MinAheadDecayChance = 0.1f; // tr�s faible juste devant
    public float MaxAheadDecayChance = 0.3f; // jamais trop fort
    #endregion

    [System.Serializable]
    public class CellPrefabEntry
    {
        public ECellType type;
        public GameObject prefab;
    }

    public List<CellPrefabEntry> cellPrefabs;
    private Dictionary<ECellType, GameObject> prefabLookup;


    /// <summary>
    /// The grid row (x) where the player spawned. Obstacles are never placed on this row
    /// for the starting columns so the player is never blocked at start.
    /// </summary>
    public int SpawnRow { get; private set; } = -1;

    public void SetSpawnRow(int row) => SpawnRow = row;

    public Cell[,] cells;
    public List<Cell> AllCells {  get; private set; } = new List<Cell>();
    
    public static Board Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        prefabLookup = new Dictionary<ECellType, GameObject>();
        if (cellPrefabs != null)
        {
            foreach (var entry in cellPrefabs)
            {
                if (entry != null && entry.prefab != null)
                    prefabLookup[entry.type] = entry.prefab;
            }
        }

        GenerateGrid();

        PlayerProgressY = referenceColumn;
        StartColumn = referenceColumn;
        DestroyedUntil = referenceColumn - 1;
        nextDialogueColumn = firstDialogueColumn;
        nextEventColumn = firstEventColumn;

        WorldColumnIndex = new int[columns];
        for (int i = 0; i < columns; i++)
            WorldColumnIndex[i] = -1;

        highestGeneratedWorldColumn = referenceColumn;
        GenerateGoldenPath();

        // g�n�ration initiale
        int initialGeneration = columns;
        for (int i = 0; i < initialGeneration; i++)
        {
            GenerateColumn(highestGeneratedWorldColumn);
            highestGeneratedWorldColumn++;
        }
    }

    #region Pathfinding (BFS)
    public Dictionary<Cell, BFSNode> GetReachableCells(int startX, int startY, int maxCost)
    {
        Dictionary<Cell, BFSNode> visited = new Dictionary<Cell, BFSNode>();
        Queue<BFSNode> queue = new Queue<BFSNode>();

        Cell start = GetCell(startX, startY);
        if (start == null)
            return visited;

        BFSNode startNode = new BFSNode(start, 0, null);
        queue.Enqueue(startNode);
        visited[start] = startNode;

        while (queue.Count > 0)
        {
            BFSNode current = queue.Dequeue();

            if (current.cost >= maxCost)
                continue;

            foreach (Cell neighbor in GetNeighbors(current.cell))
            {
                if (neighbor == null)
                    continue;

                if (!neighbor.isWalkable || neighbor.state == ECellState.Destroyed)
                    continue;

                if (neighbor.WorldColumnIndex < StartColumn)
                    continue;

                if (current.cell.WorldColumnIndex == StartColumn &&
                    neighbor.WorldColumnIndex > current.cell.WorldColumnIndex)
                    continue;

                if (visited.ContainsKey(neighbor))
                    continue;

                int moveCost = 1;
                if (neighbor.state == ECellState.Decaying)
                    moveCost = 2;
                else if (neighbor.state == ECellState.Necrosed)
                    moveCost = 3;

                int newCost = current.cost + moveCost;
                if (newCost > maxCost)
                    continue;

                BFSNode next = new BFSNode(neighbor, newCost, current);
                visited[neighbor] = next;
                queue.Enqueue(next);
            }
        }

        return visited;
    }


    IEnumerable<Cell> GetNeighbors(Cell cell)
    {
        int x = cell.gridX;
        int y = cell.gridY;

        yield return GetCell(x + 1, y);
        yield return GetCell(x - 1, y);

        yield return GetCell(x, y + 1);
        yield return GetCell(x, y - 1);
    }

    public List<Cell> ReconstructPath(BFSNode node)
    {
        List<Cell> path = new();

        while (node.parent != null)
        {
            path.Add(node.cell);
            node = node.parent;
        }

        path.Reverse();
        return path;
    }
    #endregion

    #region Golden path generation
    void GenerateGoldenPath()
    {
        goldenPath.Clear();

        int currentRow = rows / 2;
        for (int world = StartColumn; world <= LevelLength; world++)
        {
            goldenPath[world] = currentRow;

            // variation douce
            if (Random.value < 0.3f)
            {
                currentRow += Random.Range(-1, 2);
                currentRow = Mathf.Clamp(currentRow, 0, rows - 1);
            }
        }
    }

    /// <summary>
    /// Called after a column is generated. Ensures a cardinal-connected walkable passage
    /// exists on the golden path cell for this column, restoring it only if it was
    /// destroyed or made into an obstacle.
    /// </summary>
    void EnsureGoldenPath(int worldColumn, int physicalY)
    {
        if (!goldenPath.TryGetValue(worldColumn, out int safeX))
            return;

        Cell cell = cells[safeX, physicalY];

        // Restore the cell only if it is not walkable (obstacle or destroyed)
        if (!cell.isWalkable || cell.state == ECellState.Destroyed)
            cell.ApplyCellType(ECellType.Normal);
    }
    #endregion

    #region Grid generation & helpers
    void GenerateGrid()
    {
        cells = new Cell[rows, columns];
        AllCells.Clear();

        float angleStep = 2f * Mathf.PI / columns;
        int referenceRow = rows / 2;
        float referenceRadius = cellSize / angleStep;


        for (int x = 0; x < rows; x++)
        {
            float radius = referenceRadius + (x - referenceRow) * cellSize;

            for (int y = 0; y < columns; y++)
            {
  
                float angle = y * angleStep;
                Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Quaternion rot = Quaternion.LookRotation(pos.normalized);

                GameObject cellObj = Instantiate(cellPrefab, pos, rot, transform);
                float scaleFactor = radius / referenceRadius;
                cellObj.transform.localScale = new Vector3(scaleFactor, 1f, 1f);

                Cell cell = cellObj.GetComponent<Cell>();
                if (cell == null)
                {
                    Debug.LogError("Cell prefab missing Cell component");
                    Destroy(cellObj);
                    continue;
                }

                cell.gridX = x;
                cell.gridY = y;
                cell.gameObject.SetActive(false);

                cells[x, y] = cell;
                AllCells.Add(cell);
            }
        }
    }
    bool IsValidCellIndex(int x, int y)
    {
        if (x < 0 || x >= rows) return false;
        y = (y + columns) % columns;
        return y >= 0 && y < columns;
    }
    #endregion

    #region Column generation & placement
    public void GenerateColumn(int worldColumnIndex)
    {
        if (generationStopped)
            return;

        // Only enforce "max ahead" distance after initial generation
        bool isInitialGeneration = (highestGeneratedWorldColumn < columns);
        if (!isInitialGeneration && worldColumnIndex > highestGeneratedWorldColumn + 3)
            return;

        if (worldColumnIndex <= DestroyedUntil)
            return;

        int physicalY = worldColumnIndex % columns;

        if (WorldColumnIndex[physicalY] > DestroyedUntil)
            return;

        WorldColumnIndex[physicalY] = worldColumnIndex;

        for (int x = 0; x < rows; x++)
        {
            Cell cell = cells[x, physicalY];
            cell.SetWorldColumn(worldColumnIndex);
            cell.ApplyCellType(ECellType.Normal);
        }

        if (worldColumnIndex < StartColumn + 2)
        {
            // These columns act as an invisible wall behind the player.
            // Destroying them hides the visual and makes them non-walkable,
            // clearly communicating the one-way direction to the player.
            for (int x = 0; x < rows; x++)
            {
                Cell cell = cells[x, physicalY];
                cell.ApplyCellType(ECellType.Normal);
                cell.durability = 0;
                cell.UpdateState();
            }
            return;
        }

        // The player always spawns at StartColumn + 2 (currentY = referenceColumn % columns + 2).
        // This column must never receive obstacles: the board generates in Awake, before the Pawn exists.
        if (worldColumnIndex == StartColumn + 2)
        {
            for (int x = 0; x < rows; x++)
            {
                Cell cell = cells[x, physicalY];
                cell.ApplyCellType(ECellType.Normal);
                cell.durability = 6;
                cell.UpdateState();
            }
            return;
        }

        if (!endPlaced && worldColumnIndex == LevelLength)
        {
            PlaceEndCell(physicalY);
            endPlaced = true;
            generationStopped = true;
            return;
        }

        if (worldColumnIndex == nextDialogueColumn)
        {
            PlaceDialogueCell(physicalY);
            nextDialogueColumn += Random.Range(minDialogueGap, maxDialogueGap + 1);
            EnsureGoldenPath(worldColumnIndex, physicalY);
            highestGeneratedWorldColumn = Mathf.Max(highestGeneratedWorldColumn, worldColumnIndex);
            return;
        }

        if (worldColumnIndex == nextEventColumn)
        {
            PlaceEventCell(physicalY);
            nextEventColumn += Random.Range(minEventGap, maxEventGap + 1);
            EnsureGoldenPath(worldColumnIndex, physicalY);
            highestGeneratedWorldColumn = Mathf.Max(highestGeneratedWorldColumn, worldColumnIndex);
            return;
        }

        if (safeRow == -1)
            safeRow = Random.Range(0, rows);

        if (Random.Range(0, 100) < safeRowChangeChance)
        {
            safeRow += Random.Range(-1, 2);
            safeRow = Mathf.Clamp(safeRow, 0, rows - 1);
        }

        if (Random.value < 0.4f)
            PlaceObstacleCluster(physicalY);

        // After obstacles, guarantee the golden path cell is walkable in this column.
        EnsureGoldenPath(worldColumnIndex, physicalY);

        // Also guarantee that the golden path cell in the PREVIOUS world column
        // has at least one cardinal walkable neighbor so movement is never blocked.
        int prevWorldColumn = worldColumnIndex - 1;
        if (goldenPath.TryGetValue(prevWorldColumn, out int prevPathX))
        {
            int prevPhysicalY = prevWorldColumn % columns;
            if (!HasCardinalWalkableNeighbor(prevPathX, prevPhysicalY))
            {
                // Open the golden path cell of the current column as a bridge
                if (goldenPath.TryGetValue(worldColumnIndex, out int curPathX))
                    cells[curPathX, physicalY].ApplyCellType(ECellType.Normal);
            }
        }

        highestGeneratedWorldColumn = Mathf.Max(highestGeneratedWorldColumn, worldColumnIndex);
    }

    /// <summary>
    /// Returns true if the cell at (x, y) has at least one cardinal (non-diagonal)
    /// walkable, non-destroyed neighbor — matching the BFS movement rules.
    /// </summary>
    bool HasCardinalWalkableNeighbor(int x, int y)
    {
        int[] dx = { 1, -1, 0, 0 };
        int[] dy = { 0, 0, 1, -1 };

        for (int i = 0; i < 4; i++)
        {
            Cell c = GetCell(x + dx[i], y + dy[i]);
            if (c != null && c.isWalkable && c.state != ECellState.Destroyed)
                return true;
        }

        return false;
    }
    #endregion

    #region Prefab helper & accessors
    public bool TryGetPrefab(ECellType type, out GameObject prefab)
    {
        return prefabLookup.TryGetValue(type, out prefab);
    }

    public Cell GetCell(int x, int y)
    {
        if (x < 0 || x >= rows)
            return null;

        y = (y + columns) % columns;

        return cells[x, y];
    }
    #endregion

    #region Progress & destruction
    public void SetPlayerProgress(int worldColumn)
    {
        if (worldColumn > PlayerProgressY)
            PlayerProgressY = worldColumn;
    }

    // -------------------------------------------------------------------------
    // Save / Restore API (used exclusively by BoardSaveManager)
    // -------------------------------------------------------------------------

    /// <summary>Captures all private generation state into a flat data struct.</summary>
    public BoardGenerationState GetGenerationState() => new BoardGenerationState
    {
        playerProgressY             = PlayerProgressY,
        destroyedUntil              = DestroyedUntil,
        highestGeneratedWorldColumn = highestGeneratedWorldColumn,
        nextDialogueColumn          = nextDialogueColumn,
        nextEventColumn             = nextEventColumn,
        safeRow                     = safeRow,
        generationStopped           = generationStopped,
        endPlaced                   = endPlaced,
    };

    /// <summary>Restores private generation state from a saved snapshot.</summary>
    public void RestoreGenerationState(BoardGenerationState s)
    {
        PlayerProgressY             = s.playerProgressY;
        DestroyedUntil              = s.destroyedUntil;
        highestGeneratedWorldColumn = s.highestGeneratedWorldColumn;
        nextDialogueColumn          = s.nextDialogueColumn;
        nextEventColumn             = s.nextEventColumn;
        safeRow                     = s.safeRow;
        generationStopped           = s.generationStopped;
        endPlaced                   = s.endPlaced;
    }

    /// <summary>Returns the dialogue pool so the serializer can map indices to assets.</summary>
    public DialogueDatas[] GetDialoguePool() => dialoguePool;

    public void TryAdvanceDestroyedFront()
    {
        int next = DestroyedUntil + 1;
        int y = next % columns;

        for (int x = 0; x < rows; x++)
        {
            if (cells[x, y].durability > 0)
                return; // Stop if any cell is still alive
        }

        AdvanceDestroyedFront();
    }

    public void AdvanceDestroyedFront()
    {
        if (generationStopped)
            return;

        DestroyedUntil++;

        int physicalY = DestroyedUntil % columns;

        for (int x = 0; x < rows; x++)
        {
            Cell cell = cells[x, physicalY];

            cell.durability = 0;
            cell.UpdateState();
        }


        int newWorldColumn = DestroyedUntil + columns;
        GenerateColumn(newWorldColumn);

        highestGeneratedWorldColumn = Mathf.Max(highestGeneratedWorldColumn, newWorldColumn + 1);
    }
    #endregion

    #region Placers & events
    void PlaceObstacleCluster(int y)
    {
        HashSet<int> blockedRows = new HashSet<int>();

        for (int x = 0; x < rows; x++)
        {
            if (x == safeRow)
                continue;

            // Never place an obstacle on the player's spawn row.
            if (x == SpawnRow)
                continue;

            if (goldenPath.TryGetValue(cells[x, y].WorldColumnIndex, out int safeX))
            {
                if (x == safeX)
                    continue;
            }

            if (Random.value < 0.7f)
                blockedRows.Add(x);
        }

        foreach (int x in blockedRows)
        {
            Cell cell = cells[x, y];

            if (cell.WorldColumnIndex <= PlayerProgressY)
                continue;

            cell.ApplyCellType(ECellType.Obstacle);
        }
    }


    void PlaceDialogueCell(int y)
    {
        int x = Random.Range(0, rows);
        Cell cell = cells[x, y];

        cell.ApplyCellType(ECellType.Dialogue);

        if (dialoguePool.Length > 0)
            cell.dialogueData = dialoguePool[Random.Range(0, dialoguePool.Length)];

        // s�curit� : le reste de la colonne reste praticable
        for (int i = 0; i < rows; i++)
        {
            if (i == x) continue;
            if (!cells[i, y].isWalkable)
                cells[i, y].ApplyCellType(ECellType.Normal);
        }
    }

    void PlaceEndCell(int y)
    {
        int x = rows / 2;
        cells[x, y].ApplyCellType(ECellType.End);
    }

    void PlaceEventCell(int y)
    {
        int x = Random.Range(0, rows);
        Cell cell = cells[x, y];
        cell.ApplyCellType(ECellType.Event);
    }
    #endregion
} 
