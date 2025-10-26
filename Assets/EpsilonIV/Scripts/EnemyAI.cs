using UnityEngine;
using UnityEngine.AI;
using Unity.FPS.Game;

public enum EnemyState
{
    Idle,
    Patrol,
    Hunting,
    PrepareAttack,
    Attacking,
    Investigating
}

/// <summary>
/// Enemy AI with state machine for Idle, Patrol, Hunting, Attacking, and Investigating behaviors.
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
    [Tooltip("Center point of default patrol area")]
    [SerializeField] private GameObject patrolCenterObject;
    private Vector3 defaultPatrolCenter;
    private Vector3 currentPatrolCenter;

    [Tooltip("Radius of default patrol area")]
    [SerializeField] private float defaultPatrolRadius = 10f;

    [Tooltip("Movement speed while patrolling")]
    [SerializeField] private float patrolSpeed = 2f;

    [Header("Investigating Settings")]
    [Tooltip("Radius of patrol area when investigating a sound")]
    [SerializeField] private float investigativePatrolRadius = 5f;

    [Tooltip("How long to investigate around last heard sound before returning to default patrol (in seconds)")]
    [SerializeField] private float investigateDuration = 30f;

    private float investigateTimer = 0f;

    [Header("Idle Settings")]
    [Tooltip("How long to idle before resuming patrol")]
    [SerializeField] private float idleDuration = 2f;

    [Header("Hunting Settings")]
    [Tooltip("How close to get to the heard sound location")]
    [SerializeField] private float huntingStoppingDistance = 1f;

    [Tooltip("Movement speed while hunting sounds")]
    [SerializeField] private float huntingSpeed = 5f;

    [Header("Prepare Attack Settings")]
    [Tooltip("Time spent preparing/winding up before leap")]
    [SerializeField] private float prepareAttackDuration = 0.5f;

    private float prepareAttackTimer = 0f;


    [Header("State Colors")]
    [SerializeField] private Color idleColor = Color.gray;
    [SerializeField] private Color patrolColor = Color.black;
    [SerializeField] private Color huntingColor = Color.yellow;
    [SerializeField] private Color prepareAttackColor = new Color(1f, 0.5f, 0f); // Orange
    [SerializeField] private Color attackingColor = Color.red;
    [SerializeField] private Color investigatingColor = Color.cyan;

    [Header("Debug")]
    [Tooltip("Show debug visualization of target position in Game view")]
    [SerializeField] private bool showDebugVisualization = true;

    private NavMeshAgent agent;
    private Listener listener;
    private Health health;
    private Material material;
    private IAlienAttack leapAttack;

    private EnemyState currentState = EnemyState.Patrol;
    private EnemyState stateBeforeIdle = EnemyState.Patrol; // Track what state we came from
    private float idleTimer = 0f;
    private Vector3 lastHeardSoundPosition;
    private Vector3 lastHeardSoundVelocity;
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
            defaultPatrolCenter = patrolCenterObject.transform.position;
        }

        currentPatrolCenter = defaultPatrolCenter;
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
        if (defaultPatrolCenter == Vector3.zero)
        {
            defaultPatrolCenter = transform.position;
            currentPatrolCenter = defaultPatrolCenter;
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
            if (currentState == EnemyState.Hunting || currentState == EnemyState.PrepareAttack || currentState == EnemyState.Attacking)
            {
                Color lineColor = currentState == EnemyState.PrepareAttack ? Color.magenta : Color.yellow;
                Debug.DrawLine(transform.position, lastHeardSoundPosition, lineColor);
                Debug.DrawRay(lastHeardSoundPosition, Vector3.up * 2f, lineColor); // Marker at target

                // Draw line to predicted position during PrepareAttack/Attacking
                if (currentState == EnemyState.PrepareAttack || currentState == EnemyState.Attacking)
                {
                    Vector3 predictedPos = CalculatePredictedPosition();
                    Debug.DrawLine(transform.position, predictedPos, Color.cyan);
                }
            }

            // Draw attack range indicator when hunting or preparing
            if ((currentState == EnemyState.Hunting || currentState == EnemyState.PrepareAttack) && leapAttack != null)
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

            case EnemyState.PrepareAttack:
                UpdatePrepareAttack();
                break;

            case EnemyState.Attacking:
                UpdateAttacking();
                break;

            case EnemyState.Investigating:
                UpdateInvestigating();
                break;
        }
    }

    private void UpdateIdle()
    {
        idleTimer += Time.deltaTime;
        if (idleTimer >= idleDuration)
        {
            // Return to the state we were in before idling
            TransitionToState(stateBeforeIdle);
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
                // Within attack range and ready - prepare to leap!
                TransitionToState(EnemyState.PrepareAttack);
                return;
            }
        }

        // If reached the sound location
        if (!agent.pathPending && agent.remainingDistance <= huntingStoppingDistance)
        {
            // Reached sound location - investigate the area
            TransitionToState(EnemyState.Investigating);
        }
    }

    private void UpdatePrepareAttack()
    {
        // Increment timer
        prepareAttackTimer += Time.deltaTime;

        // Check if windup complete
        if (prepareAttackTimer >= prepareAttackDuration)
        {
            // Windup complete, execute attack
            TransitionToState(EnemyState.Attacking);
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

        Debug.Log($"[EnemyAI] Attack complete, transitioning to Investigating");
        // Leap complete, transition to investigating
        TransitionToState(EnemyState.Investigating);
    }

    private void UpdateInvestigating()
    {
        // Update timer
        investigateTimer += Time.deltaTime;

        // Time to return to default patrol?
        if (investigateTimer >= investigateDuration)
        {
            Debug.Log($"[EnemyAI] Investigation duration elapsed ({investigateTimer:F2}/{investigateDuration:F2}), returning to Patrol");
            // Investigation over, return to default patrol
            TransitionToState(EnemyState.Patrol);
            return;
        }

        // If reached investigation destination, idle then pick new spot
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            TransitionToState(EnemyState.Idle);
        }
    }

    private void TransitionToState(EnemyState newState)
    {
        // Allow re-entering Hunting state (to update destination when new sound heard)
        // For other states, ignore if already in that state
        if (currentState == newState && newState != EnemyState.Hunting)
            return;

        bool isActualStateChange = (currentState != newState);

        // Track state before going to Idle (so we can return to it)
        if (isActualStateChange && newState == EnemyState.Idle)
        {
            stateBeforeIdle = currentState;
        }

        // Exit current state (only if actually changing states)
        if (isActualStateChange)
        {
            ExitState(currentState, newState);
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

            case EnemyState.PrepareAttack:
                prepareAttackTimer = 0f;
                agent.isStopped = true; // Stop moving during preparation
                // Face toward target
                Vector3 directionToTarget = (lastHeardSoundPosition - transform.position).normalized;
                if (directionToTarget != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(directionToTarget);
                }
                break;

            case EnemyState.Attacking:
                agent.isStopped = true; // Stop NavMesh movement during leap
                if (leapAttack != null)
                {
                    Vector3 predictedPosition = CalculatePredictedPosition();
                    leapAttack.ExecuteAttack(predictedPosition);
                }
                break;

            case EnemyState.Investigating:
                Debug.Log($"[EnemyAI] Entering Investigating state. stateBeforeIdle: {stateBeforeIdle}, investigateTimer: {investigateTimer}");
                // Only reset timer when first entering from Attacking, not when returning from Idle
                if (stateBeforeIdle != EnemyState.Investigating)
                {
                    investigateTimer = 0f; // Reset timer
                    currentPatrolCenter = lastHeardSoundPosition; // Set patrol center to last heard sound
                    Debug.Log($"[EnemyAI] Reset timer and patrol center to: {currentPatrolCenter}");
                }
                agent.isStopped = false;
                agent.speed = patrolSpeed; // Use patrol speed
                SetRandomPatrolDestination(); // Start patrolling around the sound location
                break;
        }
    }

    private void ExitState(EnemyState state, EnemyState nextState)
    {
        // Clean up state-specific data if needed
        if (state == EnemyState.Investigating)
        {
            // Only clear patrol center and stateBeforeIdle when investigation is COMPLETE (going to Patrol)
            // Don't clear when temporarily going to Idle during investigation
            if (nextState == EnemyState.Patrol)
            {
                // Reset to default patrol center
                currentPatrolCenter = defaultPatrolCenter;

                // Clear stateBeforeIdle so next investigation starts fresh
                if (stateBeforeIdle == EnemyState.Investigating)
                {
                    stateBeforeIdle = EnemyState.Patrol;
                }
            }
        }
    }

    private void SetRandomPatrolDestination()
    {
        // Use investigative radius when investigating, otherwise use default
        float radius = (currentState == EnemyState.Investigating) ? investigativePatrolRadius : defaultPatrolRadius;

        Vector3 randomPoint = currentPatrolCenter + Random.insideUnitSphere * radius;
        randomPoint.y = currentPatrolCenter.y; // Keep on same Y level

        if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, radius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            // If no valid position found, just go to patrol center
            agent.SetDestination(currentPatrolCenter);
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
            EnemyState.PrepareAttack => prepareAttackColor,
            EnemyState.Attacking => attackingColor,
            EnemyState.Investigating => investigatingColor,
            _ => Color.white
        };

        material.color = targetColor;
    }

    private void OnSoundHeard(float loudness, Vector3 soundPosition, float quality, Vector3 soundVelocity)
    {
        // Don't interrupt preparing to attack or attacking
        if (currentState != EnemyState.PrepareAttack && currentState != EnemyState.Attacking)
        {
            lastHeardSoundPosition = soundPosition;
            lastHeardSoundVelocity = soundVelocity;
            lastHeardSoundLoudness = loudness;
            lastHeardSoundQuality = quality;

            TransitionToState(EnemyState.Hunting);
        }
    }

    /// <summary>
    /// Calculates predicted target position based on velocity and preparation time.
    /// If target has low/zero velocity, returns original position (stationary target).
    /// </summary>
    private Vector3 CalculatePredictedPosition()
    {
        // If no significant velocity, target is stationary
        if (lastHeardSoundVelocity.magnitude < 0.1f)
        {
            return lastHeardSoundPosition;
        }

        // Linear prediction: position + (velocity * time)
        Vector3 predictedOffset = lastHeardSoundVelocity * prepareAttackDuration;
        Vector3 predictedPosition = lastHeardSoundPosition + predictedOffset;

        // Try to keep prediction on NavMesh
        if (NavMesh.SamplePosition(predictedPosition, out NavMeshHit hit, 5f, NavMesh.AllAreas))
        {
            predictedPosition = hit.position;
        }

        return predictedPosition;
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
        // Visualize default patrol area
        Gizmos.color = Color.blue;
        Vector3 center = defaultPatrolCenter == Vector3.zero ? transform.position : defaultPatrolCenter;
        Gizmos.DrawWireSphere(center, defaultPatrolRadius);

        // Visualize investigative patrol area when investigating
        if (Application.isPlaying && currentState == EnemyState.Investigating)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(currentPatrolCenter, investigativePatrolRadius);
        }

        // Visualize attack range when preparing or attacking
        if (Application.isPlaying && (currentState == EnemyState.PrepareAttack || currentState == EnemyState.Attacking) && leapAttack != null)
        {
            Gizmos.color = currentState == EnemyState.PrepareAttack ? Color.yellow : Color.red;
            Gizmos.DrawWireSphere(transform.position, leapAttack.GetAttackRange());
        }
    }
}
