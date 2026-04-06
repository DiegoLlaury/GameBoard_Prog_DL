using System;
using System.Collections.Generic;

/// <summary>
/// Complete serializable snapshot of one board session.
/// Pure data — no UnityEngine dependencies so JsonUtility can serialize it fully.
/// </summary>
[Serializable]
public class BoardSaveData
{
    // ── Board generation state ────────────────────────────────────────────────
    public int  playerProgressY;
    public int  destroyedUntil;
    public int  highestGeneratedWorldColumn;
    public int  nextDialogueColumn;
    public int  nextEventColumn;
    public int  safeRow;
    public bool generationStopped;
    public bool endPlaced;

    // ── Golden path — two parallel lists because JsonUtility can't serialize Dictionary ─
    public List<int> goldenPathKeys   = new List<int>();
    public List<int> goldenPathValues = new List<int>();

    // ── Ring-buffer physicalY → worldColumnIndex ──────────────────────────────
    public int[] worldColumnRingBuffer;

    // ── All active cells ──────────────────────────────────────────────────────
    public List<CellSaveData> cells = new List<CellSaveData>();

    // ── Pawn ──────────────────────────────────────────────────────────────────
    public int pawnGridX;
    public int pawnGridY;
    public int pawnWorldColumn;
    public int pawnMovementPoints;
    public bool pawnIsUsingDice;

    // ── Turn ──────────────────────────────────────────────────────────────────
    public int currentTurn;
    public bool diceUsedThisTurn;

    // ── Resources (keyed by ResourceData.resourceId) ─────────────────────────
    public List<ResourceSaveEntry> resources = new List<ResourceSaveEntry>();

    // ── Dice durabilities, one int per slot in order ──────────────────────────
    public List<int> diceDurabilities = new List<int>();
}

[Serializable]
public struct CellSaveData
{
    public int       gridX;
    public int       gridY;
    public int       worldColumnIndex;
    public ECellType contentType;
    public int       durability;
    public bool      eventTriggered;
    public int       dialogueIndex;
    public bool      dialogueFinished;
    /// <summary>Index into Board.dialoguePool. -1 = no dialogue assigned.</summary>
    public int       dialoguePoolIndex;
}

[Serializable]
public struct ResourceSaveEntry
{
    public string resourceId;
    public int    amount;
}

/// <summary>
/// Flat struct used by Board.GetGenerationState() / Board.RestoreGenerationState()
/// to exchange all private generation fields without reflection.
/// </summary>
[Serializable]
public struct BoardGenerationState
{
    public int  playerProgressY;
    public int  destroyedUntil;
    public int  highestGeneratedWorldColumn;
    public int  nextDialogueColumn;
    public int  nextEventColumn;
    public int  safeRow;
    public bool generationStopped;
    public bool endPlaced;
}

