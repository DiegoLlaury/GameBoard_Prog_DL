using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Tracks player hits in a minigame. Provides invincibility frames
/// after each hit to prevent chain damage. Fires events on hit and game over.
/// </summary>
public class MinigamePlayerHealth : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private int maxHits = 3;
    [SerializeField] private float invincibilityDuration = 1.5f;

    [Header("Visual Feedback")]
    [SerializeField] private Renderer playerRenderer;
    [SerializeField] private float flashInterval = 0.15f;

    /// <summary>Fired each time the player takes a hit. Passes remaining hits.</summary>
    public event Action<int> OnHit;

    /// <summary>Fired when the player has taken all hits.</summary>
    public event Action OnGameOver;

    /// <summary>Current remaining hits before game over.</summary>
    public int RemainingHits { get; private set; }

    /// <summary>True during invincibility frames.</summary>
    public bool IsInvincible { get; private set; }

    private Coroutine invincibilityCoroutine;

    /// <summary>Initializes health with the given max hits and invincibility duration.</summary>
    public void Setup(int hits, float invincDuration)
    {
        maxHits = hits;
        invincibilityDuration = invincDuration;
        RemainingHits = maxHits;
        IsInvincible = false;
    }

    /// <summary>
    /// Attempts to deal one hit to the player.
    /// Ignored during invincibility frames.
    /// </summary>
    public void TakeHit()
    {
        if (IsInvincible || RemainingHits <= 0) return;

        RemainingHits--;
        Debug.Log($"[MinigamePlayerHealth] Hit taken! Remaining: {RemainingHits}");

        OnHit?.Invoke(RemainingHits);

        if (RemainingHits <= 0)
        {
            OnGameOver?.Invoke();
            return;
        }

        if (invincibilityCoroutine != null)
            StopCoroutine(invincibilityCoroutine);

        invincibilityCoroutine = StartCoroutine(InvincibilityRoutine());
    }

    private IEnumerator InvincibilityRoutine()
    {
        IsInvincible = true;
        float elapsed = 0f;

        // Flash the player renderer to signal invincibility
        while (elapsed < invincibilityDuration)
        {
            if (playerRenderer != null)
                playerRenderer.enabled = !playerRenderer.enabled;

            yield return new WaitForSeconds(flashInterval);
            elapsed += flashInterval;
        }

        // Ensure renderer is visible after invincibility ends
        if (playerRenderer != null)
            playerRenderer.enabled = true;

        IsInvincible = false;
        invincibilityCoroutine = null;
    }

    private void OnDisable()
    {
        if (invincibilityCoroutine != null)
            StopCoroutine(invincibilityCoroutine);

        IsInvincible = false;

        if (playerRenderer != null)
            playerRenderer.enabled = true;
    }
}
