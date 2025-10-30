using UnityEngine;

public class TeleportButton : MonoBehaviour
{
    [Header("Assign your player object here")]
    public GameObject player;

    [Header("Assign the target location (empty GameObject in scene)")]
    public Transform targetLocation;

    private void OnMouseDown()
    {
        if (player != null && targetLocation != null)
        {
            // Instantly move player to target position
            player.transform.position = targetLocation.position;

            Debug.Log("Player teleported to " + targetLocation.position);
        }
        else
        {
            Debug.LogWarning("Player or target location not assigned!");
        }
    }
}
