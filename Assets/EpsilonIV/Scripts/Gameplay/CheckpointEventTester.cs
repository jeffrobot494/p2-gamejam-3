using UnityEngine;
using Unity.FPS.Gameplay;

/// <summary>
/// A simple test script to verify that the CheckpointManager.OnCheckpointChanged event is firing correctly.
/// </summary>
public class CheckpointEventTester : MonoBehaviour
{
    /// <summary>
    /// This public method should be subscribed to the CheckpointManager.OnCheckpointChanged event.
    /// </summary>
    public void OnCheckpointFired(RoomCheckpoint checkpoint)
    {
        if (checkpoint != null)
        {
            Debug.Log($"[CheckpointEventTester] >>> Event received successfully! Checkpoint ID: {checkpoint.checkpointID}, Name: {checkpoint.gameObject.name} <<<");
        }
        else
        {
            Debug.LogWarning("[CheckpointEventTester] >>> Event received successfully, but the checkpoint object was null. <<<");
        }
    }
}
