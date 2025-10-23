// ListenerDebugFlash.cs
using System.Collections;
using UnityEngine;

/// <summary>
/// Debug component that flashes a GameObject's material when sounds are checked.
/// Dark blue for sounds below threshold, yellow for sounds that are heard.
/// </summary>
public class ListenerDebugFlash : MonoBehaviour
{
    [Header("Flash Colors")]
    [SerializeField] private Color checkedColor = new Color(0f, 0f, 0.5f, 1f); // Dark blue
    [SerializeField] private Color heardColor = Color.yellow;

    [Header("Flash Settings")]
    [SerializeField] private float flashDuration = 0.2f;

    private Renderer targetRenderer;
    private Material originalMaterial;
    private Material flashMaterial;
    private Coroutine flashCoroutine;

    private void Awake()
    {
        targetRenderer = GetComponent<Renderer>();

        if (targetRenderer != null)
        {
            originalMaterial = targetRenderer.material;
            flashMaterial = new Material(originalMaterial);
        }
        else
        {
            Debug.LogWarning($"ListenerDebugFlash on {gameObject.name} requires a Renderer component.", this);
        }
    }

    /// <summary>
    /// Flash dark blue when a sound is checked but below threshold.
    /// </summary>
    public void FlashChecked()
    {
        Flash(checkedColor);
    }

    /// <summary>
    /// Flash yellow when a sound is heard (above threshold).
    /// </summary>
    public void FlashHeard()
    {
        Flash(heardColor);
    }

    private void Flash(Color color)
    {
        if (targetRenderer == null || flashMaterial == null)
            return;

        // Stop any existing flash
        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(FlashCoroutine(color));
    }

    private IEnumerator FlashCoroutine(Color color)
    {
        // Set flash color
        flashMaterial.color = color;
        targetRenderer.material = flashMaterial;

        // Wait for flash duration
        yield return new WaitForSeconds(flashDuration);

        // Restore original material
        targetRenderer.material = originalMaterial;
        flashCoroutine = null;
    }

    private void OnDestroy()
    {
        // Clean up the flash material instance
        if (flashMaterial != null)
            Destroy(flashMaterial);
    }
}
