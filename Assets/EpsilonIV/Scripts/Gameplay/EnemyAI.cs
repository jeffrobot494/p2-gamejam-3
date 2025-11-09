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
[RequireComponent(typeof(AudioSource))]
public class EnemyAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MeshRenderer meshRenderer;

    [Header("Patrol Settings")]
    [SerializeField] private GameObject patrolCenterObject;
    private Vector3 defaultPatrolCenter;
    private Vector3 currentPatrolCenter;
    [SerializeField] private float defaultPatrolRadius = 10f;
    [SerializeField] private float patrolSpeed = 2f;

    [Header("Investigating Settings")]
    [SerializeField] private float investigativePatrolRadius = 5f;
    [SerializeField] private float investigateDuration = 30f;
    private float investigateTimer = 0f;

    [Header("Idle Settings")]
    [SerializeField] private float idleDuration = 2f;

    [Header("Hunting Settings")]
    [SerializeField] private float huntingStoppingDistance = 1f;
    [SerializeField] private float huntingSpeed = 5f;

    [Header("Prepare Attack Settings")]
    [SerializeField] private float prepareAttackDuration = 0.5f;
    private float prepareAttackTimer = 0f;

    [Header("Animation")]
    [SerializeField] private Animator childAnimator;
    [SerializeField] private string idleStateName = "Idle";
    [SerializeField] private string walkingStateName = "Walking";
    [SerializeField] private string gallopingStateName = "Galloping";
    [SerializeField] private string attackStateName = "Attack";
    [SerializeField] private float animCrossfade = 0.1f;
    [SerializeField] private float animPlaybackSpeed = 1f;

    [Header("Leap Overshoot Settings")]
    [SerializeField] private float overshootDistance = 3f;
    [SerializeField] private LayerMask overshootWallMask = -1;
    [SerializeField] private LayerMask overshootGroundLayer = -1;

    [Header("State Colors")]
    [SerializeField] private Color idleColor = Color.gray;
    [SerializeField] private Color patrolColor = Color.black;
    [SerializeField] private Color huntingColor = Color.yellow;
    [SerializeField] private Color prepareAttackColor = new Color(1f, 0.5f, 0f);
    [SerializeField] private Color attackingColor = Color.red;
    [SerializeField] private Color investigatingColor = Color.cyan;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] walkClips;
    [SerializeField] private AudioClip[] gallopClips;
    [SerializeField] private AudioClip[] attackClips;
    [SerializeField] private AudioClip[] alertClips;
    [SerializeField] private AudioClip[] idleClips;
    [SerializeField] private float walkCooldown = 0.5f;
    [SerializeField] private float gallopCooldown = 0.3f;
    private float lastWalkTime = -1f;
    private float lastGallopTime = -1f;

    [Header("Debug")]
    [SerializeField] private bool showDebugVisualization = true;

    private NavMeshAgent agent;
    private Listener listener;
    private Health health;
    private Material material;
    private IAlienAttack leapAttack;

    private EnemyState currentState = EnemyState.Patrol;
    private EnemyState stateBeforeIdle = EnemyState.Patrol;
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
        audioSource = GetComponent<AudioSource>();

        if (meshRenderer != null)
            material = meshRenderer.material;

        if (patrolCenterObject != null)
            defaultPatrolCenter = patrolCenterObject.transform.position;

        currentPatrolCenter = defaultPatrolCenter;

        if (childAnimator == null)
            childAnimator = GetComponentInChildren<Animator>(true);
    }

    private void Start()
    {
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            transform.position = hit.position;
        else
            Debug.LogWarning($"EnemyAI on {gameObject.name} could not find nearby NavMesh.");

        if (listener != null)
            listener.OnSoundHeard += OnSoundHeard;

        if (health != null)
            health.OnDie += OnDie;

        if (defaultPatrolCenter == Vector3.zero)
        {
            defaultPatrolCenter = transform.position;
            currentPatrolCenter = defaultPatrolCenter;
        }

        EnterState(currentState);
        UpdateStateColor();
    }

    private void OnDestroy()
    {
        if (listener != null)
            listener.OnSoundHeard -= OnSoundHeard;
        if (health != null)
            health.OnDie -= OnDie;
    }

    private void Update()
    {
        if (showDebugVisualization)
            DrawDebug();

        switch (currentState)
        {
            case EnemyState.Idle: UpdateIdle(); break;
            case EnemyState.Patrol: UpdatePatrol(); break;
            case EnemyState.Hunting: UpdateHunting(); break;
            case EnemyState.PrepareAttack: UpdatePrepareAttack(); break;
            case EnemyState.Attacking: UpdateAttacking(); break;
            case EnemyState.Investigating: UpdateInvestigating(); break;
        }
    }

    private void UpdateIdle()
    {
        idleTimer += Time.deltaTime;
        if (idleTimer >= idleDuration)
            TransitionToState(stateBeforeIdle);
    }

    private void UpdatePatrol()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            TransitionToState(EnemyState.Idle);
            return;
        }

        if (agent.velocity.magnitude > 0.1f)
            PlayClip(walkClips, ref lastWalkTime, walkCooldown);
    }

    private void UpdateHunting()
    {
        if (agent.velocity.magnitude > 0.1f)
            PlayClip(gallopClips, ref lastGallopTime, gallopCooldown);

        if (leapAttack != null)
        {
            float distanceToSound = Vector3.Distance(transform.position, lastHeardSoundPosition);

            if (distanceToSound <= leapAttack.GetAttackRange() && leapAttack.CanAttack(lastHeardSoundPosition))
            {
                if (HasLineOfSightToTarget(lastHeardSoundPosition))
                {
                    TransitionToState(EnemyState.PrepareAttack);
                    return;
                }
            }
        }

        if (!agent.pathPending && agent.remainingDistance <= huntingStoppingDistance)
            TransitionToState(EnemyState.Investigating);
    }

    private void UpdatePrepareAttack()
    {
        prepareAttackTimer += Time.deltaTime;
        if (prepareAttackTimer >= prepareAttackDuration)
            TransitionToState(EnemyState.Attacking);
    }

    private void UpdateAttacking()
    {
        if (leapAttack != null && !leapAttack.IsAttackComplete())
            return;

        TransitionToState(EnemyState.Investigating);
    }

    private void UpdateInvestigating()
    {
        investigateTimer += Time.deltaTime;
        if (investigateTimer >= investigateDuration)
        {
            TransitionToState(EnemyState.Patrol);
            return;
        }

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            TransitionToState(EnemyState.Idle);
    }

    private void TransitionToState(EnemyState newState)
    {
        if (currentState == newState && newState != EnemyState.Hunting)
            return;

        bool isActualStateChange = (currentState != newState);

        if (isActualStateChange && newState == EnemyState.Idle)
            stateBeforeIdle = currentState;

        if (isActualStateChange)
            ExitState(currentState, newState);

        currentState = newState;
        EnterState(newState);

        if (isActualStateChange)
            UpdateStateColor();
    }

    private void EnterState(EnemyState state)
    {
        switch (state)
        {
            case EnemyState.Idle:
                idleTimer = 0f;
                agent.isStopped = true;
                PlayAnimSafe(idleStateName);
                PlayRandomClip(idleClips);
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
                PlayAnimSafe(gallopingStateName);
                break;

            case EnemyState.PrepareAttack:
                prepareAttackTimer = 0f;
                agent.isStopped = true;
                FaceTarget(lastHeardSoundPosition);
                PlayRandomClip(alertClips);
                break;

            case EnemyState.Attacking:
                agent.isStopped = true;
                PlayAnimSafe(attackStateName);
                PlayRandomClip(attackClips);
                if (leapAttack != null)
                {
                    Vector3 predictedPosition = CalculatePredictedPosition();
                    Vector3 leapTarget = CalculateLeapTarget(predictedPosition);
                    leapAttack.ExecuteAttack(leapTarget);
                }
                break;

            case EnemyState.Investigating:
                if (stateBeforeIdle != EnemyState.Investigating)
                    investigateTimer = 0f;
                currentPatrolCenter = lastHeardSoundPosition;
                agent.isStopped = false;
                agent.speed = patrolSpeed;
                SetRandomPatrolDestination();
                PlayAnimSafe(walkingStateName);
                break;
        }
    }

    private void ExitState(EnemyState state, EnemyState nextState)
    {
        if (state == EnemyState.Investigating && nextState == EnemyState.Patrol)
        {
            currentPatrolCenter = defaultPatrolCenter;
            if (stateBeforeIdle == EnemyState.Investigating)
                stateBeforeIdle = EnemyState.Patrol;
        }
    }

    private void SetRandomPatrolDestination()
    {
        float radius = (currentState == EnemyState.Investigating) ? investigativePatrolRadius : defaultPatrolRadius;
        Vector3 randomPoint = currentPatrolCenter + Random.insideUnitSphere * radius;
        randomPoint.y = currentPatrolCenter.y;

        if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, radius, NavMesh.AllAreas))
            agent.SetDestination(hit.position);
        else
            agent.SetDestination(currentPatrolCenter);
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

    private void PlayAnimSafe(string stateName)
    {
        if (childAnimator == null || string.IsNullOrEmpty(stateName)) return;

        childAnimator.speed = animPlaybackSpeed;
        int hash = Animator.StringToHash(stateName);
        if (!childAnimator.HasState(0, hash))
        {
            Debug.LogWarning($"Animator missing state '{stateName}' on {childAnimator.gameObject.name}");
            return;
        }
        childAnimator.CrossFadeInFixedTime(hash, animCrossfade);
    }

    private void FaceTarget(Vector3 target)
    {
        Vector3 dir = (target - transform.position).normalized;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dir);
    }

    private bool HasLineOfSightToTarget(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, targetPosition);
        return !Physics.Raycast(transform.position, direction, distance, overshootWallMask);
    }

    private void OnSoundHeard(float loudness, Vector3 soundPosition, float quality, Vector3 soundVelocity)
    {
        if (currentState != EnemyState.PrepareAttack && currentState != EnemyState.Attacking)
        {
            lastHeardSoundPosition = soundPosition;
            lastHeardSoundVelocity = soundVelocity;
            lastHeardSoundLoudness = loudness;
            lastHeardSoundQuality = quality;
            TransitionToState(EnemyState.Hunting);
        }
    }

    private Vector3 CalculatePredictedPosition()
    {
        Vector3 predicted = (lastHeardSoundVelocity.magnitude < 0.1f) ? lastHeardSoundPosition : lastHeardSoundPosition + lastHeardSoundVelocity * prepareAttackDuration;

        if (NavMesh.SamplePosition(predicted, out NavMeshHit navHit, 5f, NavMesh.AllAreas))
            return navHit.position;

        return predicted;
    }

    private Vector3 CalculateLeapTarget(Vector3 predictedPosition)
    {
        Vector3 dir = (predictedPosition - transform.position).normalized;
        Vector3 idealOvershoot = predictedPosition + dir * overshootDistance;
        Vector3 finalTarget = idealOvershoot;

        if (Physics.Raycast(predictedPosition, dir, out RaycastHit obstacleHit, overshootDistance, overshootWallMask))
            idealOvershoot = obstacleHit.point - dir * 0.5f;

        if (Physics.Raycast(new Vector3(idealOvershoot.x, transform.position.y + 2f, idealOvershoot.z), Vector3.down, out RaycastHit groundHit, 50f, overshootGroundLayer))
            finalTarget = groundHit.point;
        else
            finalTarget = predictedPosition;

        if (NavMesh.SamplePosition(finalTarget, out NavMeshHit navHit2, 5f, NavMesh.AllAreas))
            finalTarget = navHit2.position;
        else
            finalTarget = predictedPosition;

        return finalTarget;
    }

    private void OnDie()
    {
        enabled = false;
        if (agent != null) { agent.isStopped = true; agent.enabled = false; }
        if (material != null) material.color = Color.gray;
        Destroy(gameObject, 2f);
    }

    private void DrawDebug()
    {
        if ((currentState == EnemyState.Hunting || currentState == EnemyState.PrepareAttack || currentState == EnemyState.Attacking) && leapAttack != null)
        {
            Debug.DrawLine(transform.position, lastHeardSoundPosition, Color.yellow);
            Debug.DrawRay(lastHeardSoundPosition, Vector3.up * 2f, Color.yellow);
        }
    }

    // ---------------- AUDIO HELPERS ----------------

    private void PlayClip(AudioClip[] clips, ref float lastTime, float cooldown)
    {
        if (clips == null || clips.Length == 0 || audioSource == null) return;

        float currentTime = Time.time;
        if (currentTime - lastTime < cooldown) return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        audioSource.PlayOneShot(clip, 2f);
        lastTime = currentTime;
    }

    private void PlayRandomClip(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0 || audioSource == null) return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        audioSource.PlayOneShot(clip);
    }
}
