using UnityEngine;
using UnityEngine.AI;
using Unity.FPS.Game;

public enum EnemyState
{
    Idle,
    Patrol,
    Hunting,
    Attacking
}

/// <summary>
/// Enemy AI with state machine for Idle, Patrol, Hunting, and Attacking behaviors.
/// Uses NavMesh for navigation and Listener component for hearing sounds.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Listener))]
[RequireComponent(typeof(Health))]
public class EnemyAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MeshRenderer meshRenderer;

    [Header("Patrol Settings")]
    [Tooltip("Center point of patrol area")]
    [SerializeField] private GameObject patrolCenterObject;
    private Vector3 patrolCenter;

    [Tooltip("Radius of patrol area")]
    [SerializeField] private float patrolRadius = 10f;

    [Tooltip("Movement speed while patrolling")]
    [SerializeField] private float patrolSpeed = 2f;

    [Header("Idle Settings")]
    [Tooltip("How long to idle before resuming patrol")]
    [SerializeField] private float idleDuration = 2f;

    [Header("Hunting Settings")]
    [Tooltip("How close to get to the heard sound location")]
    [SerializeField] private float huntingStoppingDistance = 1f;

    [Tooltip("Movement speed while hunting sounds")]
    [SerializeField] private float huntingSpeed = 5f;


    [Header("State Colors")]
    [SerializeField] private Color idleColor = Color.gray;
    [SerializeField] private Color patrolColor = Color.black;
    [SerializeField] private Color huntingColor = Color.yellow;
    [SerializeField] private Color attackingColor = Color.red;

    [Header("Debug")]
    [Tooltip("Show debug visualization of target position in Game view")]
    [SerializeField] private bool showDebugVisualization = true;

    private NavMeshAgent agent;
    private Listener listener;
    private Health health;
    private Material material;
    private IAlienAttack leapAttack;

    private EnemyState currentState = EnemyState.Patrol;
    private float idleTimer = 0f;
    private Vector3 lastHeardSoundPosition;
    private float lastHeardSoundLoudness;
    private float lastHeardSoundQuality;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        listener = GetComponent<Listener>();
        health = GetComponent<Health>();
        leapAttack = GetComponent<IAlienAttack>();

        if (meshRenderer != null)
        {
            material = meshRenderer.material;
        }

        if (patrolCenterObject != null)
        {
            patrolCenter = patrolCenterObject.transform.position;
        }
    }

    private void Start()
    {
        // Snap to NavMesh if close enough
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            transform.position = hit.position;
        }
        else
        {
            Debug.LogWarning($"EnemyAI on {gameObject.name} could not find nearby NavMesh. Make sure NavMesh is baked and enemy is near walkable surface.");
        }

        // Subscribe to events
        if (listener != null)
        {
            listener.OnSoundHeard += OnSoundHeard;
        }

        if (health != null)
        {
            health.OnDie += OnDie;
        }

        // Set patrol center to current position if not set
        if (patrolCenter == Vector3.zero)
        {
            patrolCenter = transform.position;
        }

        // Initialize state
        EnterState(currentState);
        UpdateStateColor();
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (listener != null)
        {
            listener.OnSoundHeard -= OnSoundHeard;
        }

        if (health != null)
        {
            health.OnDie -= OnDie;
        }
    }

    private void Update()
    {
        // Debug visualization
        if (showDebugVisualization)
        {
            // Draw line to current target sound position
            if (currentState == EnemyState.Hunting || currentState == EnemyState.Attacking)
            {
                Debug.DrawLine(transform.position, lastHeardSoundPosition, Color.yellow);
                Debug.DrawRay(lastHeardSoundPosition, Vector3.up * 2f, Color.yellow); // Marker at target
            }

            // Draw attack range indicator when hunting
            if (currentState == EnemyState.Hunting && leapAttack != null)
            {
                float attackRange = leapAttack.GetAttackRange();
                // Draw rays in a circle to show attack range
                int segments = 16;
                for (int i = 0; i < segments; i++)
                {
                    float angle1 = (i / (float)segments) * Mathf.PI * 2f;
                    float angle2 = ((i + 1) / (float)segments) * Mathf.PI * 2f;

                    Vector3 point1 = transform.position + new Vector3(Mathf.Cos(angle1), 0, Mathf.Sin(angle1)) * attackRange;
                    Vector3 point2 = transform.position + new Vector3(Mathf.Cos(angle2), 0, Mathf.Sin(angle2)) * attackRange;

                    Debug.DrawLine(point1, point2, Color.red);
                }
            }
        }

        // Execute behavior for current state
        switch (currentState)
        {
            case EnemyState.Idle:
                UpdateIdle();
                break;

            case EnemyState.Patrol:
                UpdatePatrol();
                break;

            case EnemyState.Hunting:
                UpdateHunting();
                break;

            case EnemyState.Attacking:
                UpdateAttacking();
                break;
        }
    }

    private void UpdateIdle()
    {
        idleTimer += Time.deltaTime;
        if (idleTimer >= idleDuration)
        {
            TransitionToState(EnemyState.Patrol);
        }
    }

    private void UpdatePatrol()
    {
        // If reached patrol destination
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            TransitionToState(EnemyState.Idle);
        }
    }

    private void UpdateHunting()
    {
        // Check if within attack range and can attack
        if (leapAttack != null)
        {
            float distanceToSound = Vector3.Distance(transform.position, lastHeardSoundPosition);

            if (distanceToSound <= leapAttack.GetAttackRange() && leapAttack.CanAttack(lastHeardSoundPosition))
            {
                // Within attack range and ready - leap!
                TransitionToState(EnemyState.Attacking);
                return;
            }
        }

        // If reached the sound location
        if (!agent.pathPending && agent.remainingDistance <= huntingStoppingDistance)
        {
            // Didn't find anything, go back to patrol
            TransitionToState(EnemyState.Patrol);
        }
    }

    private void UpdateAttacking()
    {
        // Wait for leap attack to complete
        if (leapAttack != null && !leapAttack.IsAttackComplete())
        {
            // Still leaping, wait...
            return;
        }

        // Leap complete, transition back to hunting at landing position
        TransitionToState(EnemyState.Hunting);
    }

    private void TransitionToState(EnemyState newState)
    {
        // Allow re-entering Hunting state (to update destination when new sound heard)
        // For other states, ignore if already in that state
        if (currentState == newState && newState != EnemyState.Hunting)
            return;

        bool isActualStateChange = (currentState != newState);

        // Exit current state (only if actually changing states)
        if (isActualStateChange)
        {
            ExitState(currentState);
        }

        // Update state
        currentState = newState;

        // Enter new state (or re-enter if Hunting)
        EnterState(newState);

        // Update visual (only if actually changing states)
        if (isActualStateChange)
        {
            UpdateStateColor();
        }
    }

    private void EnterState(EnemyState state)
    {
        switch (state)
        {
            case EnemyState.Idle:
                idleTimer = 0f;
                agent.isStopped = true;
                break;

            case EnemyState.Patrol:
                agent.isStopped = false;
                agent.speed = patrolSpeed;
                SetRandomPatrolDestination();
                break;

            case EnemyState.Hunting:
                agent.isStopped = false;
                agent.speed = huntingSpeed;
                agent.SetDestination(lastHeardSoundPosition);
                break;

            case EnemyState.Attacking:
                agent.isStopped = true; // Stop NavMesh movement during leap
                if (leapAttack != null)
                {
                    leapAttack.ExecuteAttack(lastHeardSoundPosition);
                }
                break;
        }
    }

    private void ExitState(EnemyState state)
    {
        // Clean up state-specific data if needed
    }

    private void SetRandomPatrolDestination()
    {
        Vector3 randomPoint = patrolCenter + Random.insideUnitSphere * patrolRadius;
        randomPoint.y = patrolCenter.y; // Keep on same Y level

        if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, patrolRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            // If no valid position found, just go to patrol center
            agent.SetDestination(patrolCenter);
        }
    }

    private void UpdateStateColor()
    {
        if (material == null) return;

        Color targetColor = currentState switch
        {
            EnemyState.Idle => idleColor,
            EnemyState.Patrol => patrolColor,
            EnemyState.Hunting => huntingColor,
            EnemyState.Attacking => attackingColor,
            _ => Color.white
        };

        material.color = targetColor;
    }

    private void OnSoundHeard(float loudness, Vector3 soundPosition, float quality)
    {
        // Don't interrupt attacking
        if (currentState != EnemyState.Attacking)
        {
            lastHeardSoundPosition = soundPosition;
            lastHeardSoundLoudness = loudness;
            lastHeardSoundQuality = quality;
            TransitionToState(EnemyState.Hunting);
        }
    }


    private void OnDie()
    {
        // Disable AI
        enabled = false;

        // Stop navigation
        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        // Change to death color (gray)
        if (material != null)
        {
            material.color = Color.gray;
        }

        // Could add death animation/effects here
        // For now, just destroy after a delay
        Destroy(gameObject, 2f);
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize patrol area
        Gizmos.color = Color.blue;
        Vector3 center = patrolCenter == Vector3.zero ? transform.position : patrolCenter;
        Gizmos.DrawWireSphere(center, patrolRadius);

        // Visualize attack range when attacking
        if (Application.isPlaying && currentState == EnemyState.Attacking && leapAttack != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, leapAttack.GetAttackRange());
        }
    }
}
