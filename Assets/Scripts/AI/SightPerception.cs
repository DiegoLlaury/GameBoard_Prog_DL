using UnityEngine;

/// <summary>
/// Cone-shaped vision system. Detects the player within a given angle and range,
/// using a raycast to verify there are no obstacles in between.
/// </summary>
public class SightPerception : MonoBehaviour
{
    [Header("Cone Vision")]
    [SerializeField] private float detectionRadius = 8f;
    [SerializeField] [Range(0f, 360f)] private float fieldOfViewAngle = 90f;
    [SerializeField] private float eyeHeight = 1.5f;
    [SerializeField] private float targetHeight = 1.0f;

    [Header("Raycast Filter")]
    [Tooltip("Layers to check for line-of-sight obstacles. Should NOT include the Player layer.")]
    [SerializeField] private LayerMask obstacleMask = ~0;

    [Header("References")]
    [SerializeField] private string playerTag = "Player";

    /// <summary>Whether the player is currently seen by the zombie.</summary>
    public bool IsPlayerDetected { get; private set; }

    /// <summary>Last known player position when detected.</summary>
    public Vector3 LastKnownPosition { get; private set; }

    private Transform playerTransform;

    // Base values for difficulty scaling
    private float baseDetectionRadius;
    private float baseFieldOfViewAngle;

    private void Awake()
    {
        baseDetectionRadius  = detectionRadius;
        baseFieldOfViewAngle = fieldOfViewAngle;
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindWithTag(playerTag);
        if (playerObj != null)
            playerTransform = playerObj.transform;
        else
            Debug.LogWarning($"[SightPerception] No GameObject found with tag '{playerTag}'.");
    }

    private void Update()
    {
        IsPlayerDetected = CheckPlayerInSight();
        if (IsPlayerDetected)
            LastKnownPosition = playerTransform.position;
    }

    private bool CheckPlayerInSight()
    {
        if (playerTransform == null) return false;

        // Use elevated positions to avoid self-hit and ground collisions
        Vector3 eyePos    = transform.position + Vector3.up * eyeHeight;
        Vector3 targetPos = playerTransform.position + Vector3.up * targetHeight;

        Vector3 directionToPlayer = targetPos - eyePos;
        float distanceToPlayer = directionToPlayer.magnitude;

        if (distanceToPlayer > detectionRadius) return false;

        // Angle check uses the flat (horizontal) forward direction
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);
        if (angleToPlayer > fieldOfViewAngle * 0.5f) return false;

        // Raycast against obstacles only — if nothing blocks the path, the player is visible.
        // If something IS hit, check whether it is the player (line of sight clear) or a wall.
        if (Physics.Raycast(eyePos, directionToPlayer.normalized, out RaycastHit hit,
                            distanceToPlayer, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            return hit.collider.CompareTag(playerTag);
        }

        // No obstacle hit at all — clear line of sight
        return true;
    }

    /// <summary>Forces the detection state externally (e.g. from a scream).</summary>
    public void ForceDetect(Vector3 position)
    {
        IsPlayerDetected = true;
        LastKnownPosition = position;
    }

    // -------------------------------------------------------------------------
    // Difficulty API (called by DifficultyScaler)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Applies a detection multiplier relative to the base Inspector values.
    /// Increases both the detection radius and the field of view angle.
    /// </summary>
    public void SetDetectionMultiplier(float multiplier)
    {
        detectionRadius  = baseDetectionRadius * multiplier;
        fieldOfViewAngle = Mathf.Min(360f, baseFieldOfViewAngle * multiplier);
    }

    /// <summary>
    /// Resets detection values back to base Inspector values.
    /// </summary>
    public void ResetDetection()
    {
        detectionRadius  = baseDetectionRadius;
        fieldOfViewAngle = baseFieldOfViewAngle;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 eyePos = transform.position + Vector3.up * eyeHeight;

        Gizmos.color = IsPlayerDetected ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(eyePos, detectionRadius);

        Vector3 leftBoundary  = Quaternion.Euler(0, -fieldOfViewAngle * 0.5f, 0) * transform.forward;
        Vector3 rightBoundary = Quaternion.Euler(0,  fieldOfViewAngle * 0.5f, 0) * transform.forward;
        Gizmos.DrawRay(eyePos, leftBoundary  * detectionRadius);
        Gizmos.DrawRay(eyePos, rightBoundary * detectionRadius);
    }
}
