using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles player pickup and throwing of ThrowableObjects.
/// Uses charge-up mechanic where holding throw button increases throw force.
/// </summary>
public class PlayerThrowController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Camera transform used for raycasting and throw direction")]
    [SerializeField] private Transform playerCamera;

    [Header("Pickup Settings")]
    [Tooltip("Maximum distance to detect and pick up objects")]
    [SerializeField] private float pickupRange = 3f;

    [Tooltip("Layer mask for raycast detection of throwable objects")]
    [SerializeField] private LayerMask pickupLayerMask = ~0;

    [Header("Hold Position")]
    [Tooltip("Position of held object relative to camera (local space)")]
    [SerializeField] private Vector3 holdPosition = new Vector3(0.5f, -0.3f, 1f);

    [Tooltip("Position of object during windup (pulled back, local space)")]
    [SerializeField] private Vector3 windupPosition = new Vector3(0.5f, -0.2f, 0.5f);

    [Tooltip("Speed at which object moves between hold and windup positions")]
    [SerializeField] private float positionLerpSpeed = 10f;

    [Header("Throw Settings")]
    [Tooltip("Minimum throw force (instant release)")]
    [SerializeField] private float minThrowForce = 5f;

    [Tooltip("Maximum throw force (fully charged)")]
    [SerializeField] private float maxThrowForce = 15f;

    [Tooltip("Time to reach maximum charge (seconds)")]
    [SerializeField] private float maxChargeTime = 2f;

    [Header("Input")]
    [Tooltip("Input action for picking up objects")]
    [SerializeField] private InputAction pickupAction;

    [Tooltip("Input action for throwing objects (hold to charge)")]
    [SerializeField] private InputAction throwAction;

    private enum ThrowState
    {
        NoObject,
        Holding,
        WindingUp,
        Throwing
    }

    private ThrowState currentState = ThrowState.NoObject;
    private ThrowableObject heldObject = null;
    private float chargeTime = 0f;
    private Vector3 targetPosition;

    private void OnEnable()
    {
        pickupAction?.Enable();
        throwAction?.Enable();
    }

    private void OnDisable()
    {
        pickupAction?.Disable();
        throwAction?.Disable();
    }

    private void Start()
    {
        // Validate camera reference
        if (playerCamera == null)
        {
            playerCamera = Camera.main?.transform;
            if (playerCamera == null)
            {
                Debug.LogError("[PlayerThrowController] No camera assigned and no main camera found!");
            }
        }

        // Setup input callbacks
        if (pickupAction != null)
        {
            pickupAction.performed += _ => TryPickup();
        }

        if (throwAction != null)
        {
            throwAction.started += _ => StartWindup();
            throwAction.canceled += _ => ExecuteThrow();
        }

        targetPosition = holdPosition;
    }

    private void Update()
    {
        // Update state-specific behavior
        switch (currentState)
        {
            case ThrowState.Holding:
                UpdateHolding();
                break;

            case ThrowState.WindingUp:
                UpdateWindup();
                break;
        }

        // Update held object position
        if (heldObject != null && currentState != ThrowState.Throwing)
        {
            UpdateObjectPosition();
        }
    }

    private void UpdateHolding()
    {
        // Smoothly move to hold position
        targetPosition = Vector3.Lerp(targetPosition, holdPosition, Time.deltaTime * positionLerpSpeed);
    }

    private void UpdateWindup()
    {
        // Increase charge time
        chargeTime += Time.deltaTime;
        chargeTime = Mathf.Min(chargeTime, maxChargeTime);

        // Smoothly move to windup position
        targetPosition = Vector3.Lerp(targetPosition, windupPosition, Time.deltaTime * positionLerpSpeed);
    }

    private void UpdateObjectPosition()
    {
        if (heldObject == null || playerCamera == null)
            return;

        // Position object relative to camera
        Vector3 worldPosition = playerCamera.TransformPoint(targetPosition);
        heldObject.transform.position = worldPosition;

        // Make object face same direction as camera (optional)
        heldObject.transform.rotation = playerCamera.rotation;
    }

    private void TryPickup()
    {
        // Can only pickup when not holding anything
        if (currentState != ThrowState.NoObject)
            return;

        if (playerCamera == null)
            return;

        // Raycast from camera to find throwable object
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, pickupRange, pickupLayerMask))
        {
            ThrowableObject throwable = hit.collider.GetComponent<ThrowableObject>();
            if (throwable != null && !throwable.IsHeld)
            {
                PickupObject(throwable);
            }
        }
    }

    private void PickupObject(ThrowableObject throwable)
    {
        heldObject = throwable;
        heldObject.OnPickup();

        // Set initial position to hold position
        targetPosition = holdPosition;
        UpdateObjectPosition();

        currentState = ThrowState.Holding;
    }

    private void StartWindup()
    {
        // Can only windup when holding an object
        if (currentState != ThrowState.Holding)
            return;

        chargeTime = 0f;
        currentState = ThrowState.WindingUp;
    }

    private void ExecuteThrow()
    {
        // Can only throw when winding up
        if (currentState != ThrowState.WindingUp)
            return;

        if (heldObject == null || playerCamera == null)
            return;

        currentState = ThrowState.Throwing;

        // Calculate throw force based on charge time
        float chargePercent = chargeTime / maxChargeTime;
        float throwForce = Mathf.Lerp(minThrowForce, maxThrowForce, chargePercent);

        // Apply force in camera forward direction
        Vector3 throwDirection = playerCamera.forward;
        Vector3 forceVector = throwDirection * throwForce;

        // Throw the object
        heldObject.OnThrow(forceVector);
        heldObject = null;

        // Reset state
        chargeTime = 0f;
        currentState = ThrowState.NoObject;
    }

    /// <summary>
    /// Drops the currently held object without throwing it.
    /// Useful for canceling or if player takes damage.
    /// </summary>
    public void DropHeldObject()
    {
        if (heldObject != null)
        {
            heldObject.OnThrow(Vector3.zero);
            heldObject = null;
        }

        chargeTime = 0f;
        currentState = ThrowState.NoObject;
    }

    /// <summary>
    /// Returns whether the player is currently holding an object.
    /// </summary>
    public bool IsHoldingObject => heldObject != null;

    /// <summary>
    /// Returns the current charge percentage (0-1) when winding up.
    /// </summary>
    public float ChargePercent => currentState == ThrowState.WindingUp ? (chargeTime / maxChargeTime) : 0f;

    private void OnDrawGizmos()
    {
        // Visualize pickup range
        if (playerCamera != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(playerCamera.position, playerCamera.forward * pickupRange);
        }

        // Visualize hold position
        if (playerCamera != null && heldObject != null)
        {
            Gizmos.color = Color.yellow;
            Vector3 worldHoldPos = playerCamera.TransformPoint(holdPosition);
            Gizmos.DrawWireSphere(worldHoldPos, 0.1f);

            Gizmos.color = Color.red;
            Vector3 worldWindupPos = playerCamera.TransformPoint(windupPosition);
            Gizmos.DrawWireSphere(worldWindupPos, 0.1f);
        }
    }
}
