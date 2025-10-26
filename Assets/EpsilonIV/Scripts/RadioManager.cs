using UnityEngine;

public class RadioManager : MonoBehaviour
{
    public GameObject[] npc_array;

    private int currentIndex = -1;

    void Start()
    {
        if (npc_array.Length == 0)
        {
            Debug.LogError("NPC Array is empty! Please assign GameObjects in the Inspector.");
            return;
        }


        DeactivateAllNpcs(); // for deatactivating all of the npcs in the array
    }

    // The Update function is called once per frame.
    void Update()
    {
        bool isShiftDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        // use shift+a for viewing the chat box or for viewing the next
        bool isADown = Input.GetKeyDown(KeyCode.A);

        // Input check for advancing the sequence (Shift + A)
        if (isShiftDown && isADown)
        {
            SwitchToNextNpc();
        }

        // Input check for deactivating the current NPC (Shift + R)
        if (isShiftDown && Input.GetKeyDown(KeyCode.R))
        {
            DeactivateCurrentNpc();
        }
    }

    // New function to deactivate the currently active NPC (Shift + R)
    public void DeactivateCurrentNpc()
    {
        // Check if an NPC is currently referenced (index is valid)
        if (currentIndex >= 0 && currentIndex < npc_array.Length)
        {
            if (npc_array[currentIndex] != null)
            {
                npc_array[currentIndex].SetActive(false);
                Debug.Log("Deactivated: " + npc_array[currentIndex].name + " (Index: " + currentIndex + ") using Shift+R.");
            }
        }
        else
        {
            Debug.LogWarning("No NPC is currently active or selected to deactivate.");
        }
        // Note: currentIndex is NOT changed, so Shift+A will advance to the next NPC.
    }

    public void SwitchToNextNpc()
    {
        // checking if the last element is reached
        if (currentIndex >= npc_array.Length - 1)
        {
            Debug.LogWarning("Reached the end of the NPC sequence. No more NPCs to activate.");
            return;
        }

        // switching to next and deactive the pre/curr one
        if (currentIndex >= 0 && currentIndex < npc_array.Length)
        {
            if (npc_array[currentIndex] != null)
            {
                npc_array[currentIndex].SetActive(false);
            }
        }


        currentIndex++;

        // for new character
        if (npc_array[currentIndex] != null)
        {
            npc_array[currentIndex].SetActive(true);
            Debug.Log("Switched to: " + npc_array[currentIndex].name + " (Index: " + currentIndex + ")");
        }
    }

    private void DeactivateAllNpcs()
    {
        foreach (GameObject npc in npc_array)
        {
            if (npc != null)
            {
                npc.SetActive(false);
            }
        }
    }
}