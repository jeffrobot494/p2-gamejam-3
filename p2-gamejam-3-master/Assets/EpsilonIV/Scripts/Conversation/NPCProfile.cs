using UnityEngine;

[CreateAssetMenu(menuName = "Radio/NPC Profile", fileName = "NewNPCProfile")]
public class NPCProfile : ScriptableObject
{
    public string npcName;
    public bool missionComplete = false;
}
