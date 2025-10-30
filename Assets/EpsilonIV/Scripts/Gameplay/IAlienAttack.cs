using UnityEngine;

/// <summary>
/// Interface for alien attack behaviors.
/// Allows different attack types to be swapped and combined modularly.
/// </summary>
public interface IAlienAttack
{
    /// <summary>
    /// Gets the current attack range for this attack.
    /// May be dynamic/randomized per attack.
    /// </summary>
    float GetAttackRange();

    /// <summary>
    /// Checks if this attack can currently be executed.
    /// Considers cooldown, state, and other conditions.
    /// </summary>
    /// <param name="targetPosition">Position to attack</param>
    /// <returns>True if attack is ready to use</returns>
    bool CanAttack(Vector3 targetPosition);

    /// <summary>
    /// Executes the attack toward the target position.
    /// Called by EnemyAI when transitioning to Attacking state.
    /// </summary>
    /// <param name="targetPosition">Position to attack</param>
    void ExecuteAttack(Vector3 targetPosition);

    /// <summary>
    /// Checks if the current attack has finished executing.
    /// EnemyAI polls this to know when to transition out of Attacking state.
    /// </summary>
    /// <returns>True if attack is complete</returns>
    bool IsAttackComplete();
}
