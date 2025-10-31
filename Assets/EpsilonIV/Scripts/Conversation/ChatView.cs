using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace EpsilonIV
{
    /// <summary>
    /// Displays conversation history between player and NPCs.
    /// Shows player messages and NPC responses in a scrolling text view.
    /// </summary>
    public class ChatView : MonoBehaviour
    {
        [Header("Component References")]
        [Tooltip("MessageManager to subscribe to NPC responses (Phase 3+)")]
        public MessageManager messageManager;

        [Header("UI References")]
        [Tooltip("Text component that displays the chat history")]
        public TextMeshProUGUI chatHistoryText;

        [Tooltip("ScrollRect component for scrolling chat (optional)")]
        public ScrollRect scrollRect;

        [Tooltip("Text component that displays the active NPC's name")]
        public TextMeshProUGUI npcNameText;

        [Header("Display Settings")]
        [Tooltip("Maximum number of messages to keep in history (0 = unlimited)")]
        public int maxMessages = 50;

        [Tooltip("Prefix for player messages")]
        public string playerPrefix = "Player: ";

        [Tooltip("Color for player messages (optional)")]
        public Color playerMessageColor = Color.white;

        private int messageCount = 0;

        void Start()
        {
            if (chatHistoryText == null)
            {
                Debug.LogError("ChatView: chatHistoryText is not assigned!");
            }
            else
            {
                // Clear any initial text
                chatHistoryText.text = "";
            }

            if (npcNameText != null)
            {
                npcNameText.text = "No Active NPC";
            }

            // Subscribe to MessageManager events (Phase 4)
            if (messageManager != null)
            {
                messageManager.OnNpcResponseReceived.AddListener(OnNpcResponseReceived);
            }
        }

        void OnDestroy()
        {
            // Unsubscribe from events
            if (messageManager != null)
            {
                messageManager.OnNpcResponseReceived.RemoveListener(OnNpcResponseReceived);
            }
        }

        /// <summary>
        /// Called when MessageManager receives an NPC response.
        /// Displays the response in chat and updates the active NPC name.
        /// </summary>
        private void OnNpcResponseReceived(string npcName, string message)
        {
            AddNPCMessage(npcName, message);
            SetActiveNPCName(npcName);
        }

        /// <summary>
        /// Add a player message to the chat history.
        /// Called by RadioInputHandler via OnMessageSubmitted event.
        /// </summary>
        public void AddPlayerMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            string formattedMessage = $"{playerPrefix}{message}";
            AddMessageToHistory(formattedMessage);
        }

        /// <summary>
        /// Add an NPC message to the chat history.
        /// Called by MessageManager when NPC response is received (Phase 4).
        /// </summary>
        public void AddNPCMessage(string npcName, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            string formattedMessage = $"{npcName}: {message}";
            AddMessageToHistory(formattedMessage);
        }

        /// <summary>
        /// Update the displayed NPC name.
        /// Called by MessageManager or NPCManager when active NPC changes.
        /// </summary>
        public void SetActiveNPCName(string npcName)
        {
            if (npcNameText != null)
            {
                npcNameText.text = npcName;
            }
        }

        /// <summary>
        /// Clear all messages from the chat history.
        /// </summary>
        public void ClearChat()
        {
            if (chatHistoryText != null)
            {
                chatHistoryText.text = "";
                messageCount = 0;
            }
        }

        /// <summary>
        /// Internal method to add a formatted message to the history.
        /// New messages appear at the bottom and push older messages up.
        /// </summary>
        private void AddMessageToHistory(string formattedMessage)
        {
            if (chatHistoryText == null)
            {
                Debug.LogError("ChatView: Cannot add message - chatHistoryText is null");
                return;
            }

            // Append new message at the bottom
            if (!string.IsNullOrEmpty(chatHistoryText.text))
            {
                chatHistoryText.text = chatHistoryText.text + "\n" + formattedMessage;
            }
            else
            {
                chatHistoryText.text = formattedMessage;
            }

            messageCount++;

            // Enforce max message limit if set
            if (maxMessages > 0 && messageCount > maxMessages)
            {
                TrimOldestMessage();
            }

            // Auto-scroll to bottom
            ScrollToBottom();
        }

        /// <summary>
        /// Remove the oldest message from the history.
        /// Since new messages are at the bottom, oldest is at the top.
        /// </summary>
        private void TrimOldestMessage()
        {
            if (chatHistoryText == null || string.IsNullOrEmpty(chatHistoryText.text))
                return;

            // Find the first newline and remove everything before it (oldest message at top)
            int firstNewline = chatHistoryText.text.IndexOf('\n');
            if (firstNewline >= 0)
            {
                chatHistoryText.text = chatHistoryText.text.Substring(firstNewline + 1);
                messageCount--;
            }
        }

        /// <summary>
        /// Scroll the chat view to the bottom to show the latest message.
        /// </summary>
        private void ScrollToBottom()
        {
            if (scrollRect != null)
            {
                // Force layout rebuild before scrolling
                Canvas.ForceUpdateCanvases();

                // Scroll to bottom (verticalNormalizedPosition: 0 = bottom, 1 = top)
                scrollRect.verticalNormalizedPosition = 0f;
            }
        }
    }
}
