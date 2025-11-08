using UnityEngine;

public class ParentToPlatform : MonoBehaviour
{


    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger Entered");
        if (other.CompareTag("Player"))
        {
            //parent the player to the platform
            other.transform.SetParent(transform);
        }
    }

    void OnTriggerExit(Collider other)
    {
        Debug.Log("Trigger Exited");
        if (other.CompareTag("Player"))
        {
            //unparent the player from the platform
            other.transform.SetParent(null);
        }
    }


}
