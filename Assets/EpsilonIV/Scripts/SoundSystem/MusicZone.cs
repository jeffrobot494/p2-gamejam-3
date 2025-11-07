using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class MusicZoneTrigger : MonoBehaviour
{
    [Header("Zone Music")]
    public AudioClip zoneMusic;
    [Range(0f, 1f)] public float targetVolume = 0.5f;

    private void Awake()
    {
        var col = GetComponent<BoxCollider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            MusicManager.Instance?.PlayMusic(zoneMusic, targetVolume);
        }
    }
}
