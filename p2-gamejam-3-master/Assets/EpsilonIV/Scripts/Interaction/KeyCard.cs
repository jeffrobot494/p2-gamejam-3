using UnityEngine;

namespace EpsilonIV
{
    /// <summary>
    /// A throwable keycard that can unlock specific doors
    /// Must be held in hand when interacting with a KeyCardDoor
    /// </summary>
    [RequireComponent(typeof(ThrowableObject))]
    public class KeyCard : MonoBehaviour
    {
        [Header("Keycard Identity")]
        [Tooltip("Unique ID for this keycard (e.g., 'RedKeycard', 'SecurityPass', 'MaintenanceKey')")]
        public string KeycardID = "DefaultKeycard";

        [Tooltip("Display name shown to player")]
        public string KeycardName = "Keycard";

        [Header("Visual")]
        [Tooltip("Color tint for the keycard (optional)")]
        public Color KeycardColor = Color.white;

        /// <summary>
        /// Gets the unique ID of this keycard
        /// </summary>
        public string GetKeycardID()
        {
            return KeycardID;
        }
    }
}
