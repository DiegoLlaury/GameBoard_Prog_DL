using UnityEngine;

/// <summary>
/// ScriptableObject holding difficulty curves for the Zombie Survival minigame.
/// Each AnimationCurve maps elapsed time (seconds) to a multiplier value.
/// </summary>
[CreateAssetMenu(fileName = "ZombieSurvivalConfig", menuName = "Scriptable Objects/ZombieSurvivalConfig")]
public class ZombieSurvivalConfig : ScriptableObject
{
    [Header("Player")]
    public int maxHits = 3;
    public float invincibilityDuration = 1.5f;

    [Header("Collectibles")]
    public int initialCollectibleCount = 5;
    public int maxCollectiblesOnMap = 8;
    public float respawnDelay = 2f;
    public float spawnRadius = 15f;

    [Header("Difficulty Curves (X = time in seconds, Y = multiplier)")]
    [Tooltip("Multiplier for zombie patrol and chase speed")]
    public AnimationCurve speedCurve = AnimationCurve.Linear(0f, 1f, 180f, 2.5f);

    [Tooltip("Multiplier for detection radius and FOV angle")]
    public AnimationCurve detectionCurve = AnimationCurve.Linear(0f, 1f, 180f, 2.5f);

    [Tooltip("Multiplier for attack rate (divides cooldown)")]
    public AnimationCurve attackRateCurve = AnimationCurve.Linear(0f, 1f, 180f, 2f);

    [Tooltip("Multiplier for scream frequency (divides interval)")]
    public AnimationCurve screamFreqCurve = AnimationCurve.Linear(0f, 1f, 180f, 2f);

    [Header("Loot Table")]
    public CollectibleEntry[] collectibleTable;

    [System.Serializable]
    public struct CollectibleEntry
    {
        public ResourceData resource;
        public int minAmount;
        public int maxAmount;
        [Range(0f, 1f)] public float weight;
    }
}
