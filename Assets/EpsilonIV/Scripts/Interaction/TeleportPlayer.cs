using UnityEngine;
using UnityEngine.UI;

public class TeleportButtonHighlight : MonoBehaviour
{
    [Header("References")]
    public GameObject player;
    public Transform targetLocation;
    public Text teleportText;

    [Header("Materials")]
    public Material normalMaterial;
    public Material highlightMaterial;

    private Renderer rend;

    void Start()
    {
        rend = GetComponent<Renderer>();
        rend.material = normalMaterial;
        Debug.Log("Ran");
        if (teleportText != null)
            teleportText.gameObject.SetActive(false);
            
    }

    void OnMouseEnter()
    {
        Debug.Log("Mouse entered");
        rend.material = highlightMaterial;
        if (teleportText != null)
            teleportText.gameObject.SetActive(true);
            
    }

    void OnMouseExit()
    {
        rend.material = normalMaterial;
        if (teleportText != null)
            teleportText.gameObject.SetActive(false);
    }

    void OnMouseDown()
    {

        Debug.Log(player);
        Debug.Log(targetLocation);

        if (player != null && targetLocation != null)
        {
            Debug.Log("Triggered");
            CharacterController cc = player.GetComponent<CharacterController>();
            cc.enabled = false; // temporarily disable controller
            player.transform.position = targetLocation.position;
            cc.enabled = true;

        }
    }
}
