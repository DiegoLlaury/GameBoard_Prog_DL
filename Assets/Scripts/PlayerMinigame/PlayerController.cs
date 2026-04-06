using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Top-down player controller for the minigame.
/// Moves the character on the XZ plane using the directional arrow keys.
/// Requires a Rigidbody set to freeze Y position and all rotations.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    private static readonly RigidbodyConstraints MovementConstraints =
        RigidbodyConstraints.FreezePositionY |
        RigidbodyConstraints.FreezeRotationX |
        RigidbodyConstraints.FreezeRotationY |
        RigidbodyConstraints.FreezeRotationZ;

    private Rigidbody rb;
    private InputAction moveAction;
    private Vector2 inputDirection;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = MovementConstraints;

        moveAction = new InputAction(
            name: "Move",
            type: InputActionType.Value,
            expectedControlType: "Vector2"
        );

        moveAction.AddCompositeBinding("2DVector")
            .With("Up",    "<Keyboard>/upArrow")
            .With("Down",  "<Keyboard>/downArrow")
            .With("Left",  "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");
    }

    private void OnEnable()
    {
        moveAction.Enable();
        moveAction.performed += OnMove;
        moveAction.canceled  += OnMove;
    }

    private void OnDisable()
    {
        moveAction.performed -= OnMove;
        moveAction.canceled  -= OnMove;
        moveAction.Disable();
    }

    private void OnMove(InputAction.CallbackContext ctx)
    {
        inputDirection = ctx.ReadValue<Vector2>();
    }

    private void FixedUpdate()
    {
        Vector3 moveDirection = new Vector3(inputDirection.x, 0f, inputDirection.y);
        rb.linearVelocity = moveDirection * moveSpeed;

        if (moveDirection != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(moveDirection);
    }
}
