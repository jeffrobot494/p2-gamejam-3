using UnityEngine;

public class AddCollidersToChildren : MonoBehaviour
{
    void Start()
    {
        // Loop through all children
        foreach (Transform child in transform)
        {
            if (child.GetComponent<Renderer>())
            {
                // Add a BoxCollider if missing
                if (!child.GetComponent<BoxCollider>())
                {
                    child.gameObject.AddComponent<BoxCollider>();
                }
            }
        }
    }
}
