using Unity.FPS.Game;
using UnityEngine;

/// <summary>
/// Deals damage to any Damageable object that touches this GameObject.
/// Useful for testing damage systems, hazards, traps, etc.
/// </summary>
public class DamageOnTouch : MonoBehaviour
{
    [Header("Damage Settings")]
    [Tooltip("Amount of damage to deal on contact")]
    [SerializeField] private float damageAmount = 10f;

    [Tooltip("Time in seconds before this object can damage the same target again")]
    [SerializeField] private float damageCooldown = 1f;

    [Tooltip("If true, damage is dealt continuously while touching. If false, damage is dealt once per contact.")]
    [SerializeField] private bool continuousDamage = false;

    [Header("Optional")]
    [Tooltip("If set, only objects with this tag will take damage")]
    [SerializeField] private string targetTag = "";

    private float lastDamageTime = -999f;

    private void OnCollisionEnter(Collision collision)
    {
        TryDealDamage(collision.gameObject);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (continuousDamage)
        {
            TryDealDamage(collision.gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryDealDamage(other.gameObject);
    }

    private void OnTriggerStay(Collider other)
    {
        if (continuousDamage)
        {
            TryDealDamage(other.gameObject);
        }
    }

    private void TryDealDamage(GameObject target)
    {
        Debug.Log($"[DamageOnTouch] TryDealDamage called for: {target.name}");

        // Check cooldown
        if (Time.time < lastDamageTime + damageCooldown)
        {
            Debug.Log($"[DamageOnTouch] On cooldown. Last damage: {lastDamageTime}, Current: {Time.time}");
            return;
        }

        // Check tag filter if specified
        if (!string.IsNullOrEmpty(targetTag) && !target.CompareTag(targetTag))
        {
            Debug.Log($"[DamageOnTouch] Target tag '{target.tag}' doesn't match required tag '{targetTag}'");
            return;
        }

        // Try to get Damageable component
        Damageable damageable = target.GetComponent<Damageable>();
        if (damageable == null)
        {
            Debug.Log($"[DamageOnTouch] No Damageable on {target.name}, checking parent...");
            // Try to find it in parent (in case we hit a child collider)
            damageable = target.GetComponentInParent<Damageable>();
        }

        // Deal damage if we found a Damageable
        if (damageable != null)
        {
            Debug.Log($"[DamageOnTouch] Found Damageable! Dealing {damageAmount} damage to {target.name}");
            damageable.InflictDamage(damageAmount, false, gameObject);
            lastDamageTime = Time.time;
        }
        else
        {
            Debug.LogWarning($"[DamageOnTouch] No Damageable component found on {target.name} or its parents!");
        }
    }
}
