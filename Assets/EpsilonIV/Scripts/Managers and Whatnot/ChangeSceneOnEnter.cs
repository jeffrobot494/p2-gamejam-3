using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadSceneAfterAudio : MonoBehaviour
{
    [Tooltip("Name of the scene to load after audio finishes")]
    public string nextSceneName = "StartMenuScene";

    [Tooltip("Audio clip to play before loading the scene")]
    public AudioClip startSound;

    private AudioSource audioSource;
    private bool hasStarted = false;

    void Start()
    {
        // Get or create an AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
    }

    void Update()
    {
        if (!hasStarted && Input.GetKeyDown(KeyCode.Return))
        {
            hasStarted = true;

            if (startSound != null)
            {
                audioSource.clip = startSound;
                audioSource.Play();
                Invoke(nameof(LoadNextScene), startSound.length);
            }
            else
            {
                // If no sound assigned, load scene immediately
                LoadNextScene();
            }
        }
    }

    void LoadNextScene()
    {
        SceneManager.LoadScene(nextSceneName);
    }
}
