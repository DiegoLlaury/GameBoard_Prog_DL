using System.Collections.Generic;
using System.Data;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class Board : MonoBehaviour
{
    [Header("Grid")]
    public int rows = 5;
    public int columns = 10;
    public float cellSize = 1f;
    public GameObject cellPrefab;

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
    
    private int safeRow = -1;
    [SerializeField] private int safeRowChangeChance = 30; // %

    private bool endPlaced = false;

    [Header("Coord Reference")]
    public int PlayerProgressY { get; private set; } = 0;
    public int StartColumn { get; private set; }
    public int DestroyedUntil { get; private set; }
    public int[] WorldColumnIndex;
    private int highestGeneratedWorldColumn;

    [Header("Data Pool")]

    [SerializeField] private DialogueDatas[] dialoguePool;

    [Header("Decay Balancing")]

    public int MaxDecayDistance = 15;     // largeur de la vague
    public float MinDecayChance = 0.4f;  // chance minimale (près du joueur)
    public float MaxDecayChance = 0.9f;   // chance max (loin derrière)

    public float DecayingBoostChance = 0.75f;
    public float NecrosedBoostChance = 0.95f;

    public float NeighborDecayChance = 0.6f;

    public int MaxAheadDistance = 6;          // jusqu’où devant le joueur ça pourrit
    public float MinAheadDecayChance = 0.1f; // très faible juste devant
    public float MaxAheadDecayChance = 0.3f; // jamais trop fort

    [System.Serializable]
    public class CellPrefabEntry
    {
        public ECellType type;
        public GameObject prefab;
    }

    public List<CellPrefabEntry> cellPrefabs;
    private Dictionary<ECellType, GameObject> prefabLookup;


    public Cell[,] cells;
    

    public List<Cell> AllCells {  get; private set; } = new List<Cell>();
    public static Board Instance;

    private void Awake()
    {
        Instance = this;
        
        prefabLookup = new Dictionary<ECellType, GameObject>();
        foreach (var entry in cellPrefabs)
        {
            prefabLookup[entry.type] = entry.prefab;
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

        // génération initiale
        for (int i = 0; i < columns; i++)
        {
            GenerateColumn(highestGeneratedWorldColumn);
            highestGeneratedWorldColumn++;
        }
    }


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

                cell.gridX = x;
                cell.gridY = y;
           
                cell.gameObject.SetActive(false);


                cells[x, y] = cell;
                AllCells.Add(cell);
            }
        }
    }

    public void GenerateColumn(int worldColumnIndex)
    {
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
            for (int x = 0; x < rows; x++)
            {
                Cell cell = cells[x, physicalY];
                cell.durability = 1;
                cell.UpdateState();
            }
            return;
        }

        // ===== FIN DE NIVEAU =====
        if (!endPlaced && worldColumnIndex == LevelLength)
        {
            PlaceEndCell(physicalY);
            endPlaced = true;
            return;
        }

        /// DIALOGUE ///
        bool dialoguePlaced = false;
        if (worldColumnIndex == nextDialogueColumn)
        {
            PlaceDialogueCell(physicalY);
            nextDialogueColumn += Random.Range(minDialogueGap, maxDialogueGap + 1);
            dialoguePlaced = true;
            return;
        }

        /// EVENT ///
        if (!dialoguePlaced && worldColumnIndex == nextEventColumn)
        {
            PlaceEventCell(physicalY);
            nextEventColumn += Random.Range(minEventGap, maxEventGap + 1);
            return;
        }
        
        /// OBSTACLE ///
        if (safeRow == -1)
            safeRow = Random.Range(0, rows);

        // variation douce
        if (Random.Range(0, 100) < safeRowChangeChance)
        {
            safeRow += Random.Range(-1, 2);
            safeRow = Mathf.Clamp(safeRow, 0, rows - 1);
        }
        if (Random.value < 0.4f)
            PlaceObstacleCluster(physicalY);
        
        bool hasPath = false;
        for (int x = 0; x < rows; x++)
        {
            if (cells[x, physicalY].isWalkable)
            {
                hasPath = true;
                break;
            }
        }

        if (!hasPath)
        {
            cells[safeRow, physicalY].ApplyCellType(ECellType.Normal);
        }
    }

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

    public List<Cell> GetPath(int startX, int startY, int targetX, int targetY)
    {
        List<Cell> path = new List<Cell>();

        int x = startX;
        int y = startY;

        int safety = rows * columns; // anti-boucle infinie

        while ((x != targetX || y != targetY) && safety-- > 0)
        {
            // déplacement radial
            if (x != targetX)
            {
                x += (x < targetX) ? 1 : -1;
            }
            else
            {
                // déplacement circulaire LE PLUS COURT
                int right = (targetY - y + columns) % columns;
                int left = (y - targetY + columns) % columns;

                if (right <= left)
                    y = (y + 1) % columns;
                else
                    y = (y - 1 + columns) % columns;
            }

            Cell cell = GetCell(x, y);

            if (cell == null || cell.state == ECellState.Destroyed)
                return new List<Cell>();

            if (cell == null || (!cell.isWalkable && cell.contentType != ECellType.Dialogue) || cell.state == ECellState.Destroyed)
                return new List<Cell>();

            path.Add(cell);
        }

        return path;
    }

    public void SetPlayerProgress(int worldColumn)
    {
        if (worldColumn > PlayerProgressY)
            PlayerProgressY = worldColumn;
    }

    public void TryAdvanceDestroyedFront()
    {

        int next = DestroyedUntil + 1;
        int y = next % columns;

        for (int x = 0; x < rows; x++)
        {
            if (cells[x, y].durability > 0)
                return;
        }

        AdvanceDestroyedFront();
    }

    public void AdvanceDestroyedFront()
    {
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

    void PlaceObstacleCluster(int y)
    {
        HashSet<int> blockedRows = new HashSet<int>();

        for (int x = 0; x < rows; x++)
        {
            if (x == safeRow)
                continue;

            if (Random.value < 0.5f)
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

        Cell oldCell = cells[x, y];
        GameObject go = oldCell.gameObject;

        Debug.Log($"[DIALOGUE] colonne {cells[x, y].WorldColumnIndex} row {x}");

        // Sauvegarde des données importantes
        int gridX = oldCell.gridX;
        int gridY = oldCell.gridY;
        int worldColumn = oldCell.WorldColumnIndex;
        Transform visualRoot = oldCell.transform.Find("VisualRoot");

        if (visualRoot != null)
        {
            foreach (Transform child in visualRoot)
                Destroy(child.gameObject);
        }

        // Supprimer l'ancien composant Cell
        Destroy(oldCell);

        // Ajouter le DialogueCell
        DialogueCell dlgCell = go.AddComponent<DialogueCell>();

        // Réassigner les données
        dlgCell.gridX = gridX;
        dlgCell.gridY = gridY;
        dlgCell.SetWorldColumn(worldColumn);

        typeof(Cell)
        .GetField("visualRoot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
        ?.SetValue(dlgCell, visualRoot);

        // Appliquer le type
        dlgCell.ApplyCellType(ECellType.Dialogue);

        // Assigner un dialogue
        if (dialoguePool.Length > 0)
        {
            dlgCell.dialogueData = dialoguePool[Random.Range(0, dialoguePool.Length)];
        }

        cells[x, y] = dlgCell;

        for (int i = 0; i < rows; i++)
        {
            if (i == x) continue;

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
        cells[x, y].ApplyCellType(ECellType.Event);
    }
} 
