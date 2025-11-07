using UnityEngine;
using System.Collections;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [Header("Audio Settings")]
    public float fadeTime = 2f;

    private AudioSource musicSource;
    private Coroutine currentFade;

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // AudioSource setup
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f; // 2D sound
    }

    public void PlayMusic(AudioClip clip, float targetVolume = 0.5f)
    {
        if (clip == null) return;

        // Stop previous fade if active
        if (currentFade != null)
            StopCoroutine(currentFade);

        if (musicSource.clip == clip && musicSource.isPlaying)
            return; // already playing this track

        // Start new track
        currentFade = StartCoroutine(FadeToNewClip(clip, targetVolume));
    }

    private IEnumerator FadeToNewClip(AudioClip newClip, float targetVolume)
    {
        float startVol = musicSource.volume;

        // Fade out current track
        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            musicSource.volume = Mathf.Lerp(startVol, 0f, t / fadeTime);
            yield return null;
        }
        musicSource.volume = 0f;

        musicSource.clip = newClip;
        musicSource.Play();

        // Fade in new track
        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            musicSource.volume = Mathf.Lerp(0f, targetVolume, t / fadeTime);
            yield return null;
        }
        musicSource.volume = targetVolume;

        currentFade = null;
    }
}
