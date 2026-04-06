// Legacy stub — board persistence is now handled by BoardSaveManager + BoardStateSerializer.
// Kept to avoid breaking existing asset references.
using System.IO;
using UnityEngine;

[System.Serializable]
public struct PlayerDatasStruct
{
    public int cellNumber;
    public int fleshNumber;
    public bool IsPlayerInMiniGame;
    public int MiniGameNumber;
}

public class SaveController
{
    public void SaveGameData(PlayerDatasStruct playerDatas, string filename)
    {
        string data = JsonUtility.ToJson(playerDatas);
        File.WriteAllText(Path.Combine(Application.persistentDataPath, filename), data);
    }

    public PlayerDatasStruct LoadGameData(string filename)
    {
        string path = Path.Combine(Application.persistentDataPath, filename);
        if (File.Exists(path))
        {
            string data = File.ReadAllText(path);
            return JsonUtility.FromJson<PlayerDatasStruct>(data);
        }

        var empty = new PlayerDatasStruct();
        SaveGameData(empty, filename);
        return empty;
    }
}
