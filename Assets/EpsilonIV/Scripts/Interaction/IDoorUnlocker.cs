using UnityEngine;

namespace Unity.FPS.Gameplay
{
    /// <summary>
    /// Interface for components that can unlock doors
    /// Allows different unlock mechanisms (always open, keycard, pinpad, etc.)
    /// </summary>
    public interface IDoorUnlocker
    {
        /// <summary>
        /// Checks if the player can unlock this door
        /// </summary>
        /// <param name="player">The player GameObject attempting to unlock</param>
        /// <returns>True if player can unlock, false otherwise</returns>
        bool CanUnlock(GameObject player);

        /// <summary>
        /// Called when player attempts to unlock the door
        /// Can be used to show UI, play sounds, etc.
        /// </summary>
        /// <param name="player">The player GameObject attempting to unlock</param>
        /// <param name="door">The door being unlocked</param>
        void OnUnlockAttempt(GameObject player, Door door);
    }
}
