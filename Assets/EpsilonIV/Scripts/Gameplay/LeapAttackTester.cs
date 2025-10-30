using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Simple test script to manually trigger leap attacks.
/// Attach to alien GameObject alongside LeapAttack component.
/// Press T to leap toward a target position.
/// </summary>
public class LeapAttackTester : MonoBehaviour
{
    [Header("Test Settings")]
    [Tooltip("Target position to leap toward (can be another GameObject or marker)")]
    [SerializeField] private Transform targetPosition;

    [Tooltip("If no target set, leap this many units forward")]
    [SerializeField] private float defaultLeapDistance = 4f;

    private IAlienAttack leapAttack;
    private InputAction leapAction;

    private void Awake()
    {
        leapAttack = GetComponent<IAlienAttack>();

        if (leapAttack == null)
        {
            Debug.LogError("[LeapAttackTester] No IAlienAttack component found! Make sure LeapAttack is attached.");
            enabled = false;
            return;
        }

        // Create input action for T key
        leapAction = new InputAction(binding: "<Keyboard>/t");
        leapAction.performed += ctx => TriggerLeap();
    }

    private void OnEnable()
    {
        leapAction?.Enable();
    }

    private void OnDisable()
    {
        leapAction?.Disable();
    }

    private void TriggerLeap()
    {
        Vector3 target;

        // Determine target position
        if (targetPosition != null)
        {
            target = targetPosition.position;
        }
        else
        {
            // Leap forward from current position
            target = transform.position + transform.forward * defaultLeapDistance;
        }

        // Check if can attack
        if (leapAttack.CanAttack(target))
        {
            Debug.Log($"[LeapAttackTester] Triggering leap to {target}");
            leapAttack.ExecuteAttack(target);
        }
        else
        {
            Debug.LogWarning($"[LeapAttackTester] Cannot attack yet (cooldown or already leaping)");
        }
    }

    private void OnDrawGizmos()
    {
        // Show target position
        if (targetPosition != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(targetPosition.position, 0.5f);
            Gizmos.DrawLine(transform.position, targetPosition.position);
        }
        else if (Application.isPlaying)
        {
            // Show default leap direction
            Vector3 defaultTarget = transform.position + transform.forward * defaultLeapDistance;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(defaultTarget, 0.5f);
            Gizmos.DrawLine(transform.position, defaultTarget);
        }
    }
}
