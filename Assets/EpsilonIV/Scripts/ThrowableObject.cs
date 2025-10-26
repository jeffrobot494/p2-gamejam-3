using UnityEngine;

/// <summary>
/// Object that can be picked up and thrown by the player.
/// Emits sound when it collides with surfaces based on impact velocity.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class ThrowableObject : MonoBehaviour
{
    [Header("Pickup Settings")]
    [Tooltip("Tag used to identify this as a pickupable object")]
    [SerializeField] private string pickupTag = "Throwable";

    [Header("Sound Emission")]
    [Tooltip("Minimum impact velocity to emit sound (m/s)")]
    [SerializeField] private float minImpactVelocity = 1f;

    [Tooltip("Maximum impact velocity for loudness scaling (m/s)")]
    [SerializeField] private float maxImpactVelocity = 10f;

    [Tooltip("Minimum loudness when impact velocity = minImpactVelocity")]
    [Range(0f, 1f)]
    [SerializeField] private float minLoudness = 0.3f;

    [Tooltip("Maximum loudness when impact velocity >= maxImpactVelocity")]
    [Range(0f, 1f)]
    [SerializeField] private float maxLoudness = 0.8f;

    [Tooltip("Sound quality parameter passed to Sound system")]
    [SerializeField] private float soundQuality = 1f;

    [Header("Physics")]
    [Tooltip("Time in seconds before object can emit sound again after impact")]
    [SerializeField] private float soundCooldown = 0.2f;

    private Rigidbody rb;
    private Collider col;
    private SoundEmitter soundEmitter;
    private float lastSoundTime = -999f;
    private bool isHeld = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        soundEmitter = GetComponent<SoundEmitter>();

        // Set tag if not already set
        if (string.IsNullOrEmpty(gameObject.tag) || gameObject.tag == "Untagged")
        {
            gameObject.tag = pickupTag;
        }

        if (soundEmitter == null)
        {
            Debug.LogWarning($"[ThrowableObject] No SoundEmitter found on {gameObject.name}. Object won't emit sounds on impact.");
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Don't emit sound if being held or on cooldown
        if (isHeld || Time.time < lastSoundTime + soundCooldown)
            return;

        // Calculate impact velocity magnitude
        float impactVelocity = collision.relativeVelocity.magnitude;

        // Only emit sound if impact is strong enough
        if (impactVelocity >= minImpactVelocity)
        {
            // Scale loudness based on impact velocity
            float normalizedVelocity = Mathf.InverseLerp(minImpactVelocity, maxImpactVelocity, impactVelocity);
            float loudness = Mathf.Lerp(minLoudness, maxLoudness, normalizedVelocity);

            // Emit sound
            if (soundEmitter != null)
            {
                soundEmitter.EmitSound(loudness, soundQuality);
            }

            lastSoundTime = Time.time;
        }
    }

    /// <summary>
    /// Called by PlayerThrowController when picking up this object.
    /// Disables physics and collision.
    /// </summary>
    public void OnPickup()
    {
        isHeld = true;
        rb.isKinematic = true;
        col.enabled = false;
    }

    /// <summary>
    /// Called by PlayerThrowController when throwing this object.
    /// Enables physics and collision, applies throw force.
    /// </summary>
    /// <param name="throwForce">Force vector to apply to the object</param>
    public void OnThrow(Vector3 throwForce)
    {
        isHeld = false;
        rb.isKinematic = false;
        col.enabled = true;
        rb.AddForce(throwForce, ForceMode.Impulse);
    }

    /// <summary>
    /// Returns whether this object is currently being held by the player.
    /// </summary>
    public bool IsHeld => isHeld;

    /// <summary>
    /// Returns the rigidbody component.
    /// </summary>
    public Rigidbody Rigidbody => rb;
}
