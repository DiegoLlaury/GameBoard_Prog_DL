using System;
using UnityEngine;

/// <summary>
/// Trigger placed at the end of the race track.
/// Fires OnPlayerFinished when the player's collider enters the zone.
/// Attach to a GameObject with a Trigger Collider.
/// </summary>
public class FinishZone : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";

    /// <summary>Fired once when the player enters the finish zone.</summary>
    public event Action OnPlayerFinished;

    private bool triggered;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag(playerTag)) return;

        triggered = true;
        OnPlayerFinished?.Invoke();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.35f);
        Gizmos.DrawCube(transform.position, transform.localScale);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}
