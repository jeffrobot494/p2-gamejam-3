using UnityEngine;
using UnityEngine.Events;
using System.Collections;

namespace EpsilonIV
{
    /// <summary>
    /// Survivor controller for dangling from building scenario.
    /// Extends the base Survivor class with special "let go" behavior.
    /// Player must convince the survivor via dialogue to let go and come down.
    /// </summary>
    public class DanglingSurvivor : Survivor
    {
        [Header("Dangling Specific")]
        [Tooltip("Is the survivor currently dangling from the building?")]
        [SerializeField] private bool isDangling = true;

        [Tooltip("Position where survivor lands after letting go")]
        [SerializeField] private Transform landingPosition;

        [Header("Physics & Animation")]
        [Tooltip("Animator for fall animations")]
        [SerializeField] private Animator animator;

        [Tooltip("Rigidbody for physics-based fall (optional)")]
        [SerializeField] private Rigidbody rb;

        [Tooltip("Enable physics-based fall when letting go")]
        [SerializeField] private bool usePhysicsFall = true;

        [Tooltip("Disable collision during fall to prevent issues")]
        [SerializeField] private bool disableCollisionDuringFall = true;

        [Header("Timing")]
        [Tooltip("Delay before executing let go (gives time for final dialogue)")]
        [SerializeField] private float letGoDelay = 2f;

        [Tooltip("Time it takes to fall/move to landing position (only used for lerp-based falls, not physics)")]
        [SerializeField] private float fallDuration = 1.5f;

        [Header("Events")]
        [Tooltip("Fired when survivor decides to let go (before fall starts)")]
        public UnityEvent OnLetGoDecision;

        [Tooltip("Fired when survivor starts falling")]
        public UnityEvent OnStartFalling;

        [Tooltip("Fired when survivor lands safely")]
        public UnityEvent OnLanded;

        [Tooltip("Fired with the reason survivor decided to let go")]
        public UnityEvent<string> OnLetGoWithReason;

        // State tracking
        private bool hasLetGo = false;
        private Collider[] cachedColliders;

        #region Properties

        /// <summary>
        /// Check if survivor is still dangling
        /// </summary>
        public bool IsDangling => isDangling;

        /// <summary>
        /// Check if survivor has already let go
        /// </summary>
        public bool HasLetGo => hasLetGo;

        #endregion

        #region Unity Lifecycle

        void OnEnable()
        {
            // Called when GameObject is activated
            if (debugMode)
                Debug.Log($"[DanglingSurvivor] OnEnable() called on {gameObject.name}");

            // Set IsDangling immediately when activated
            if (isDangling && animator != null)
            {
                animator.SetBool("IsDangling", true);
                if (debugMode)
                    Debug.Log($"[DanglingSurvivor] OnEnable: Set IsDangling = true");
            }
        }

        void Start()
        {

            base.Start();

            if (debugMode)
                Debug.Log($"[DanglingSurvivor] Start() called on {gameObject.name}");

            // Auto-find components if not assigned
            if (animator == null)
            {
                // Search in children as well since animator might be on child GameObject
                animator = GetComponentInChildren<Animator>();
                if (debugMode)
                    Debug.Log($"[DanglingSurvivor] Auto-found animator: {(animator != null ? "SUCCESS on " + animator.gameObject.name : "FAILED")}");
            }

            if (rb == null)
                rb = GetComponent<Rigidbody>();

            // Cache colliders for disabling during fall
            if (disableCollisionDuringFall)
            {
                cachedColliders = GetComponentsInChildren<Collider>();
            }

            // Initial setup - make sure physics is disabled while dangling
            if (rb != null && isDangling)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            // Debug: Check animator state
            if (debugMode)
            {
                Debug.Log($"[DanglingSurvivor] isDangling={isDangling}, animator={animator}, animator!=null={animator != null}");
                if (animator != null)
                {
                    Debug.Log($"[DanglingSurvivor] Animator controller: {(animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "NULL")}");
                }
            }

            // Set dangling animation if applicable
            if (isDangling && animator != null)
            {
                animator.SetBool("IsDangling", true);
                if (debugMode)
                {
                    Debug.Log($"[DanglingSurvivor] Set animator IsDangling = true");
                    // Verify it was set
                    bool currentValue = animator.GetBool("IsDangling");
                    Debug.Log($"[DanglingSurvivor] Verified IsDangling parameter value: {currentValue}");
                }
            }
            else if (debugMode)
            {
                if (!isDangling)
                    Debug.LogWarning($"[DanglingSurvivor] Not setting IsDangling because isDangling=false");
                if (animator == null)
                    Debug.LogError($"[DanglingSurvivor] Cannot set IsDangling - animator is NULL!");
            }

            if (debugMode)
            {
                Debug.Log($"[DanglingSurvivor] {gameObject.name} initialized. Dangling: {isDangling}");
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Survivor decides to let go and come down.
        /// Called by the function handler when LLM calls the come_down function.
        /// </summary>
        /// <param name="reason">Optional reason why survivor decided to let go</param>
        public void LetGo(string reason = "I trust you")
        {
            if (hasLetGo)
            {
                if (debugMode)
                    Debug.LogWarning($"[DanglingSurvivor] {gameObject.name} already let go. Ignoring duplicate call.");
                return;
            }

            if (!isDangling)
            {
                if (debugMode)
                    Debug.LogWarning($"[DanglingSurvivor] {gameObject.name} is not dangling. Cannot let go.");
                return;
            }

            hasLetGo = true;

            if (debugMode)
                Debug.Log($"[DanglingSurvivor] {gameObject.name} decided to let go! Reason: {reason}");

            // Fire events
            OnLetGoDecision?.Invoke();
            OnLetGoWithReason?.Invoke(reason);

            // Start the let go sequence
            StartCoroutine(LetGoSequence());
        }

        /// <summary>
        /// Override Rescue to handle unique rescue flow
        /// </summary>
        public new void Rescue()
        {
            // If survivor is still dangling, they can't be rescued directly
            // They need to let go first
            if (isDangling)
            {
                if (debugMode)
                    Debug.LogWarning($"[DanglingSurvivor] {gameObject.name} is still dangling. Must let go before being rescued.");
                return;
            }

            // Call base rescue method
            base.Rescue();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Coroutine that handles the entire let go sequence
        /// </summary>
        private IEnumerator LetGoSequence()
        {
            // Phase 1: Wait for dialogue to finish
            if (debugMode)
                Debug.Log($"[DanglingSurvivor] Waiting {letGoDelay}s before letting go...");

            yield return new WaitForSeconds(letGoDelay);

            // Phase 2: Start the fall
            if (debugMode)
                Debug.Log($"[DanglingSurvivor] Starting fall sequence!");

            // Note: Keep isDangling true and IsDangling animation playing during fall
            // We only set it to false when they actually land
            OnStartFalling?.Invoke();

            // Trigger fall if you have a separate fall animation
            // (But keep IsDangling true so the dangling animation keeps playing)
            if (animator != null && HasAnimatorParameter("Fall"))
            {
                animator.SetTrigger("Fall");
                if (debugMode)
                    Debug.Log($"[DanglingSurvivor] Triggered 'Fall' animation (keeping IsDangling=true)");
            }

            // Disable colliders during fall if configured
            if (disableCollisionDuringFall && cachedColliders != null)
            {
                foreach (var col in cachedColliders)
                {
                    if (col != null)
                        col.enabled = false;
                }
            }

            // Choose fall method: physics or lerp
            if (usePhysicsFall && rb != null)
            {
                yield return StartCoroutine(PhysicsBasedFall());
            }
            else if (landingPosition != null)
            {
                yield return StartCoroutine(LerpToLanding());
            }
            else
            {
                // Fallback: just wait
                yield return new WaitForSeconds(fallDuration);
            }

            // Phase 3: Landing
            HandleLanding();
        }

        /// <summary>
        /// Physics-based fall using Rigidbody
        /// Waits for OnCollisionEnter to detect landing
        /// </summary>
        private IEnumerator PhysicsBasedFall()
        {
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;

                if (debugMode)
                    Debug.Log($"[DanglingSurvivor] Enabled physics for fall - waiting for ground collision");
            }

            // Just wait indefinitely - OnCollisionEnter will handle landing
            // This coroutine will be stopped by StopAllCoroutines() when we land
            while (true)
            {
                yield return null;
            }
        }

        /// <summary>
        /// Called by Unity when this collider/rigidbody touches another collider
        /// </summary>
        void OnCollisionEnter(Collision collision)
        {
            if (debugMode)
                Debug.Log($"[DanglingSurvivor] OnCollisionEnter CALLED! Hit: {collision.gameObject.name}, isKinematic={rb?.isKinematic}, isDangling={isDangling}");

            // If we're falling (not kinematic) and hit something, we've landed
            if (rb != null && !rb.isKinematic)
            {
                if (debugMode)
                    Debug.Log($"[DanglingSurvivor] OnCollisionEnter with {collision.gameObject.name} - triggering landing");

                // Stop the fall coroutine and immediately land
                StopAllCoroutines();
                HandleLanding();
            }
        }

        /// <summary>
        /// Smooth lerp to landing position (non-physics)
        /// </summary>
        private IEnumerator LerpToLanding()
        {
            Vector3 startPos = transform.position;
            Vector3 targetPos = landingPosition.position;
            float elapsed = 0f;

            while (elapsed < fallDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fallDuration;

                // Use ease-in curve for realistic fall
                float easedT = 1f - Mathf.Pow(1f - t, 3f);

                transform.position = Vector3.Lerp(startPos, targetPos, easedT);
                yield return null;
            }

            transform.position = targetPos;
        }

        /// <summary>
        /// Handles survivor landing on the ground
        /// </summary>
        private void HandleLanding()
        {
            if (debugMode)
                Debug.Log($"[DanglingSurvivor] {gameObject.name} landed!");

            // Set dangling to false NOW that they've landed
            isDangling = false;

            // Re-enable colliders
            if (disableCollisionDuringFall && cachedColliders != null)
            {
                foreach (var col in cachedColliders)
                {
                    if (col != null)
                        col.enabled = true;
                }
            }

            // Disable physics if it was enabled
            if (rb != null)
            {
                rb.velocity = Vector3.zero; // Set velocity BEFORE making kinematic
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            // Update animator - stop dangling animation, return to idle
            if (animator != null)
            {
                animator.SetBool("IsDangling", false);
                if (debugMode)
                    Debug.Log($"[DanglingSurvivor] Set IsDangling=false, should transition to Idle");
            }

            // Fire landing event
            OnLanded?.Invoke();

            // Now survivor can be rescued normally
            if (debugMode)
                Debug.Log($"[DanglingSurvivor] {gameObject.name} can now be rescued!");
        }

        /// <summary>
        /// Helper to check if animator has a specific parameter
        /// </summary>
        private bool HasAnimatorParameter(string paramName)
        {
            if (animator == null) return false;

            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.name == paramName)
                    return true;
            }
            return false;
        }

        #endregion

        #region Testing / Debug

        [ContextMenu("Test: Let Go")]
        private void TestLetGo()
        {
            LetGo("Testing from context menu");
        }

        [ContextMenu("Test: Reset Dangling State")]
        private void TestReset()
        {
            hasLetGo = false;
            isDangling = true;
            Debug.Log($"[DanglingSurvivor] Reset to dangling state");
        }

        [ContextMenu("Log State")]
        private void LogState()
        {
            Debug.Log($"[DanglingSurvivor] {gameObject.name} - Dangling: {isDangling}, HasLetGo: {hasLetGo}, State: {CurrentState}");
        }

        #endregion
    }
}
