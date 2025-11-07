using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenuSceneTransition : MonoBehaviour
{
    private bool hasTransitioned = false;

    void Update()
    {
        if (!hasTransitioned && Input.GetKeyDown(KeyCode.Return))
        {
            hasTransitioned = true;
            SceneManager.LoadScene("AcceptMenuScene");
        }
    }
}
