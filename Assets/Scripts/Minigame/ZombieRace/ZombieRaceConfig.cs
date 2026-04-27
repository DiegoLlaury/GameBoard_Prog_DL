using UnityEngine;

/// <summary>
/// ScriptableObject centralizing all tunable parameters for the Zombie Race minigame.
/// Create via Assets > Create > Scriptable Objects > ZombieRaceConfig.
/// </summary>
[CreateAssetMenu(fileName = "ZombieRaceConfig", menuName = "Scriptable Objects/ZombieRaceConfig")]
public class ZombieRaceConfig : ScriptableObject
{
    [Header("Timer")]
    [Tooltip("Time limit in seconds. The player must reach the finish before this runs out.")]
    public float timeLimit = 60f;

    [Header("Player Movement")]
    public float playerMoveSpeed = 6f;
    public float playerSprintSpeed = 10f;
    public float playerRotationSpeed = 200f;
    public float gravity = -20f;
    public float jumpHeight = 1.5f;

    [Header("Zombie")]
    [Tooltip("Base move speed of the pursuing zombie.")]
    public float zombieStartSpeed = 3.5f;

    [Tooltip("Speed the zombie reaches at the end of the time limit (ramps up over time).")]
    public float zombieMaxSpeed = 6f;

    [Header("Result Bonus (optional)")]
    [Tooltip("Resource awarded to the player on successful finish.")]
    public ResourceData finishReward;

    [Tooltip("Amount of the finish reward resource.")]
    public int finishRewardAmount = 2;

    [Tooltip("Bonus awarded for each second remaining when finishing (floor division).")]
    public ResourceData timeBonus;

    [Tooltip("1 unit of timeBonus per this many seconds remaining.")]
    public int timeBonusInterval = 10;
}
