using UnityEngine;

namespace EpsilonIV
{
    /// <summary>
    /// Ladder system using a single BoxCollider as a trigger.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class Ladder : MonoBehaviour
    {
        [Header("Detection")]
        public LayerMask PlayerLayer = -1;
        [Range(0f, 1f)] public float DotThresholdToEnter = 0.7f;

        [Header("Bounds")]
        [Tooltip("Offset to define ladder top & bottom for exit checks")]
        public float LadderHeight = 3f;

        [Header("Debug")]
        public bool DebugMode = false;
        public bool ShowGizmos = true;

        public float TopY => transform.position.y + LadderHeight * 0.5f;
        public float BottomY => transform.position.y - LadderHeight * 0.5f;

        private void OnTriggerStay(Collider other)
        {
            if (!IsPlayerLayer(other.gameObject.layer))
                return;

            PlayerLadderController player = other.GetComponent<PlayerLadderController>();
            if (player == null || player.IsOnLadder)
                return;

            PlayerInputHandler input = other.GetComponent<PlayerInputHandler>();
            if (input == null) return;

            Vector3 moveInput = input.GetMoveInput();
            bool pressingForward = moveInput.z > 0.1f;
            bool pressingBackward = moveInput.z < -0.1f;

            if (!pressingForward && !pressingBackward)
                return;

            // Check facing direction
            float dot = Vector3.Dot(other.transform.forward, transform.forward);
            bool facingLadder = Mathf.Abs(dot) > DotThresholdToEnter;

            if (!facingLadder)
                return;

            if (DebugMode)
                Debug.Log($"[Ladder] Player entered ladder trigger and is facing correctly (dot={dot})");

            player.EnterLadder(this);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsPlayerLayer(other.gameObject.layer))
                return;

            PlayerLadderController player = other.GetComponent<PlayerLadderController>();
            if (player != null && player.CurrentLadder == this)
            {
                player.ExitLadder();
            }
        }

        private bool IsPlayerLayer(int layer)
        {
            return ((1 << layer) & PlayerLayer) != 0;
        }

        private void OnDrawGizmos()
        {
            if (!ShowGizmos) return;

            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, transform.forward * 1f);
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, new Vector3(1f, LadderHeight, 0.2f));

            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * (LadderHeight * 0.5f + 0.5f), "LADDER");
            #endif
        }
    }
}
