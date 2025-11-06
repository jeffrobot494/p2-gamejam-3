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

    void OnCutsceneEnd(PlayableDirector obj)
    {
        SceneManager.LoadScene(nextSceneName);
    }
}
