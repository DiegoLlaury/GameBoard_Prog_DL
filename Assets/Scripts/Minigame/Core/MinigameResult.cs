using System.Collections.Generic;

/// <summary>
/// Holds the results of a completed minigame.
/// Passed back to MinigameManager when the minigame ends.
/// </summary>
public class MinigameResult
{
    /// <summary>Total score (e.g. number of collectibles).</summary>
    public int Score;

    /// <summary>How long the player survived in seconds.</summary>
    public float SurvivalTime;

    /// <summary>Number of hits taken before game over.</summary>
    public int HitsTaken;

    /// <summary>Resources collected during the minigame (already added to ResourceManager).</summary>
    public Dictionary<ResourceData, int> CollectedResources = new Dictionary<ResourceData, int>();

    /// <summary>Tracks a collected resource for the result summary.</summary>
    public void TrackResource(ResourceData data, int amount)
    {
        if (data == null) return;

        if (CollectedResources.ContainsKey(data))
            CollectedResources[data] += amount;
        else
            CollectedResources[data] = amount;
    }
}
