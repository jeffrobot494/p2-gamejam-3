using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public class AfterStartMenuSceneTransition : MonoBehaviour
{
    [Tooltip("The PlayableDirector (Timeline) for the transition cutscene")]
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
            Debug.LogError("AfterStartMenuSceneTransition: No PlayableDirector found!");
        }
    }

    void OnTimelineEnd(PlayableDirector obj)
    {
        SceneManager.LoadScene("Final");
    }

    void OnDestroy()
    {
        if (director != null)
        {
            director.stopped -= OnTimelineEnd;
        }
    }
}
