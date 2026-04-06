using UnityEngine;

/// <summary>
/// Evaluates difficulty curves over elapsed time and applies multipliers
/// to ZombieIAController and SightPerception.
/// </summary>
public class DifficultyScaler : MonoBehaviour
{
    private ZombieSurvivalConfig config;
    private ZombieIAController[] zombies;
    private SightPerception[] sights;
    private float elapsedTime;
    private bool isActive;

    /// <summary>Current difficulty multiplier for UI display.</summary>
    public float CurrentSpeedMultiplier { get; private set; } = 1f;

    /// <summary>Initializes the scaler with config and zombie references.</summary>
    public void Setup(ZombieSurvivalConfig cfg, ZombieIAController[] zombieControllers, SightPerception[] sightComponents)
    {
        config = cfg;
        zombies = zombieControllers;
        sights = sightComponents;
        elapsedTime = 0f;
        isActive = true;
    }

    /// <summary>Stops difficulty scaling.</summary>
    public void Stop()
    {
        isActive = false;
    }

    private void Update()
    {
        if (!isActive || config == null) return;

        elapsedTime += Time.deltaTime;

        float speedMult    = config.speedCurve.Evaluate(elapsedTime);
        float detectMult   = config.detectionCurve.Evaluate(elapsedTime);
        float attackMult   = config.attackRateCurve.Evaluate(elapsedTime);
        float screamMult   = config.screamFreqCurve.Evaluate(elapsedTime);

        CurrentSpeedMultiplier = speedMult;

        if (zombies != null)
        {
            foreach (var zombie in zombies)
            {
                if (zombie != null)
                    zombie.SetDifficultyMultipliers(speedMult, attackMult, screamMult);
            }
        }

        if (sights != null)
        {
            foreach (var sight in sights)
            {
                if (sight != null)
                    sight.SetDetectionMultiplier(detectMult);
            }
        }
    }
}
