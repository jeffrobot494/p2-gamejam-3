using UnityEngine;

namespace EpsilonIV
{
    /// <summary>
    /// Helper component for ladder entry/exit triggers
    /// Forwards trigger events to parent Ladder component
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class LadderTrigger : MonoBehaviour
    {
        [Tooltip("Reference to parent ladder")]
        public Ladder ParentLadder;

        [Tooltip("Is this the top trigger? (false = bottom trigger)")]
        public bool IsTopTrigger = false;

        private Collider m_Trigger;

        void Start()
        {
            m_Trigger = GetComponent<Collider>();

            // Validate setup
            if (m_Trigger == null)
            {
                Debug.LogError($"[LadderTrigger] {gameObject.name} needs a Collider component!");
                return;
            }

            if (!m_Trigger.isTrigger)
            {
                Debug.LogWarning($"[LadderTrigger] {gameObject.name} collider should be set as trigger! Auto-fixing.");
                m_Trigger.isTrigger = true;
            }

            if (ParentLadder == null)
            {
                Debug.LogError($"[LadderTrigger] {gameObject.name} needs a ParentLadder reference!");
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (ParentLadder != null)
            {
                ParentLadder.OnTriggerEntered(other, IsTopTrigger);
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (ParentLadder != null)
            {
                ParentLadder.OnTriggerExited(other, IsTopTrigger);
            }
        }

        void OnDrawGizmos()
        {
            // Draw trigger bounds
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                Gizmos.color = IsTopTrigger ? new Color(0f, 1f, 0f, 0.3f) : new Color(0f, 0f, 1f, 0.3f);
                Gizmos.matrix = transform.localToWorldMatrix;

                if (col is BoxCollider box)
                {
                    Gizmos.DrawCube(box.center, box.size);
                }
                else if (col is SphereCollider sphere)
                {
                    Gizmos.DrawSphere(sphere.center, sphere.radius);
                }
            }
        }
    }
}
