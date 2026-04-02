using UnityEngine;

[CreateAssetMenu(fileName = "DialogueData", menuName = "Scriptable Objects/DialogueData")]
public class DialogueDatas : ScriptableObject
{
    public string characterName;
    public Sprite characterImage;

    [TextArea(2, 10)]
    public string[] dialogues; // dialogues lin�aires

    [System.Serializable]
    public class DialogueChoice
    {
        public string choiceText;
        public string consequenceText; // ce qui se passe si choisi
        public int effectOnPlayer; // optionnel : +1 mouvement, etc

        [Header("Resource Cost")]
        public ResourceData costResource;
        public int costAmount;

        [Header("Resource Reward")]
        public ResourceData rewardResource;
        public int rewardAmount;

        [Header("Dice Reward")]
        public Dice rewardDicePrefab;
        public int rewardDiceCount = 1;
    }

    public DialogueChoice[] choices; // si vide -> pas de choix
}
