using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Player controller for the Zombie Race minigame.
/// Creates its own InputActions in code — no PlayerInput component required.
/// Uses world-space axes (X/Z) for movement — compatible with any fixed overhead camera.
/// Requires a CharacterController. Call Setup() then StartMovement() to enable movement.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class ZombieRacePlayerController : MonoBehaviour
{

    private CharacterController characterController;
    private ZombieRaceConfig config;

    private Vector2 moveInput;
    private bool sprintHeld;
    private bool jumpPressed;

    private Vector3 velocity;
    private bool isMovementEnabled;

    private InputAction moveAction;
    private InputAction sprintAction;
    private InputAction jumpAction;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        BuildInputActions();
    }

    private void BuildInputActions()
    {
        // Move — WASD + arrow keys, same as PlayerController
        moveAction = new InputAction(
            name: "Move",
            type: InputActionType.Value,
            expectedControlType: "Vector2");

        moveAction.AddCompositeBinding("2DVector")
            .With("Up",    "<Keyboard>/w")
            .With("Up",    "<Keyboard>/upArrow")
            .With("Down",  "<Keyboard>/s")
            .With("Down",  "<Keyboard>/downArrow")
            .With("Left",  "<Keyboard>/a")
            .With("Left",  "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/d")
            .With("Right", "<Keyboard>/rightArrow");

        moveAction.AddBinding("<Gamepad>/leftStick");

        // Sprint — Left Shift / Gamepad left shoulder
        sprintAction = new InputAction(
            name: "Sprint",
            type: InputActionType.Button);

        sprintAction.AddBinding("<Keyboard>/leftShift");
        sprintAction.AddBinding("<Gamepad>/leftShoulder");

        // Jump — Space / Gamepad south button
        jumpAction = new InputAction(
            name: "Jump",
            type: InputActionType.Button);

        jumpAction.AddBinding("<Keyboard>/space");
        jumpAction.AddBinding("<Gamepad>/buttonSouth");
    }

    private void OnEnable()
    {
        moveAction.Enable();
        sprintAction.Enable();
        jumpAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        sprintAction.Disable();
        jumpAction.Disable();
    }

    private void OnDestroy()
    {
        moveAction.Dispose();
        sprintAction.Dispose();
        jumpAction.Dispose();
    }

    // -------------------------------------------------------------------------

    /// <summary>Initializes movement parameters from config. Does NOT enable movement yet.</summary>
    public void Setup(ZombieRaceConfig raceConfig)
    {
        config            = raceConfig;
        isMovementEnabled = false;
    }

    /// <summary>Enables player movement — call this after the countdown ends.</summary>
    public void StartMovement() => isMovementEnabled = true;

    /// <summary>Freezes player movement — call this on race end or game over.</summary>
    public void StopMovement()
    {
        isMovementEnabled = false;
        velocity          = Vector3.zero;
    }

    // -------------------------------------------------------------------------

    private void Update()
    {
        if (!isMovementEnabled || config == null) return;

        ReadInputs();
        ApplyGravity();
        ApplyMovement();
    }

    private void ReadInputs()
    {
        moveInput   = moveAction.ReadValue<Vector2>();
        sprintHeld  = sprintAction.IsPressed();
        jumpPressed = jumpAction.WasPressedThisFrame();
    }

    private void ApplyGravity()
    {
        bool grounded = characterController.isGrounded;

        if (grounded && velocity.y < 0f)
            velocity.y = -2f;

        if (jumpPressed && grounded)
            velocity.y = Mathf.Sqrt(config.jumpHeight * -2f * config.gravity);

        velocity.y += config.gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }

    private void ApplyMovement()
    {
        float speed = sprintHeld ? config.playerSprintSpeed : config.playerMoveSpeed;

        // With a fixed overhead camera the camera orientation is unreliable as a movement
        // reference frame. Instead we use world-space axes directly: Z (forward on the track)
        // and X (lateral). The player model rotates to face the movement direction, which gives
        // the correct visual feedback regardless of camera angle.
        Vector3 moveDir = new Vector3(moveInput.x, 0f, moveInput.y).normalized;

        characterController.Move(moveDir * speed * Time.deltaTime);

        // Rotate the character to face the movement direction
        if (moveDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                config.playerRotationSpeed * Time.deltaTime);
        }
    }
}
