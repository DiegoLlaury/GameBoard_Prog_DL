using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Spawns collectibles on valid NavMesh positions within a configurable radius.
/// Respawns a new collectible after one is picked up (with delay).
/// </summary>
public class CollectibleSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject collectiblePrefab;
    [SerializeField] private float spawnRadius = 15f;
    [SerializeField] private float spawnHeight = 0.5f;
    [SerializeField] private int maxCollectibles = 8;
    [SerializeField] private float respawnDelay = 2f;

    [Header("NavMesh")]
    [SerializeField] private float navMeshSampleDistance = 5f;

    private const int MAX_SPAWN_ATTEMPTS = 30;

    /// <summary>Fired when a collectible is picked up. Passes resource and amount.</summary>
    public event Action<ResourceData, int> OnCollectiblePickedUp;

    private readonly List<Collectible> activeCollectibles = new List<Collectible>();
    private ZombieSurvivalConfig.CollectibleEntry[] lootTable;
    private float totalWeight;
    private bool isSpawning;

    /// <summary>Initializes the spawner with config data and performs initial spawn.</summary>
    public void Setup(ZombieSurvivalConfig config)
    {
        spawnRadius = config.spawnRadius;
        maxCollectibles = config.maxCollectiblesOnMap;
        respawnDelay = config.respawnDelay;
        lootTable = config.collectibleTable;

        totalWeight = 0f;
        if (lootTable != null)
        {
            foreach (var entry in lootTable)
                totalWeight += entry.weight;
        }

        isSpawning = true;
        SpawnInitialBatch(config.initialCollectibleCount);
    }

    /// <summary>Stops spawning and destroys all active collectibles.</summary>
    public void Cleanup()
    {
        isSpawning = false;
        StopAllCoroutines();

        foreach (var c in activeCollectibles)
        {
            if (c != null)
                Destroy(c.gameObject);
        }
        activeCollectibles.Clear();
    }

    private void SpawnInitialBatch(int count)
    {
        for (int i = 0; i < count && activeCollectibles.Count < maxCollectibles; i++)
            SpawnOne();
    }

    private void SpawnOne()
    {
        if (collectiblePrefab == null || lootTable == null || lootTable.Length == 0)
        {
            Debug.LogWarning("[CollectibleSpawner] Missing prefab or loot table.");
            return;
        }

        Vector3 spawnPos;
        if (!TryFindSpawnPosition(out spawnPos))
        {
            Debug.LogWarning("[CollectibleSpawner] Could not find valid NavMesh position.");
            return;
        }

        spawnPos.y += spawnHeight;

        // Pick random resource from weighted loot table
        ZombieSurvivalConfig.CollectibleEntry entry = PickRandomEntry();
        int amount = UnityEngine.Random.Range(entry.minAmount, entry.maxAmount + 1);

        GameObject obj = Instantiate(collectiblePrefab, spawnPos, Quaternion.identity);
        Collectible collectible = obj.GetComponent<Collectible>();
        if (collectible == null)
            collectible = obj.AddComponent<Collectible>();

        collectible.Setup(entry.resource, amount);
        collectible.OnCollected += HandleCollected;
        activeCollectibles.Add(collectible);
    }

    private void HandleCollected(Collectible collectible, ResourceData data, int amount)
    {
        collectible.OnCollected -= HandleCollected;
        activeCollectibles.Remove(collectible);

        OnCollectiblePickedUp?.Invoke(data, amount);

        if (isSpawning && activeCollectibles.Count < maxCollectibles)
            StartCoroutine(RespawnAfterDelay());
    }

    private IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);

        if (isSpawning && activeCollectibles.Count < maxCollectibles)
            SpawnOne();
    }

    private bool TryFindSpawnPosition(out Vector3 position)
    {
        position = Vector3.zero;

        for (int i = 0; i < MAX_SPAWN_ATTEMPTS; i++)
        {
            Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * spawnRadius;
            Vector3 candidate = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, navMeshSampleDistance, NavMesh.AllAreas))
            {
                position = hit.position;
                return true;
            }
        }

        return false;
    }

    private ZombieSurvivalConfig.CollectibleEntry PickRandomEntry()
    {
        float roll = UnityEngine.Random.Range(0f, totalWeight);
        float cumulative = 0f;

        for (int i = 0; i < lootTable.Length; i++)
        {
            cumulative += lootTable[i].weight;
            if (roll <= cumulative)
                return lootTable[i];
        }

        return lootTable[lootTable.Length - 1];
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}
