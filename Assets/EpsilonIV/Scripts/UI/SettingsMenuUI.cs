using UnityEngine;
using UnityEngine.UI;

namespace EpsilonIV
{
    /// <summary>
    /// Controls the settings menu UI panel visibility and binds checkboxes to AudioSettingsManager.
    /// Shows/hides automatically based on InputStateManager events.
    /// </summary>
    public class SettingsMenuUI : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The panel GameObject to show/hide")]
        [SerializeField] private GameObject menuPanel;

        [Tooltip("InputStateManager to listen for menu state changes")]
        [SerializeField] private InputStateManager inputStateManager;

        [Tooltip("AudioSettingsManager to control audio settings")]
        [SerializeField] private AudioSettingsManager audioSettingsManager;

        [Header("UI Elements")]
        [Tooltip("Toggle for NPC audio on/off")]
        [SerializeField] private Toggle npcAudioToggle;

        [Tooltip("Toggle for game audio on/off")]
        [SerializeField] private Toggle gameAudioToggle;

        [Header("Debug")]
        [SerializeField] private bool debugMode = true;

        void Awake()
        {
            // Auto-find references if not assigned
            if (inputStateManager == null)
            {
                inputStateManager = FindFirstObjectByType<InputStateManager>();
            }

            if (audioSettingsManager == null)
            {
                audioSettingsManager = FindFirstObjectByType<AudioSettingsManager>();
            }

            // Validate references
            if (inputStateManager == null)
            {
                Debug.LogError("[SettingsMenuUI] No InputStateManager found! Menu won't show/hide correctly.");
            }

            if (audioSettingsManager == null)
            {
                Debug.LogError("[SettingsMenuUI] No AudioSettingsManager found! Audio toggles won't work.");
            }

            if (menuPanel == null)
            {
                Debug.LogError("[SettingsMenuUI] Menu panel not assigned! Assign the panel GameObject in inspector.");
            }
        }

        void Start()
        {
            // Subscribe to InputStateManager events
            if (inputStateManager != null)
            {
                inputStateManager.OnEnterMenuState.AddListener(ShowMenu);
                inputStateManager.OnExitMenuState.AddListener(HideMenu);
            }

            // Initialize toggle listeners
            if (npcAudioToggle != null && audioSettingsManager != null)
            {
                // Set initial state from AudioSettingsManager
                npcAudioToggle.isOn = audioSettingsManager.NPCAudioEnabled;

                // Listen for toggle changes
                npcAudioToggle.onValueChanged.AddListener(OnNPCAudioToggleChanged);
            }

            if (gameAudioToggle != null && audioSettingsManager != null)
            {
                // Set initial state from AudioSettingsManager
                gameAudioToggle.isOn = audioSettingsManager.GameAudioEnabled;

                // Listen for toggle changes
                gameAudioToggle.onValueChanged.AddListener(OnGameAudioToggleChanged);
            }

            // Start with menu hidden
            if (menuPanel != null)
            {
                menuPanel.SetActive(false);
            }
        }

        void OnDestroy()
        {
            // Unsubscribe from events
            if (inputStateManager != null)
            {
                inputStateManager.OnEnterMenuState.RemoveListener(ShowMenu);
                inputStateManager.OnExitMenuState.RemoveListener(HideMenu);
            }

            if (npcAudioToggle != null)
            {
                npcAudioToggle.onValueChanged.RemoveListener(OnNPCAudioToggleChanged);
            }

            if (gameAudioToggle != null)
            {
                gameAudioToggle.onValueChanged.RemoveListener(OnGameAudioToggleChanged);
            }
        }

        /// <summary>
        /// Show the settings menu
        /// </summary>
        private void ShowMenu()
        {
            if (menuPanel != null)
            {
                menuPanel.SetActive(true);

                if (debugMode)
                    Debug.Log("[SettingsMenuUI] Menu shown");
            }
        }

        /// <summary>
        /// Hide the settings menu
        /// </summary>
        private void HideMenu()
        {
            if (menuPanel != null)
            {
                menuPanel.SetActive(false);

                if (debugMode)
                    Debug.Log("[SettingsMenuUI] Menu hidden");
            }
        }

        /// <summary>
        /// Called when NPC audio toggle is changed
        /// </summary>
        private void OnNPCAudioToggleChanged(bool isOn)
        {
            if (audioSettingsManager != null)
            {
                audioSettingsManager.SetNPCAudio(isOn);

                if (debugMode)
                    Debug.Log($"[SettingsMenuUI] NPC Audio toggled: {isOn}");
            }
        }

        /// <summary>
        /// Called when game audio toggle is changed
        /// </summary>
        private void OnGameAudioToggleChanged(bool isOn)
        {
            if (audioSettingsManager != null)
            {
                audioSettingsManager.SetGameAudio(isOn);

                if (debugMode)
                    Debug.Log($"[SettingsMenuUI] Game Audio toggled: {isOn}");
            }
        }

        /// <summary>
        /// Refresh toggle states from AudioSettingsManager (for external changes)
        /// </summary>
        public void RefreshToggles()
        {
            if (audioSettingsManager == null) return;

            if (npcAudioToggle != null)
            {
                npcAudioToggle.isOn = audioSettingsManager.NPCAudioEnabled;
            }

            if (gameAudioToggle != null)
            {
                gameAudioToggle.isOn = audioSettingsManager.GameAudioEnabled;
            }
        }
    }
}
