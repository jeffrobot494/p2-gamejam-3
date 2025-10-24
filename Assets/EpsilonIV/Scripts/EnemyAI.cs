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

    [Header("Idle Settings")]
    [Tooltip("How long to idle before resuming patrol")]
    [SerializeField] private float idleDuration = 2f;

    [Header("Hunting Settings")]
    [Tooltip("How close to get to the heard sound location")]
    [SerializeField] private float huntingStoppingDistance = 1f;

    [Header("Attacking Settings")]
    [Tooltip("How far player can be before enemy stops attacking")]
    [SerializeField] private float attackRange = 3f;

    [Header("State Colors")]
    [SerializeField] private Color idleColor = Color.gray;
    [SerializeField] private Color patrolColor = Color.black;
    [SerializeField] private Color huntingColor = Color.yellow;
    [SerializeField] private Color attackingColor = Color.red;

    private NavMeshAgent agent;
    private Listener listener;
    private Health health;
    private Material material;

    private EnemyState currentState = EnemyState.Patrol;
    private float idleTimer = 0f;
    private Vector3 lastHeardSoundPosition;
    private float lastHeardSoundLoudness;
    private float lastHeardSoundQuality;
    private GameObject playerTarget;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        listener = GetComponent<Listener>();
        health = GetComponent<Health>();

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
        // If reached the sound location
        if (!agent.pathPending && agent.remainingDistance <= huntingStoppingDistance)
        {
            // Didn't find anything, go back to patrol
            TransitionToState(EnemyState.Patrol);
        }
    }

    private void UpdateAttacking()
    {
        // If player escapes or dies
        if (playerTarget == null || Vector3.Distance(transform.position, playerTarget.transform.position) > attackRange)
        {
            playerTarget = null;
            TransitionToState(EnemyState.Patrol);
        }
        else
        {
            // Keep moving towards player
            agent.SetDestination(playerTarget.transform.position);
        }
    }

    private void TransitionToState(EnemyState newState)
    {
        if (currentState == newState) return;

        // Exit current state
        ExitState(currentState);

        // Update state
        currentState = newState;

        // Enter new state
        EnterState(newState);

        // Update visual
        UpdateStateColor();
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
                SetRandomPatrolDestination();
                break;

            case EnemyState.Hunting:
                agent.isStopped = false;
                agent.SetDestination(lastHeardSoundPosition);
                break;

            case EnemyState.Attacking:
                agent.isStopped = false;
                if (playerTarget != null)
                {
                    agent.SetDestination(playerTarget.transform.position);
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

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            playerTarget = collision.gameObject;
            TransitionToState(EnemyState.Attacking);
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
        if (Application.isPlaying && currentState == EnemyState.Attacking)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
