using System.Collections;
using UnityEngine;

/// <summary>
/// Leap attack behavior for aliens.
/// Launches the alien in an arc toward the target position.
/// Attack range is randomized between min/max to keep player guessing.
/// </summary>
public class LeapAttack : MonoBehaviour, IAlienAttack
{
    [Header("Attack Range")]
    [Tooltip("Minimum range for leap attack")]
    [SerializeField] private float attackRangeMin = 3f;

    [Tooltip("Maximum range for leap attack")]
    [SerializeField] private float attackRangeMax = 5f;

    [Header("Leap Settings")]
    [Tooltip("Speed of leap in units per second")]
    [SerializeField] private float leapSpeed = 8f;

    [Tooltip("Height of leap arc")]
    [SerializeField] private float leapHeight = 2f;

    [Header("Cooldown")]
    [Tooltip("Cooldown between attacks in seconds")]
    [SerializeField] private float attackCooldown = 3f;

    [Header("Damage")]
    [Tooltip("Child GameObject with trigger collider and DamageOnTouch (will be activated during leap)")]
    [SerializeField] private GameObject attackZone;

    [Header("Debug")]
    [Tooltip("Show debug logs for attack behavior")]
    [SerializeField] private bool debugMode = true;

    // State tracking
    private float currentAttackRange;
    private float lastAttackTime = -999f;
    private bool isLeaping = false;
    private Vector3 leapStartPosition;
    private Vector3 leapTargetPosition;

    private void Awake()
    {
        // Initialize with random attack range
        RandomizeAttackRange();

        // Ensure attack zone starts disabled
        if (attackZone != null)
        {
            attackZone.SetActive(false);
        }
    }

    public float GetAttackRange()
    {
        return currentAttackRange;
    }

    public bool CanAttack(Vector3 targetPosition)
    {
        // Check if off cooldown and not currently attacking
        bool ready = Time.time >= lastAttackTime + attackCooldown && !isLeaping;

        if (debugMode && !ready)
        {
            float timeLeft = (lastAttackTime + attackCooldown) - Time.time;
            Debug.Log($"[LeapAttack] CanAttack: false (Cooldown: {timeLeft:F1}s remaining, IsLeaping: {isLeaping})");
        }

        return ready;
    }

    public void ExecuteAttack(Vector3 targetPosition)
    {
        if (debugMode)
        {
            Debug.Log($"[LeapAttack] ExecuteAttack called! Target: {targetPosition}, Current Range: {currentAttackRange}");
        }

        // Store leap info
        leapStartPosition = transform.position;
        leapTargetPosition = targetPosition;

        // Start leap
        isLeaping = true;
        lastAttackTime = Time.time;

        StartCoroutine(PerformLeap());
    }

    public bool IsAttackComplete()
    {
        return !isLeaping;
    }

    private IEnumerator PerformLeap()
    {
        // Calculate duration based on distance and speed
        float distance = Vector3.Distance(leapStartPosition, leapTargetPosition);
        float leapDuration = distance / leapSpeed;

        if (debugMode)
        {
            Debug.Log($"[LeapAttack] Starting leap from {leapStartPosition} to {leapTargetPosition} (Distance: {distance:F2}, Duration: {leapDuration:F2}s)");
        }

        // Activate attack zone during leap
        if (attackZone != null)
        {
            attackZone.SetActive(true);
        }

        float elapsedTime = 0f;

        while (elapsedTime < leapDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / leapDuration;

            // Linear horizontal movement
            Vector3 horizontalPosition = Vector3.Lerp(leapStartPosition, leapTargetPosition, progress);

            // Arc vertical movement (parabola)
            float heightOffset = leapHeight * Mathf.Sin(progress * Mathf.PI);

            // Combine horizontal and vertical
            Vector3 newPosition = horizontalPosition + Vector3.up * heightOffset;
            transform.position = newPosition;

            // Make alien face direction of movement
            if (progress < 0.9f) // Don't rotate at very end
            {
                Vector3 direction = (leapTargetPosition - leapStartPosition).normalized;
                if (direction != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(direction);
                }
            }

            yield return null;
        }

        // Ensure we end exactly at target
        Vector3 finalPosition = leapTargetPosition;
        finalPosition.y = leapStartPosition.y; // Keep on same Y level
        transform.position = finalPosition;

        if (debugMode)
        {
            Debug.Log($"[LeapAttack] Leap complete! Landed at {transform.position}");
        }

        // Deactivate attack zone
        if (attackZone != null)
        {
            attackZone.SetActive(false);
        }

        // Attack complete
        isLeaping = false;

        // Randomize attack range for next attack
        RandomizeAttackRange();
    }

    private void RandomizeAttackRange()
    {
        currentAttackRange = Random.Range(attackRangeMin, attackRangeMax);

        if (debugMode)
        {
            Debug.Log($"[LeapAttack] Attack range randomized to: {currentAttackRange:F2}f");
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize current attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, currentAttackRange);

        // Visualize attack range bounds
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, attackRangeMin);
        Gizmos.DrawWireSphere(transform.position, attackRangeMax);

        // If leaping, show path
        if (Application.isPlaying && isLeaping)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(leapStartPosition, leapTargetPosition);
            Gizmos.DrawWireSphere(leapTargetPosition, 0.5f);
        }
    }
}
