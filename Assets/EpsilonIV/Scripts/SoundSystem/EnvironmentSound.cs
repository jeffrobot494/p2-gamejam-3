using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class EnvironmentSound : MonoBehaviour
{
    [Header("Sound Clips")]
    [Tooltip("One or more audio clips to choose from. If more than one, it picks randomly.")]
    public AudioClip[] ambientClips;

    [Header("Playback Settings")]
    [Tooltip("Should the sound loop continuously?")]
    public bool loop = true;

    [Tooltip("If not looping, random delay (seconds) before next play.")]
    public Vector2 randomDelayRange = new Vector2(5f, 10f);

    [Range(0f, 1f)] 
    public float volume = 1f;

    [Header("3D Sound Settings")]
    [Range(0f, 1f)]
    public float spatialBlend = 1f; // 1 = fully 3D
    public float minDistance = 2f;
    public float maxDistance = 20f;
    public AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;

    private AudioSource source;
    private Coroutine playRoutine;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.volume = volume;
        source.spatialBlend = spatialBlend;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.rolloffMode = rolloffMode;
    }

    private void Start()
    {
        if (ambientClips == null || ambientClips.Length == 0)
        {
            Debug.LogWarning("[EnvironmentSound] No audio clips assigned.", this);
            return;
        }

        if (loop)
        {
            source.clip = ambientClips[Random.Range(0, ambientClips.Length)];
            source.Play();
        }
        else
        {
            playRoutine = StartCoroutine(PlayRandomly());
        }
    }

    private IEnumerator PlayRandomly()
    {
        while (true)
        {
            AudioClip clip = ambientClips[Random.Range(0, ambientClips.Length)];
            source.PlayOneShot(clip, volume);
            yield return new WaitForSeconds(clip.length + Random.Range(randomDelayRange.x, randomDelayRange.y));
        }
    }

    private void OnDisable()
    {
        if (playRoutine != null)
            StopCoroutine(playRoutine);
    }
}
