using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class ElevatorController : MonoBehaviour
{
    [Header("Elevator Positions")]
    [SerializeField] private Transform topPosition;
    [SerializeField] private Transform bottomPosition;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float startDelay = 3f;

    [Header("Audio Settings")]
    [Tooltip("Looping sound while the elevator is moving")]
    [SerializeField] private AudioClip movingSound;
    [Tooltip("Sound when elevator starts moving")]
    [SerializeField] private AudioClip startSound;
    [Tooltip("Sound when elevator stops")]
    [SerializeField] private AudioClip stopSound;
    [Tooltip("Volume for movement sounds")]
    [Range(0f, 1f)]
    [SerializeField] private float movementVolume = 1f;

    private bool isAtTop = false;
    private bool isMoving = false;

    private AudioSource audioSource;

    private void Awake()
    {
        // Set starting position to bottom
        if (bottomPosition != null)
            transform.position = bottomPosition.position;
        else
            Debug.LogWarning("[ElevatorController] Bottom position not assigned!");

        // Setup AudioSource
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // 3D sound
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        audioSource.minDistance = 1f;
        audioSource.maxDistance = 15f;
        audioSource.loop = false;
    }

    /// <summary>
    /// Public method to trigger elevator movement to the opposite position.
    /// Can be called from a button script or interaction event.
    /// </summary>
    public void TriggerElevator()
    {
        if (!isMoving)
            StartCoroutine(MoveElevatorRoutine());
    }

    private IEnumerator MoveElevatorRoutine()
    {
        isMoving = true;

        // Optional start delay
        yield return new WaitForSeconds(startDelay);

        // Play start sound
        if (startSound != null)
            audioSource.PlayOneShot(startSound, movementVolume);

        // Wait a short moment so startSound doesn’t overlap harshly
        yield return new WaitForSeconds(0.2f);

        // Start looping movement sound
        if (movingSound != null)
        {
            audioSource.clip = movingSound;
            audioSource.loop = true;
            audioSource.volume = movementVolume;
            audioSource.Play();
        }

        // Determine start and end points
        Vector3 startPos = transform.position;
        Vector3 targetPos = isAtTop ? bottomPosition.position : topPosition.position;

        float distance = Vector3.Distance(startPos, targetPos);
        float duration = distance / moveSpeed;
        float elapsed = 0f;

        // Move smoothly
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        // Snap to target
        transform.position = targetPos;
        isAtTop = !isAtTop;
        isMoving = false;

        // Stop movement sound with a quick fade out
        if (audioSource.isPlaying && movingSound != null)
            StartCoroutine(FadeOutSound(0.5f));

        // Play stop sound
        if (stopSound != null)
            audioSource.PlayOneShot(stopSound, movementVolume);
    }

    private IEnumerator FadeOutSound(float fadeDuration)
    {
        float startVolume = audioSource.volume;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeDuration);
            yield return null;
        }

        audioSource.Stop();
        audioSource.volume = movementVolume;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (topPosition != null && bottomPosition != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(topPosition.position, bottomPosition.position);
            Gizmos.DrawSphere(topPosition.position, 0.2f);
            Gizmos.DrawSphere(bottomPosition.position, 0.2f);
        }
    }
#endif
}
