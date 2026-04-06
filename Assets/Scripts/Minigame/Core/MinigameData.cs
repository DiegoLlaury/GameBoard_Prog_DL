using UnityEngine;

/// <summary>
/// ScriptableObject defining a minigame type.
/// Create one asset per minigame and add it to MinigameManager.availableMinigames.
/// </summary>
[CreateAssetMenu(fileName = "NewMinigame", menuName = "Scriptable Objects/MinigameData")]
public class MinigameData : ScriptableObject
{
    [Header("Identity")]
    public string minigameId;
    public string displayName;
    [TextArea] public string description;

    [Header("Scene")]
    public string sceneName;

    [Header("Visual")]
    public Sprite icon;
}
