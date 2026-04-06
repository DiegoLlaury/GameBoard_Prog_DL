/// <summary>
/// Contract for all minigame controllers.
/// Implement this interface to create a new minigame type.
/// </summary>
public interface IMinigame
{
    /// <summary>Called once when the minigame scene is loaded.</summary>
    void Initialize();

    /// <summary>Starts the gameplay loop.</summary>
    void StartGame();

    /// <summary>Ends the gameplay loop and cleans up.</summary>
    void EndGame();

    /// <summary>Returns the result of the minigame after it ends.</summary>
    MinigameResult GetResult();
}
