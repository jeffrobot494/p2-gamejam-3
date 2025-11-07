using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public class CutsceneEndSceneLoader : MonoBehaviour
{
    public PlayableDirector director;
    public string nextSceneName = "StartMenuScene";

    void Start()
    {
        if (director == null)
            director = GetComponent<PlayableDirector>();

        director.stopped += OnCutsceneEnd;
    }

    void Update()
    {
        //if enter pressed, skip
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            OnCutsceneEnd(director);
        }
    }

    void OnCutsceneEnd(PlayableDirector obj)
    {
        SceneManager.LoadScene(nextSceneName);
    }
}
