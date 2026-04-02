using UnityEngine;
using System.IO;
using Unity.Loading;

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

        string path = Application.persistentDataPath + "/" + filename;

        File.WriteAllText(path, data);
    }

    public PlayerDatasStruct LoadGameData(string filename)
    {
        PlayerDatasStruct playerDatas = new PlayerDatasStruct();

        string path = Application.persistentDataPath + "/" + filename;

        if (File.Exists(path))
        {
            string data = File.ReadAllText(path);
            playerDatas = JsonUtility.FromJson<PlayerDatasStruct>(data);
        }
        else
        {
            SaveGameData(playerDatas, filename);
        }

        return playerDatas;
    }
}
