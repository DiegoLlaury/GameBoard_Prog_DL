using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stateless helper that captures and restores a complete <see cref="BoardSaveData"/>
/// snapshot. Uses only public APIs — no reflection.
/// </summary>
public static class BoardStateSerializer
{
    // ─────────────────────────────────────────────────────────────────────────
    // CAPTURE
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Builds a full snapshot from the currently active board scene.</summary>
    public static BoardSaveData Capture(Board board, Pawn pawn, TurnManager turnManager,
                                        ResourceManager resourceManager, DiceInventoryUI diceInventory)
    {
        var data = new BoardSaveData();
        CaptureBoard(board, data);
        CapturePawn(pawn, data);
        data.currentTurn     = turnManager.currentTurn;
        data.diceUsedThisTurn = turnManager.diceUsedThisTurn;
        CaptureResources(resourceManager, data);
        CaptureDice(diceInventory, data);
        return data;
    }

    static void CaptureBoard(Board board, BoardSaveData data)
    {
        BoardGenerationState gen = board.GetGenerationState();
        data.playerProgressY             = gen.playerProgressY;
        data.destroyedUntil              = gen.destroyedUntil;
        data.highestGeneratedWorldColumn = gen.highestGeneratedWorldColumn;
        data.nextDialogueColumn          = gen.nextDialogueColumn;
        data.nextEventColumn             = gen.nextEventColumn;
        data.safeRow                     = gen.safeRow;
        data.generationStopped           = gen.generationStopped;
        data.endPlaced                   = gen.endPlaced;

        // Golden path Dictionary → two parallel lists (JsonUtility limitation)
        data.goldenPathKeys.Clear();
        data.goldenPathValues.Clear();
        foreach (var kvp in board.goldenPath)
        {
            data.goldenPathKeys.Add(kvp.Key);
            data.goldenPathValues.Add(kvp.Value);
        }

        // Ring buffer
        data.worldColumnRingBuffer = (int[])board.WorldColumnIndex.Clone();

        // Cells — only active ones carry meaningful state
        DialogueDatas[] dialoguePool = board.GetDialoguePool();
        data.cells.Clear();
        foreach (Cell cell in board.AllCells)
        {
            if (!cell.gameObject.activeSelf)
                continue;

            int poolIndex = -1;
            if (dialoguePool != null && cell.dialogueData != null)
                poolIndex = System.Array.IndexOf(dialoguePool, cell.dialogueData);

            data.cells.Add(new CellSaveData
            {
                gridX             = cell.gridX,
                gridY             = cell.gridY,
                worldColumnIndex  = cell.WorldColumnIndex,
                contentType       = cell.contentType,
                durability        = cell.durability,
                eventTriggered    = cell.EventTriggered,
                dialogueIndex     = cell.dialogueIndex,
                dialogueFinished  = cell.dialogueFinished,
                dialoguePoolIndex = poolIndex,
            });
        }
    }

    static void CapturePawn(Pawn pawn, BoardSaveData data)
    {
        data.pawnGridX          = pawn.currentX;
        data.pawnGridY          = pawn.currentY;
        data.pawnWorldColumn    = pawn.currentWorldColumn;
        data.pawnMovementPoints = pawn.movementPoints;
        data.pawnIsUsingDice    = pawn.IsUsingDice;
    }

    static void CaptureResources(ResourceManager resourceManager, BoardSaveData data)
    {
        data.resources.Clear();
        // Enumerate all ResourceData assets loaded in memory to cover every resource type
        foreach (ResourceData rd in Resources.FindObjectsOfTypeAll<ResourceData>())
        {
            if (string.IsNullOrEmpty(rd.resourceId))
                continue;

            data.resources.Add(new ResourceSaveEntry
            {
                resourceId = rd.resourceId,
                amount     = resourceManager.GetResource(rd),
            });
        }
    }

    static void CaptureDice(DiceInventoryUI diceInventory, BoardSaveData data)
    {
        data.diceDurabilities.Clear();
        IReadOnlyList<DiceUI> slots = diceInventory.GetDiceSlots();
        foreach (DiceUI ui in slots)
        {
            if (ui != null && ui.dice != null)
                data.diceDurabilities.Add(ui.dice.durability);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // RESTORE
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Applies a saved snapshot to the current scene.
    /// Must be called after all Awake() and Start() have finished so the grid exists.
    /// </summary>
    public static void Restore(BoardSaveData data, Board board, Pawn pawn, TurnManager turnManager,
                                ResourceManager resourceManager, DiceInventoryUI diceInventory)
    {
        RestoreBoard(data, board);
        RestorePawn(data, pawn, board);
        turnManager.RestoreTurnCount(data.currentTurn);
        turnManager.diceUsedThisTurn = data.diceUsedThisTurn;
        RestoreResources(data, resourceManager);
        RestoreDice(data, diceInventory);
    }

    static void RestoreBoard(BoardSaveData data, Board board)
    {
        // Restore generation state via the dedicated Board API — no reflection needed
        board.RestoreGenerationState(new BoardGenerationState
        {
            playerProgressY             = data.playerProgressY,
            destroyedUntil              = data.destroyedUntil,
            highestGeneratedWorldColumn = data.highestGeneratedWorldColumn,
            nextDialogueColumn          = data.nextDialogueColumn,
            nextEventColumn             = data.nextEventColumn,
            safeRow                     = data.safeRow,
            generationStopped           = data.generationStopped,
            endPlaced                   = data.endPlaced,
        });

        // Golden path
        board.goldenPath.Clear();
        for (int i = 0; i < data.goldenPathKeys.Count && i < data.goldenPathValues.Count; i++)
            board.goldenPath[data.goldenPathKeys[i]] = data.goldenPathValues[i];

        // Ring buffer
        if (data.worldColumnRingBuffer != null &&
            data.worldColumnRingBuffer.Length == board.WorldColumnIndex.Length)
        {
            System.Array.Copy(data.worldColumnRingBuffer, board.WorldColumnIndex,
                              board.WorldColumnIndex.Length);
        }

        // Cell lookup by grid position for O(1) access
        var cellLookup = new Dictionary<(int, int), Cell>(board.AllCells.Count);
        foreach (Cell cell in board.AllCells)
            cellLookup[(cell.gridX, cell.gridY)] = cell;

        // Deactivate all — restore loop re-activates only saved cells
        foreach (Cell cell in board.AllCells)
            cell.gameObject.SetActive(false);

        DialogueDatas[] dialoguePool = board.GetDialoguePool();

        foreach (CellSaveData cd in data.cells)
        {
            if (!cellLookup.TryGetValue((cd.gridX, cd.gridY), out Cell cell))
                continue;

            cell.gameObject.SetActive(true);
            cell.SetWorldColumn(cd.worldColumnIndex);

            // ApplyCellType sets the visual + walkability but also randomises durability,
            // so we overwrite durability right after.
            cell.ApplyCellType(cd.contentType);

            cell.durability      = cd.durability;
            cell.dialogueIndex   = cd.dialogueIndex;
            cell.dialogueFinished = cd.dialogueFinished;
            cell.EventTriggered  = cd.eventTriggered;

            if (dialoguePool != null && cd.dialoguePoolIndex >= 0 &&
                cd.dialoguePoolIndex < dialoguePool.Length)
            {
                cell.dialogueData = dialoguePool[cd.dialoguePoolIndex];
            }

            // Recompute visual state from the restored durability value
            cell.UpdateState();
        }
    }

    static void RestorePawn(BoardSaveData data, Pawn pawn, Board board)
    {
        pawn.currentX           = data.pawnGridX;
        pawn.currentY           = data.pawnGridY;
        pawn.currentWorldColumn = data.pawnWorldColumn;
        pawn.movementPoints     = data.pawnMovementPoints;
        pawn.IsUsingDice        = data.pawnIsUsingDice;

        Cell cell = board.GetCell(data.pawnGridX, data.pawnGridY);
        if (cell != null)
        {
            pawn.transform.position = cell.transform.position;
            pawn.transform.rotation = cell.transform.rotation;
        }

        board.SetSpawnRow(data.pawnGridX);
        board.SetPlayerProgress(data.pawnWorldColumn);

        // If the pawn still had movement points, re-show the movement range
        if (data.pawnMovementPoints > 0 && data.pawnIsUsingDice)
            pawn.ShowMovementRange();
    }

    static void RestoreResources(BoardSaveData data, ResourceManager resourceManager)
    {
        // Build a lookup from resourceId → asset
        var rdLookup = new Dictionary<string, ResourceData>();
        foreach (ResourceData rd in Resources.FindObjectsOfTypeAll<ResourceData>())
        {
            if (!string.IsNullOrEmpty(rd.resourceId))
                rdLookup[rd.resourceId] = rd;
        }

        foreach (ResourceSaveEntry entry in data.resources)
        {
            if (!rdLookup.TryGetValue(entry.resourceId, out ResourceData rd))
            {
                Debug.LogWarning($"[BoardStateSerializer] Unknown resourceId '{entry.resourceId}' — skipped.");
                continue;
            }

            // Compute delta so we don't bypass the event
            int delta = entry.amount - resourceManager.GetResource(rd);
            if (delta != 0)
                resourceManager.AddResource(rd, delta);
        }
    }

    static void RestoreDice(BoardSaveData data, DiceInventoryUI diceInventory)
    {
        // Clear any dice that may have been added before the restore
        diceInventory.ClearAllDice();

        Dice prefab = diceInventory.GetStartingDicePrefab();
        if (prefab == null)
        {
            Debug.LogWarning("[BoardStateSerializer] No starting dice prefab — cannot restore dice.");
            return;
        }

        foreach (int durability in data.diceDurabilities)
        {
            Dice newDice = Object.Instantiate(prefab);
            newDice.durability = durability;
            diceInventory.AddDice(newDice);
        }
    }
}

