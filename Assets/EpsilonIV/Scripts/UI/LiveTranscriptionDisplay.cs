using UnityEngine;
using TMPro;

namespace EpsilonIV
{
    /// <summary>
    /// Displays live STT transcription as the user speaks.
    /// Shows interim results in real-time.
    /// </summary>
    public class LiveTranscriptionDisplay : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("TextMeshPro component to display the live transcript")]
        public TextMeshProUGUI transcriptText;

        [Tooltip("MessageManager to listen for STT events")]
        public MessageManager messageManager;

        [Header("Visual Settings")]
        [Tooltip("Color for interim (partial) transcripts")]
        public Color interimColor = new Color(1f, 1f, 1f, 0.7f); // Semi-transparent white

        [Tooltip("Color for final transcripts")]
        public Color finalColor = Color.white;

        [Header("Animation")]
        [Tooltip("Show a typing indicator while listening")]
        public bool showTypingIndicator = true;

        private bool isListening = false;

        void Awake()
        {
            if (transcriptText == null)
            {
                Debug.LogError("LiveTranscriptionDisplay: transcriptText is not assigned!");
            }

            if (messageManager == null)
            {
                Debug.LogError("LiveTranscriptionDisplay: messageManager is not assigned!");
            }
        }

        void OnEnable()
        {
            if (messageManager != null && messageManager.player2STT != null)
            {
                messageManager.player2STT.OnSTTReceived.AddListener(OnTranscriptReceived);
                messageManager.player2STT.OnListeningStarted.AddListener(OnListeningStarted);
                messageManager.player2STT.OnListeningStopped.AddListener(OnListeningStopped);
            }
        }

        void OnDisable()
        {
            if (messageManager != null && messageManager.player2STT != null)
            {
                messageManager.player2STT.OnSTTReceived.RemoveListener(OnTranscriptReceived);
                messageManager.player2STT.OnListeningStarted.RemoveListener(OnListeningStarted);
                messageManager.player2STT.OnListeningStopped.RemoveListener(OnListeningStopped);
            }
        }

        private void OnListeningStarted()
        {
            isListening = true;
            if (transcriptText != null)
            {
                transcriptText.text = showTypingIndicator ? "Listening..." : "";
                transcriptText.color = interimColor;
            }
        }

        private void OnListeningStopped()
        {
            isListening = false;
            // Clear after a brief delay
            Invoke(nameof(ClearTranscript), 0.5f);
        }

        private void OnTranscriptReceived(string transcript)
        {
            if (transcriptText == null || !isListening) return;

            // Update display with latest transcript
            transcriptText.text = transcript;
            transcriptText.color = interimColor;
        }

        private void ClearTranscript()
        {
            if (transcriptText != null)
            {
                transcriptText.text = "";
            }
        }
    }
}
