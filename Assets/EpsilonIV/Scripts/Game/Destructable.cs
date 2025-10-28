using UnityEngine;

namespace Unity.FPS.Game
{
    public class Destructable : MonoBehaviour
    {
        Health m_Health;

        void Start()
        {
            m_Health = GetComponent<Health>();
            if (m_Health == null)
                Debug.LogError($"Missing Health component on {gameObject.name}");

            // Subscribe to damage & death actions
            if (m_Health != null)
            {
                m_Health.OnDie += OnDie;
                m_Health.OnDamaged += OnDamaged;
            }
        }

        void OnDamaged(float damage, GameObject damageSource)
        {
            // TODO: damage reaction
        }

        void OnDie()
        {
            // this will call the OnDestroy function
            Destroy(gameObject);
        }
    }
}