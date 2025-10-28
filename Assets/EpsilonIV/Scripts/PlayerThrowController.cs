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
    [Tooltip("Note: Pickup is now handled by PlayerInteraction + IInteractable system")]
    [SerializeField] private bool _pickupInfoOnly = false;

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
        throwAction?.Enable();
    }

    private void OnDisable()
    {
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

        // Update held object position (using local position since it's parented)
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
        if (heldObject == null)
            return;

        // Update local position (object is parented to camera)
        heldObject.transform.localPosition = targetPosition;
    }

    /// <summary>
    /// Picks up the specified throwable object. Called by ThrowableObject when interacted with.
    /// </summary>
    public void PickupObject(ThrowableObject throwable)
    {
        // Can only pickup when not holding anything
        if (currentState != ThrowState.NoObject)
            return;

        if (throwable == null || throwable.IsHeld)
            return;

        heldObject = throwable;
        heldObject.OnPickup();

        // Parent to camera
        heldObject.transform.SetParent(playerCamera);

        // Set initial position to hold position
        targetPosition = holdPosition;
        heldObject.transform.localPosition = targetPosition;
        heldObject.transform.localRotation = Quaternion.identity;

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

        // Unparent before throwing
        heldObject.transform.SetParent(null);

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
            // Unparent before dropping
            heldObject.transform.SetParent(null);
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
}
