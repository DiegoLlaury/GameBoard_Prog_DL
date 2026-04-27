using System;
using UnityEngine;
using UnityEngine.AI;

public enum ZombieState
{
    Patrol,
    Chase,
    Scream,
    Jump,
}

/// <summary>
/// Zombie AI controller.
/// States: Patrol → Chase → Jump.
/// Attack is an overlay (upper body layer) that fires during Chase without stopping.
/// Scream: patrol-only periodic sound wave detection, waits for animation to finish.
/// Jump: smooth parabolic arc with instant animation via CrossFade.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SightPerception))]
public class ZombieIAController : MonoBehaviour
{
    [Header("Patrol")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float waypointReachedDistance = 0.5f;
    [SerializeField] private float patrolSpeed = 2f;

    [Header("Chase")]
    [SerializeField] private float chaseSpeed = 5f;
    [SerializeField] private float losePlayerDelay = 3f;

    [Header("Attack (Upper Body Overlay)")]
    [SerializeField] private float attackDistance = 1.5f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float attackAnimDuration = 1f;

    [Header("Scream")]
    [SerializeField] private float screamRadius = 10f;
    [SerializeField] private float screamInterval = 8f;
    [SerializeField] private float screamAnimDuration = 2f;
    [SerializeField] private float minPatrolBeforeScream = 3f;
    [SerializeField] private LayerMask obstacleLayerMask = ~0;

    [Header("Jump")]
    [SerializeField] private float jumpTriggerDistance = 6f;
    [SerializeField] private float jumpCooldown = 5f;
    [SerializeField] private float jumpDuration = 0.8f;
    [SerializeField] private float jumpArcHeight = 2.5f;
    [SerializeField] private float jumpAnimSpeedMultiplier = 1.5f;

    private const int UPPER_BODY_LAYER = 1;
    private const float JUMP_CROSSFADE_DURATION = 0.05f;
    private const float ATTACK_CROSSFADE_DURATION = 0.1f;
    private const float LAYER_BLEND_SPEED = 10f;
    private const string JUMP_STATE_NAME = "Jump";
    private const string ATTACK_STATE_NAME = "Attack";
    private const string EMPTY_STATE_NAME = "Empty";

    private static readonly int AnimSpeed  = Animator.StringToHash("Speed");
    private static readonly int AnimScream = Animator.StringToHash("Scream");

    private NavMeshAgent    agent;
    private Animator        animator;
    private SightPerception sight;

    private ZombieState state = ZombieState.Patrol;
    private int   currentWaypoint;
    private float losePlayerTimer;
    private float attackCooldownTimer;
    private float screamTimer;
    private float jumpCooldownTimer;

    // Scream — stores result until animation finishes
    private bool screamDetectedPlayer;
    private float patrolTimer;

    // Jump — arc interpolation data
    private Vector3 jumpStartPos;
    private Vector3 jumpTargetPos;
    private float   jumpElapsed;

    // Attack — upper body layer weight timer
    private float attackLayerTimer;

    private Transform playerTransform;

    /// <summary>Fired when an attack successfully lands on a target in range.</summary>
    public event Action<Transform> OnAttackLanded;

    /// <summary>
    /// When true, the zombie skips Patrol entirely and chases the player immediately.
    /// Scream and waypoints are disabled. Used by ZombieRaceMinigame.
    /// </summary>
    private bool pursuitOnlyMode;

    // Base values for difficulty scaling
    private float basePatrolSpeed;
    private float baseChaseSpeed;
    private float baseAttackCooldown;
    private float baseScreamInterval;

    private void Awake()
    {
        agent    = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        sight    = GetComponent<SightPerception>();

        // Store base values before any difficulty scaling
        basePatrolSpeed    = patrolSpeed;
        baseChaseSpeed     = chaseSpeed;
        baseAttackCooldown = attackCooldown;
        baseScreamInterval = screamInterval;
    }

    private void Start()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null) playerTransform = player.transform;

        screamTimer = screamInterval;

        // Upper Body layer starts inactive (no attack playing)
        animator.SetLayerWeight(UPPER_BODY_LAYER, 0f);

        TransitionTo(pursuitOnlyMode ? ZombieState.Chase : ZombieState.Patrol);
    }

    private void Update()
    {
        attackCooldownTimer -= Time.deltaTime;
        jumpCooldownTimer   -= Time.deltaTime;
        screamTimer         -= Time.deltaTime;

        UpdateAttackLayer();

        switch (state)
        {
            case ZombieState.Patrol:  UpdatePatrol();  break;
            case ZombieState.Chase:   UpdateChase();   break;
            case ZombieState.Jump:    UpdateJump();    break;
            // Scream: waiting for OnScreamFinished callback
        }

        UpdateAnimator();
    }

    // -------------------------------------------------------------------------
    // Attack — upper body overlay (independent of state machine)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Smoothly blends the upper body layer weight.
    /// Uses animator state info to detect when the Attack clip is playing,
    /// avoiding reliance on Write Defaults in the Empty state.
    /// </summary>
    private void UpdateAttackLayer()
    {
        float currentWeight = animator.GetLayerWeight(UPPER_BODY_LAYER);
        float targetWeight = attackLayerTimer > 0f ? 1f : 0f;

        if (attackLayerTimer > 0f)
            attackLayerTimer -= Time.deltaTime;

        float newWeight = Mathf.MoveTowards(currentWeight, targetWeight, LAYER_BLEND_SPEED * Time.deltaTime);
        animator.SetLayerWeight(UPPER_BODY_LAYER, newWeight);
    }

    /// <summary>
    /// Fires the attack animation on the upper body layer via CrossFade.
    /// Bypasses the Empty state entirely — no Write Defaults T-Pose flash.
    /// Does not interrupt movement or state transitions.
    /// </summary>
    private void TriggerAttack()
    {
        attackCooldownTimer = attackCooldown;
        attackLayerTimer    = attackAnimDuration;

        // CrossFade directly into Attack on layer 1 — bypasses the trigger system
        // and the Empty state, so Write Defaults on Empty never causes T-Pose
        animator.CrossFadeInFixedTime(ATTACK_STATE_NAME, ATTACK_CROSSFADE_DURATION, UPPER_BODY_LAYER);
        animator.SetLayerWeight(UPPER_BODY_LAYER, 1f);

        // Notify subscribers (MinigamePlayerHealth) that the attack landed
        if (playerTransform != null &&
            Vector3.Distance(transform.position, playerTransform.position) <= attackDistance)
        {
            OnAttackLanded?.Invoke(playerTransform);
        }
    }

    // -------------------------------------------------------------------------
    // Patrol
    // -------------------------------------------------------------------------

    private void UpdatePatrol()
    {
        patrolTimer += Time.deltaTime;

        // In pursuit-only mode, skip scream and go straight to chase as soon as possible
        if (pursuitOnlyMode)
        {
            TransitionTo(ZombieState.Chase);
            return;
        }

        // Scream can only happen after patrolling for a minimum duration
        if (screamTimer <= 0f && patrolTimer >= minPatrolBeforeScream)
        {
            TriggerScream();
            return;
        }

        if (sight.IsPlayerDetected)
        {
            TransitionTo(ZombieState.Chase);
            return;
        }

        if (waypoints == null || waypoints.Length == 0) return;

        if (!agent.pathPending && agent.remainingDistance <= waypointReachedDistance)
            AdvanceWaypoint();
    }

    // -------------------------------------------------------------------------
    // Chase — attack happens here as an overlay, zombie keeps moving
    // -------------------------------------------------------------------------

    private void UpdateChase()
    {
        // In pursuit-only mode, always chase the player directly without losing target
        if (pursuitOnlyMode)
        {
            if (playerTransform != null)
            {
                agent.SetDestination(playerTransform.position);

                float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);

                if (distToPlayer <= attackDistance && attackCooldownTimer <= 0f)
                    TriggerAttack();

                if (distToPlayer >= jumpTriggerDistance && jumpCooldownTimer <= 0f)
                    TriggerJump();
            }
            return;
        }

        if (sight.IsPlayerDetected)
        {
            losePlayerTimer = losePlayerDelay;
            agent.SetDestination(sight.LastKnownPosition);

            float distToPlayer = Vector3.Distance(transform.position, sight.LastKnownPosition);

            // Attack while chasing — upper body only, legs keep running
            if (distToPlayer <= attackDistance && attackCooldownTimer <= 0f)
            {
                TriggerAttack();
            }

            // Jump if player is too far but still in sight
            if (distToPlayer >= jumpTriggerDistance && jumpCooldownTimer <= 0f)
            {
                TriggerJump();
                return;
            }
        }
        else
        {
            losePlayerTimer -= Time.deltaTime;
            if (losePlayerTimer <= 0f)
            {
                TransitionTo(ZombieState.Patrol);
                return;
            }
            agent.SetDestination(sight.LastKnownPosition);
        }
    }

    // -------------------------------------------------------------------------
    // Scream — patrol-only, deferred reaction after animation finishes
    // -------------------------------------------------------------------------

    /// <summary>
    /// Stops the zombie, plays the scream animation, and evaluates whether
    /// the player is inside the sound wave (no obstacles). The zombie only
    /// reacts AFTER the animation finishes via OnScreamFinished.
    /// </summary>
    private void TriggerScream()
    {
        screamTimer          = screamInterval;
        screamDetectedPlayer = false;

        TransitionTo(ZombieState.Scream);
        animator.SetTrigger(AnimScream);

        // Evaluate detection immediately, but only store the result
        if (playerTransform != null)
        {
            float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            if (distToPlayer <= screamRadius)
            {
                Vector3 origin  = transform.position + Vector3.up;
                Vector3 target  = playerTransform.position + Vector3.up;
                Vector3 dir     = (target - origin).normalized;
                float   rayDist = Vector3.Distance(origin, target);

                // Raycast checks for obstacles between zombie and player.
                // If the first hit IS the player, there is no obstacle blocking.
                // If nothing is hit at all, the path is clear too.
                if (Physics.Raycast(origin, dir, out RaycastHit hit, rayDist,
                                    obstacleLayerMask, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider.CompareTag("Player"))
                    {
                        screamDetectedPlayer = true;
                        sight.ForceDetect(playerTransform.position);
                    }
                    // else: an actual obstacle blocks the path — no detection
                }
                else
                {
                    // Nothing hit at all — clear line of sight
                    screamDetectedPlayer = true;
                    sight.ForceDetect(playerTransform.position);
                }
            }
        }
    }

    /// <summary>
    /// Called via Invoke after screamAnimDuration seconds.
    /// Transitions to Chase if player was detected, otherwise back to Patrol.
    /// </summary>
    private void OnScreamFinished()
    {
        if (state != ZombieState.Scream) return;

        TransitionTo(screamDetectedPlayer ? ZombieState.Chase : ZombieState.Patrol);
    }

    // -------------------------------------------------------------------------
    // Jump — smooth parabolic arc with instant animation start
    // -------------------------------------------------------------------------

    /// <summary>
    /// Launches the zombie in a parabolic arc toward the player.
    /// Uses CrossFadeInFixedTime for instant animation start (no 0.25s blend delay).
    /// NavMeshAgent is fully disabled during flight.
    /// </summary>
    private void TriggerJump()
    {
        if (playerTransform == null) return;

        jumpCooldownTimer = jumpCooldown;

        // Snapshot start and target positions
        jumpStartPos  = transform.position;
        jumpTargetPos = playerTransform.position;
        jumpElapsed   = 0f;

        // Face the target before takeoff
        Vector3 flatDir = jumpTargetPos - jumpStartPos;
        flatDir.y = 0f;
        if (flatDir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(flatDir);

        // Disconnect NavMeshAgent so it doesn't override position
        agent.isStopped      = true;
        agent.updatePosition = false;
        agent.updateRotation = false;

        // Instant animation start via CrossFade (bypasses the 0.25s trigger transition)
        animator.CrossFadeInFixedTime(JUMP_STATE_NAME, JUMP_CROSSFADE_DURATION, 0);
        animator.speed = jumpAnimSpeedMultiplier;

        state = ZombieState.Jump;
    }

    private void UpdateJump()
    {
        jumpElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(jumpElapsed / jumpDuration);

        // Horizontal interpolation between start and target
        Vector3 pos = Vector3.Lerp(jumpStartPos, jumpTargetPos, t);

        // Vertical parabolic arc peaking at t = 0.5
        float arc = jumpArcHeight * 4f * t * (1f - t);
        pos.y += arc;

        transform.position = pos;

        if (t >= 1f)
            LandJump();
    }

    private void LandJump()
    {
        // Restore normal animation speed
        animator.speed = 1f;

        // Re-enable NavMeshAgent at the landing position
        agent.Warp(transform.position);
        agent.updatePosition = true;
        agent.updateRotation = true;
        agent.isStopped      = false;

        TransitionTo(ZombieState.Chase);
    }

    // -------------------------------------------------------------------------
    // Transitions
    // -------------------------------------------------------------------------

    private void TransitionTo(ZombieState newState)
    {
        ExitState(state);
        state = newState;
        EnterState(state);
    }

    private void EnterState(ZombieState entering)
    {
        switch (entering)
        {
            case ZombieState.Patrol:
                agent.speed     = patrolSpeed;
                agent.isStopped = false;
                patrolTimer     = 0f;
                screamTimer     = Mathf.Max(screamTimer, minPatrolBeforeScream);
                GoToCurrentWaypoint();
                break;

            case ZombieState.Chase:
                agent.speed     = chaseSpeed;
                agent.isStopped = false;
                losePlayerTimer = losePlayerDelay;
                break;

            case ZombieState.Scream:
                agent.isStopped = true;
                Invoke(nameof(OnScreamFinished), screamAnimDuration);
                break;

            // Jump: enter is handled entirely in TriggerJump()
        }
    }

    private void ExitState(ZombieState exiting)
    {
        switch (exiting)
        {
            case ZombieState.Scream:
                CancelInvoke(nameof(OnScreamFinished));
                agent.isStopped = false;
                break;

            case ZombieState.Jump:
                animator.speed = 1f;
                agent.updatePosition = true;
                agent.updateRotation = true;
                agent.isStopped = false;
                break;
        }
    }

    // -------------------------------------------------------------------------
    // Patrol helpers
    // -------------------------------------------------------------------------

    private void AdvanceWaypoint()
    {
        currentWaypoint = (currentWaypoint + 1) % waypoints.Length;
        GoToCurrentWaypoint();
    }

    private void GoToCurrentWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        agent.SetDestination(waypoints[currentWaypoint].position);
    }

    // -------------------------------------------------------------------------
    // Pursuit-only mode (ZombieRace)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Enables pursuit-only mode: the zombie skips Patrol, ignores waypoints and scream,
    /// and immediately chases the player at all times. Call this before enabling the component.
    /// </summary>
    public void EnablePursuitMode()
    {
        pursuitOnlyMode = true;

        // If already running, switch to Chase immediately
        if (state != ZombieState.Chase)
            TransitionTo(ZombieState.Chase);
    }

    /// <summary>
    /// Disables pursuit-only mode and returns the zombie to normal Patrol/Chase behaviour.
    /// </summary>
    public void DisablePursuitMode()
    {
        pursuitOnlyMode = false;
        TransitionTo(ZombieState.Patrol);
    }

    // -------------------------------------------------------------------------
    // Animator
    // -------------------------------------------------------------------------

    private void UpdateAnimator()
    {
        bool frozen = state == ZombieState.Scream
                   || state == ZombieState.Jump;
        animator.SetFloat(AnimSpeed, frozen ? 0f : agent.velocity.magnitude);
    }

    // -------------------------------------------------------------------------
    // Gizmos
    // -------------------------------------------------------------------------

    private void OnDrawGizmosSelected()
    {
        // Scream radius (cyan)
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, screamRadius);

        // Jump trigger distance (magenta)
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, jumpTriggerDistance);

        // Attack distance (red)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);
    }

    // -------------------------------------------------------------------------
    // Difficulty API (called by DifficultyScaler)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Applies difficulty multipliers relative to the base Inspector values.
    /// Called each frame by DifficultyScaler during a minigame.
    /// </summary>
    public void SetDifficultyMultipliers(float speedMult, float attackRateMult, float screamFreqMult)
    {
        patrolSpeed    = basePatrolSpeed    * speedMult;
        chaseSpeed     = baseChaseSpeed     * speedMult;
        attackCooldown = baseAttackCooldown / Mathf.Max(attackRateMult, 0.01f);
        screamInterval = baseScreamInterval / Mathf.Max(screamFreqMult, 0.01f);

        // Apply speed immediately if currently chasing or patrolling
        if (state == ZombieState.Chase)
            agent.speed = chaseSpeed;
        else if (state == ZombieState.Patrol)
            agent.speed = patrolSpeed;
    }

    /// <summary>
    /// Resets all difficulty multipliers back to base Inspector values.
    /// </summary>
    public void ResetDifficulty()
    {
        patrolSpeed    = basePatrolSpeed;
        chaseSpeed     = baseChaseSpeed;
        attackCooldown = baseAttackCooldown;
        screamInterval = baseScreamInterval;
    }

    /// <summary>
    /// Overrides both patrol and chase speed to the given absolute value.
    /// Used by ZombieRaceMinigame to ramp up pursuit speed over time.
    /// </summary>
    public void SetMoveSpeed(float speed)
    {
        patrolSpeed = speed;
        chaseSpeed  = speed;

        if (state == ZombieState.Chase)
            agent.speed = chaseSpeed;
        else if (state == ZombieState.Patrol)
            agent.speed = patrolSpeed;
    }
}
