using Unity.FPS.Game;
using Unity.FPS.Gameplay;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays the player's health as a fill bar in the UI
/// </summary>
public class PlayerHealthBar : MonoBehaviour
{
    [Tooltip("Image component displaying current health (must have Image Type set to 'Filled')")]
    public Image HealthFillImage;

    [Tooltip("Optional: Color gradient from full health to no health")]
    public bool useColorGradient = true;

    [Tooltip("Color when at full health")]
    public Color fullHealthColor = Color.green;

    [Tooltip("Color when at no health")]
    public Color noHealthColor = Color.red;

    private Health m_PlayerHealth;

    void Start()
    {
        // Find the player's PlayerCharacterController
        PlayerCharacterController playerCharacterController =
            FindFirstObjectByType<PlayerCharacterController>();

        if (playerCharacterController == null)
        {
            Debug.LogError($"PlayerHealthBar: Could not find PlayerCharacterController in scene!");
            enabled = false;
            return;
        }

        m_PlayerHealth = playerCharacterController.GetComponent<Health>();

        if (m_PlayerHealth == null)
        {
            Debug.LogError($"PlayerHealthBar: PlayerCharacterController does not have a Health component!");
            enabled = false;
            return;
        }

        if (HealthFillImage == null)
        {
            Debug.LogError($"PlayerHealthBar: HealthFillImage is not assigned!");
            enabled = false;
            return;
        }
    }

    void Update()
    {
        if (m_PlayerHealth == null) return;

        // Update health bar fill amount
        float healthRatio = m_PlayerHealth.GetRatio();
        HealthFillImage.fillAmount = healthRatio;

        // Update color gradient if enabled
        if (useColorGradient)
        {
            HealthFillImage.color = Color.Lerp(noHealthColor, fullHealthColor, healthRatio);
        }
    }
}
