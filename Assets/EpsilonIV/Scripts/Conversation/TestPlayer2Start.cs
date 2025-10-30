using UnityEngine;
using player2_sdk;

public class TestPlayer2Start : MonoBehaviour
{
    public NpcManager npcManager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        AuthenticationUI.Setup(npcManager);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
