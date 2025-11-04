using UnityEngine;
using System.Collections;

public class ElevatorController : MonoBehaviour
{
    [Header("Elevator Positions")]
    [SerializeField] private Transform topPosition;
    [SerializeField] private Transform bottomPosition;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float startDelay = 3f;

    private bool isAtTop = false;   // start at bottom
    private bool isMoving = false;

    private void Awake()
    {
        // Set starting position to bottom
        if (bottomPosition != null)
            transform.position = bottomPosition.position;
        else
            Debug.LogWarning("[ElevatorController] Bottom position not assigned!");
    }

    /// <summary>
    /// Public method to trigger elevator movement to the opposite position.
    /// Can be called from a button script or interaction event.
    /// </summary>
    public void TriggerElevator()
    {
        if (!isMoving)
            StartCoroutine(MoveElevatorRoutine());
    }

    private IEnumerator MoveElevatorRoutine()
    {
        isMoving = true;

        // Delay before starting movement
        yield return new WaitForSeconds(startDelay);

        // Determine start and end points
        Vector3 startPos = transform.position;
        Vector3 targetPos = isAtTop ? bottomPosition.position : topPosition.position;

        float distance = Vector3.Distance(startPos, targetPos);
        float duration = distance / moveSpeed;
        float elapsed = 0f;

        // Smooth movement
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        transform.position = targetPos;
        isAtTop = !isAtTop;
        isMoving = false;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (topPosition != null && bottomPosition != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(topPosition.position, bottomPosition.position);
            Gizmos.DrawSphere(topPosition.position, 0.2f);
            Gizmos.DrawSphere(bottomPosition.position, 0.2f);
        }
    }
#endif
}
