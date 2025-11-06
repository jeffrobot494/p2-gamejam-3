using UnityEngine;

namespace EpsilonIV
{
    /// <summary>
    /// Simple unlocker that always allows the door to be opened
    /// Use this for doors that don't require any special unlock mechanism
    /// </summary>
    public class AlwaysUnlockedDoor : MonoBehaviour, IDoorUnlocker
    {
        public bool CanUnlock(GameObject player)
        {
            // Always return true - no restrictions
            return true;
        }

        public void OnUnlockAttempt(GameObject player, Door door)
        {
            // Nothing special needs to happen
            // Door will just open
        }
    }
}
