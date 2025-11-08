using UnityEngine;
using player2_sdk;

namespace EpsilonIV
{
    /// <summary>
    /// Manages audio settings: NPC TTS audio and general game audio.
    /// Provides methods to mute/unmute and persists settings via PlayerPrefs.
    /// </summary>
    public class AudioSettingsManager : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("AudioListener to control all game audio (usually on player camera)")]
        [SerializeField] private AudioListener audioListener;

        [Tooltip("NpcManager to control NPC TTS audio")]
        [SerializeField] private NpcManager npcManager;

        [Header("Default Settings")]
        [Tooltip("Is NPC audio enabled by default?")]
        [SerializeField] private bool npcAudioEnabledByDefault = true;

        [Tooltip("Is game audio enabled by default?")]
        [SerializeField] private bool gameAudioEnabledByDefault = true;

        [Header("PlayerPrefs Keys")]
        [SerializeField] private string npcAudioKey = "Settings_NPCAudio";
        [SerializeField] private string gameAudioKey = "Settings_GameAudio";

        [Header("Debug")]
        [SerializeField] private bool debugMode = true;

        // Current state
        private bool npcAudioEnabled = true;
        private bool gameAudioEnabled = true;

        public bool NPCAudioEnabled => npcAudioEnabled;
        public bool GameAudioEnabled => gameAudioEnabled;

        void Awake()
        {
            // Auto-find references if not assigned
            if (audioListener == null)
            {
                audioListener = FindFirstObjectByType<AudioListener>();
            }

            if (npcManager == null)
            {
                npcManager = FindFirstObjectByType<NpcManager>();
            }

            // Validate references
            if (audioListener == null)
            {
                Debug.LogWarning("[AudioSettingsManager] No AudioListener found. Game audio toggle will not work.");
            }

            if (npcManager == null)
            {
                Debug.LogWarning("[AudioSettingsManager] No NpcManager found. NPC audio toggle will not work.");
            }
        }

        void Start()
        {
            // Load saved settings or use defaults
            LoadSettings();
            ApplySettings();

            if (debugMode)
            {
                Debug.Log($"[AudioSettingsManager] Start complete - AudioListener.pause = {AudioListener.pause}");
            }
        }

        /// <summary>
        /// Toggle NPC audio on/off
        /// </summary>
        public void ToggleNPCAudio()
        {
            SetNPCAudio(!npcAudioEnabled);
        }

        /// <summary>
        /// Toggle game audio on/off
        /// </summary>
        public void ToggleGameAudio()
        {
            SetGameAudio(!gameAudioEnabled);
        }

        /// <summary>
        /// Enable or disable NPC TTS audio
        /// </summary>
        public void SetNPCAudio(bool enabled)
        {
            npcAudioEnabled = enabled;

            if (npcManager != null)
            {
                npcManager.TTS = enabled;
                if (debugMode)
                    Debug.Log($"[AudioSettingsManager] NPC TTS: {(enabled ? "ENABLED" : "MUTED")}");
                
            }

            // Save setting
            PlayerPrefs.SetInt(npcAudioKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Enable or disable all game audio
        /// </summary>
        public void SetGameAudio(bool enabled)
        {
            gameAudioEnabled = enabled;

            if (audioListener != null)
            {
                // Unity's AudioListener.pause pauses ALL audio playback
                AudioListener.pause = !enabled;

                if (debugMode)
                    Debug.Log($"[AudioSettingsManager] Game Audio: {(enabled ? "ENABLED" : "PAUSED")}");
            }

            // Save setting
            PlayerPrefs.SetInt(gameAudioKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Load settings from PlayerPrefs
        /// </summary>
        private void LoadSettings()
        {
            // Load NPC audio setting (default to true if not set)
            npcAudioEnabled = PlayerPrefs.GetInt(npcAudioKey, npcAudioEnabledByDefault ? 1 : 0) == 1;

            // Load game audio setting (default to true if not set)
            gameAudioEnabled = PlayerPrefs.GetInt(gameAudioKey, gameAudioEnabledByDefault ? 1 : 0) == 1;

            if (debugMode)
            {
                Debug.Log($"[AudioSettingsManager] Loaded settings - NPC Audio: {npcAudioEnabled}, Game Audio: {gameAudioEnabled}");
            }
        }

        /// <summary>
        /// Apply current settings to audio systems
        /// </summary>
        private void ApplySettings()
        {
            SetNPCAudio(npcAudioEnabled);
            SetGameAudio(gameAudioEnabled);
        }

        /// <summary>
        /// Reset all audio settings to defaults
        /// </summary>
        public void ResetToDefaults()
        {
            SetNPCAudio(npcAudioEnabledByDefault);
            SetGameAudio(gameAudioEnabledByDefault);

            if (debugMode)
                Debug.Log("[AudioSettingsManager] Reset to default settings");
        }
    }
}
