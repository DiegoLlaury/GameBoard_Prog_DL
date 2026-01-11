using UnityEngine;

[CreateAssetMenu(fileName = "NewResource", menuName = "Scriptable Objects/ResourceData", order = 0)]
public class ResourceData : ScriptableObject
{
    [Header("Identity")]
    public string resourceId;          
    public string displayName;         
    [TextArea]
    public string description;

    [Header("Visual")]
    public Sprite icon;
    public Color uiColor = Color.white;

    [Header("Flags")]
    public bool persistentBetweenLevels = true;
}
