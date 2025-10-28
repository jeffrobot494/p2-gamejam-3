using UnityEngine;

namespace Unity.FPS.Gameplay
{
    /// <summary>
    /// Interface for objects that the player can interact with
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// Called when the player interacts with this object
        /// </summary>
        void Interact();

        /// <summary>
        /// Gets the transform of the interactable object (for positioning UI)
        /// </summary>
        Transform GetTransform();

        /// <summary>
        /// Optional: Get custom interaction text (e.g., "Open Door", "Activate Panel")
        /// Returns null to use default "[E] INTERACT"
        /// </summary>
        string GetInteractionPrompt();
    }
}
