using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public class AcceptMenuSceneTransition : MonoBehaviour
{
    [Tooltip("The PlayableDirector (Timeline) for the accept menu cutscene")]
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
            Debug.LogError("AcceptMenuSceneTransition: No PlayableDirector found!");
        }
    }

    void OnTimelineEnd(PlayableDirector obj)
    {
        SceneManager.LoadScene("AfterStartMenuScene");
    }

    void OnDestroy()
    {
        if (director != null)
        {
            director.stopped -= OnTimelineEnd;
        }
    }
}
