using Unity.FPS.Game;
using Unity.FPS.Gameplay;
using UnityEngine;
using UnityEngine.UI;
using EpsilonIV;

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

        // Calculate alpha: 100% health = 1.0 alpha, 0% health = 0.0 alpha
        float alpha = healthRatio;

        // Update color gradient if enabled, with alpha
        if (useColorGradient)
        {
            Color gradientColor = Color.Lerp(noHealthColor, fullHealthColor, healthRatio);
            HealthFillImage.color = new Color(gradientColor.r, gradientColor.g, gradientColor.b, alpha);
        }
        else
        {
            // Just update alpha, keep existing color
            Color currentColor = HealthFillImage.color;
            HealthFillImage.color = new Color(currentColor.r, currentColor.g, currentColor.b, alpha);
        }
    }
}
