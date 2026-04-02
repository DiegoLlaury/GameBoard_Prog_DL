using UnityEngine;

/// <summary>
/// Third-person camera that stays behind and above the player.
/// The "behind" direction is always toward the grid centre (inward),
/// giving a clean TPS view that rotates with the player around the ring.
/// </summary>
[RequireComponent(typeof(Camera))]
public class IsometricCameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Pawn pawn;
    [SerializeField] private Board board;

    [Header("TPS Offset")]
    [Tooltip("Height of the camera above the player.")]
    [SerializeField] private float height = 5f;

    [Tooltip("Distance behind the player (toward grid centre).")]
    [SerializeField] private float pullback = 7f;

    [Header("Smoothing")]
    [Tooltip("Position follow smoothing time (seconds).")]
    [SerializeField] private float positionSmoothTime = 0.3f;

    [Tooltip("Rotation smoothing time (seconds).")]
    [SerializeField] private float rotationSmoothTime = 0.5f;

    private Vector3 positionVelocity = Vector3.zero;
    private float targetYaw = 0f;
    private float currentYaw = 0f;
    private float yawVelocity = 0f;

    private void Start()
    {
        if (pawn == null)
            pawn = FindFirstObjectByType<Pawn>();

        if (board == null)
            board = Board.Instance;

        targetYaw = ComputePlayerYaw();
        currentYaw = targetYaw;

        // Hard snap on first frame.
        transform.position = ComputeTargetPosition(currentYaw);
        transform.LookAt(pawn.transform.position + Vector3.up * 1f, Vector3.up);
    }

    private void LateUpdate()
    {
        if (pawn == null)
            return;

        targetYaw = ComputePlayerYaw();
        currentYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref yawVelocity, rotationSmoothTime);

        Vector3 targetPos = ComputeTargetPosition(currentYaw);
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref positionVelocity, positionSmoothTime);

        // Always look slightly above the player's feet for a natural TPS feel.
        Vector3 lookTarget = pawn.transform.position + Vector3.up * 1f;
        transform.rotation = Quaternion.LookRotation(lookTarget - transform.position, Vector3.up);
    }

    /// <summary>
    /// Returns the outward yaw (degrees) matching the player's current column on the ring.
    /// </summary>
    private float ComputePlayerYaw()
    {
        if (pawn == null || board == null)
            return 0f;

        float angleStep = 2f * Mathf.PI / board.columns;
        float radians = pawn.currentY * angleStep;
        Vector3 outward = new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians));
        return Mathf.Atan2(outward.z, outward.x) * Mathf.Rad2Deg;
    }

    /// <summary>
    /// Computes the desired world-space camera position for the given yaw.
    /// </summary>
    private Vector3 ComputeTargetPosition(float yaw)
    {
        float rad = yaw * Mathf.Deg2Rad;
        Vector3 outward = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad));
        Vector3 inward = -outward;

        return pawn.transform.position + inward * pullback + Vector3.up * height;
    }
}
