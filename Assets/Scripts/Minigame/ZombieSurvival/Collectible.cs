using System;
using UnityEngine;

/// <summary>
/// Pickup item in a minigame. When the player enters its trigger,
/// adds resources via ResourceManager and fires OnCollected.
/// Rotates and bobs for visual feedback.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Collectible : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private float rotateSpeed = 90f;
    [SerializeField] private float bobAmplitude = 0.2f;
    [SerializeField] private float bobFrequency = 2f;

    /// <summary>Fired when this collectible is picked up. Passes the resource and amount.</summary>
    public event Action<Collectible, ResourceData, int> OnCollected;

    private ResourceData resourceData;
    private int amount;
    private Vector3 startPosition;
    private string playerTag = "Player";

    /// <summary>Configures this collectible with resource data and amount.</summary>
    public void Setup(ResourceData data, int resourceAmount, string tag = "Player")
    {
        resourceData = data;
        amount = resourceAmount;
        playerTag = tag;
        startPosition = transform.position;
    }

    private void Update()
    {
        // Rotation
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);

        // Bobbing
        Vector3 pos = startPosition;
        pos.y += Mathf.Sin(Time.time * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
        transform.position = pos;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        // Add resource to persistent manager
        if (resourceData != null && ResourceManager.Instance != null)
        {
            ResourceManager.Instance.AddResource(resourceData, amount);
            Debug.Log($"[Collectible] +{amount} {resourceData.displayName}");
        }

        OnCollected?.Invoke(this, resourceData, amount);
        Destroy(gameObject);
    }
}
