using UnityEngine;
using UnityEngine.AI;
using Unity.FPS.Game;
using UnityEngine.Animations;

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

    [Header("Animation")]
    [Tooltip("Animator on the child object that plays 'Idle' and 'Walking' states")]
    [SerializeField] private Animator childAnimator;

    [Tooltip("Animator state name for idle loop")]
    [SerializeField] private string idleStateName = "Idle";

    [Tooltip("Animator state name for walk loop")]
    [SerializeField] private string walkingStateName = "Walking";
    
    [Tooltip("Animator state name for galloping/running (Hunting)")]
    [SerializeField] private string gallopingStateName = "Galloping";

    [Tooltip("Animator state name for attacking animation")]
    [SerializeField] private string attackStateName = "Attack";

    [Tooltip("Crossfade duration (seconds) when switching states")]
    [SerializeField] private float animCrossfade = 0.1f;

    [Tooltip("Animator playback speed (1 = normal)")]
    [SerializeField] private float animPlaybackSpeed = 1f;

    private float prepareAttackTimer = 0f;

    [Header("Leap Overshoot Settings")]
    [Tooltip("How far past predicted target to aim when leaping")]
    [SerializeField] private float overshootDistance = 3f;

    [Tooltip("Layers considered as walls/obstacles for overshoot calculation")]
    [SerializeField] private LayerMask overshootWallMask = -1;

    [Tooltip("Layers considered as ground for overshoot calculation")]
    [SerializeField] private LayerMask overshootGroundLayer = -1;


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
            material = meshRenderer.material;

        if (patrolCenterObject != null)
            defaultPatrolCenter = patrolCenterObject.transform.position;

        currentPatrolCenter = defaultPatrolCenter;

        // Auto-find child Animator if not wired in Inspector
        if (childAnimator == null)
            childAnimator = GetComponentInChildren<Animator>(true);
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

                // Draw line to predicted position and leap target during PrepareAttack/Attacking
                if (currentState == EnemyState.PrepareAttack || currentState == EnemyState.Attacking)
                {
                    Vector3 predictedPos = CalculatePredictedPosition();
                    Vector3 leapTarget = CalculateLeapTarget(predictedPos);

                    // Cyan line to final leap target
                    Debug.DrawLine(transform.position, leapTarget, Color.cyan);

                    // White line showing predicted vs leap target offset (overshoot)
                    Debug.DrawLine(predictedPos, leapTarget, Color.white);
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
                // Check line of sight before preparing to leap
                if (HasLineOfSightToTarget(lastHeardSoundPosition))
                {
                    // Within attack range, ready, and clear line of sight - prepare to leap!
                    TransitionToState(EnemyState.PrepareAttack);
                    return;
                }
                // else: no line of sight, continue hunting to get closer/around obstacle
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

        // Attack complete, transition to Investigating
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
            // Investigation duration elapsed, return to Patrol
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
                PlayAnimSafe(idleStateName);
                break;

            case EnemyState.Patrol:
                agent.isStopped = false;
                agent.speed = patrolSpeed;
                SetRandomPatrolDestination();
                PlayAnimSafe(walkingStateName);
                break;

            case EnemyState.Hunting:
                agent.isStopped = false;
                agent.speed = huntingSpeed;
                agent.SetDestination(lastHeardSoundPosition);
                // Optional: choose which anim to play while hunting (walk/run). For now, reuse Walking:
                PlayAnimSafe(gallopingStateName);
                break;

            case EnemyState.PrepareAttack:
                prepareAttackTimer = 0f;
                agent.isStopped = true;
                // keep current animation or play a windup later
                FaceTarget(lastHeardSoundPosition);
                break;

            case EnemyState.Attacking:
                agent.isStopped = true;
                PlayAnimSafe(attackStateName);
                if (leapAttack != null)
                {
                    Vector3 predictedPosition = CalculatePredictedPosition();
                    Vector3 leapTarget = CalculateLeapTarget(predictedPosition);
                    leapAttack.ExecuteAttack(leapTarget);
                }
                break;

            case EnemyState.Investigating:
                if (stateBeforeIdle != EnemyState.Investigating)
                {
                    investigateTimer = 0f;
                    currentPatrolCenter = lastHeardSoundPosition;
                }
                agent.isStopped = false;
                agent.speed = patrolSpeed;
                SetRandomPatrolDestination();
                PlayAnimSafe(walkingStateName);
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

        // Robust helper for playing Animator states by name
    private void PlayAnimSafe(string stateName)
    {
        if (childAnimator == null || string.IsNullOrEmpty(stateName)) return;

        childAnimator.speed = animPlaybackSpeed;

        // Layer 0 assumed; adjust if you use layers
        int hash = Animator.StringToHash(stateName);
        if (!childAnimator.HasState(0, hash))
        {
            Debug.LogWarning($"[EnemyAI] Animator missing state '{stateName}' on layer 0 (object: {childAnimator.gameObject.name}).");
            return;
        }

        // Use CrossFadeInFixedTime for deterministic blending across frame rates
        childAnimator.CrossFadeInFixedTime(hash, animCrossfade);
    }

    private void FaceTarget(Vector3 target)
    {
        Vector3 dir = (target - transform.position).normalized;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dir);
    }

    /// <summary>
    /// Checks if there's a clear line of sight from the alien to the target position.
    /// Returns false if a wall/obstacle blocks the path.
    /// </summary>
    private bool HasLineOfSightToTarget(Vector3 targetPosition)
    {
        Vector3 startPos = transform.position;
        Vector3 direction = (targetPosition - startPos).normalized;
        float distance = Vector3.Distance(startPos, targetPosition);

        // Raycast from alien to target
        RaycastHit hit;
        if (Physics.Raycast(startPos, direction, out hit, distance, overshootWallMask))
        {
            // Something is blocking the path
            return false;
        }

        // Clear line of sight
        return true;
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
    /// Snaps result to nearest NavMesh point for valid landing.
    /// </summary>
    private Vector3 CalculatePredictedPosition()
    {
        Vector3 predictedPosition;

        // If no significant velocity, target is stationary
        if (lastHeardSoundVelocity.magnitude < 0.1f)
        {
            predictedPosition = lastHeardSoundPosition;
        }
        else
        {
            // Linear prediction: position + (velocity * time)
            Vector3 predictedOffset = lastHeardSoundVelocity * prepareAttackDuration;
            predictedPosition = lastHeardSoundPosition + predictedOffset;
        }

        // Snap to nearest NavMesh point to ensure valid landing position
        if (NavMesh.SamplePosition(predictedPosition, out NavMeshHit navHit, 5f, NavMesh.AllAreas))
        {
            return navHit.position;
        }

        // If no NavMesh found nearby, return original (might be off-mesh but better than nothing)
        return predictedPosition;
    }

    /// <summary>
    /// Calculates the final leap target with smart overshoot.
    /// Handles obstacles, terrain elevation, and pits/edges.
    /// Falls back to predicted position if overshoot is unsafe.
    /// NOTE: Line of sight is checked before this function is called in UpdateHunting()
    /// </summary>
    private Vector3 CalculateLeapTarget(Vector3 predictedPosition)
    {
        Vector3 startPos = transform.position;
        Vector3 direction = (predictedPosition - startPos).normalized;

        // Calculate ideal overshoot point beyond predicted position
        Vector3 idealOvershoot = predictedPosition + (direction * overshootDistance);
        Vector3 finalTarget = idealOvershoot;

        // Check for obstacles/walls between predicted position and overshoot
        RaycastHit obstacleHit;
        if (Physics.Raycast(predictedPosition, direction, out obstacleHit, overshootDistance, overshootWallMask))
        {
            // Wall detected - aim just before it
            idealOvershoot = obstacleHit.point - (direction * 0.5f);
        }

        // Find ground at overshoot point (handles elevation changes)
        // Use alien's Y position to ensure landing on same floor level
        RaycastHit groundHit;
        Vector3 rayStart = new Vector3(idealOvershoot.x, startPos.y + 2f, idealOvershoot.z);

        if (Physics.Raycast(rayStart, Vector3.down, out groundHit, 50f, overshootGroundLayer))
        {
            // Ground found at overshoot point
            finalTarget = groundHit.point;
        }
        else
        {
            // No ground found (pit/edge) - don't overshoot, use predicted position
            Debug.LogWarning($"[EnemyAI] No ground at overshoot point, falling back to predicted position");
            finalTarget = predictedPosition;
        }

        // Optional: Ensure target is on NavMesh (so alien can walk after landing)
        if (NavMesh.SamplePosition(finalTarget, out NavMeshHit navHit, 5f, NavMesh.AllAreas))
        {
            finalTarget = navHit.position;
        }
        else
        {
            // Off NavMesh - use predicted position instead (safer)
            Debug.LogWarning($"[EnemyAI] Overshoot point off NavMesh, using predicted position");
            finalTarget = predictedPosition;
        }

        return finalTarget;
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
