using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public class OpeningSceneTransition : MonoBehaviour
{
    [Tooltip("The PlayableDirector (Timeline) for the opening cutscene")]
    public PlayableDirector director;

    void Start()
    {
        if (director == null)
            director = GetComponent<PlayableDirector>();

        if (director != null)
        {
            director.stopped += OnTimelineEnd;
        }
        else
        {
            Debug.LogError("OpeningSceneTransition: No PlayableDirector found!");
        }
    }

    void OnTimelineEnd(PlayableDirector obj)
    {
        SceneManager.LoadScene("StartMenuScene");
    }

    void OnDestroy()
    {
        if (director != null)
        {
            director.stopped -= OnTimelineEnd;
        }
    }
}
