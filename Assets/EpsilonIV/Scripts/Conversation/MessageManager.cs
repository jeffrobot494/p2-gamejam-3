using UnityEngine;
using UnityEngine.Events;
using player2_sdk;

namespace EpsilonIV
{
    /// <summary>
    /// Core message orchestration and API communication.
    /// Manages sending messages to NPCs via RadioNpc components.
    /// </summary>
    public class MessageManager : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Player2 SDK NPC Manager component")]
        public NpcManager npcManager;

        [Tooltip("RadioInputHandler to subscribe to message submissions")]
        public RadioInputHandler radioInputHandler;

        [Tooltip("Array of NPC GameObjects (each should have a RadioNpc component)")]
        public GameObject[] npcGameObjects;

        [Header("Events")]
        [Tooltip("Fired when NPC response is received")]
        public UnityEvent<string, string> OnNpcResponseReceived;

        void Awake()
        {
            Debug.Log("MessageManager: Awake() called");

            if (npcManager == null)
            {
                Debug.LogError("MessageManager: NpcManager is not assigned!");
            }

            if (radioInputHandler == null)
            {
                Debug.LogError("MessageManager: RadioInputHandler is not assigned!");
            }

            if (npcGameObjects == null || npcGameObjects.Length == 0)
            {
                Debug.LogWarning("MessageManager: No NPC GameObjects assigned!");
            }

            // Subscribe to responses from each RadioNpc
            SetupNPCListeners();
        }

        void OnEnable()
        {
            // Subscribe to message submission from RadioInputHandler
            if (radioInputHandler != null)
            {
                radioInputHandler.OnMessageSubmitted.AddListener(OnPlayerMessageSubmitted);
                Debug.Log("MessageManager: Subscribed to OnMessageSubmitted");
            }
        }

        void OnDisable()
        {
            // Unsubscribe from events
            if (radioInputHandler != null)
            {
                radioInputHandler.OnMessageSubmitted.RemoveListener(OnPlayerMessageSubmitted);
            }
        }

        /// <summary>
        /// Subscribe to OnRadioResponse event from each RadioNpc.
        /// </summary>
        private void SetupNPCListeners()
        {
            if (npcGameObjects == null) return;

            foreach (var npcObj in npcGameObjects)
            {
                if (npcObj == null) continue;

                RadioNpc radioNpc = npcObj.GetComponent<RadioNpc>();
                if (radioNpc == null)
                {
                    Debug.LogWarning($"MessageManager: NPC GameObject {npcObj.name} has no RadioNpc component");
                    continue;
                }

                // Subscribe to this NPC's responses
                radioNpc.OnRadioResponse.RemoveListener(OnNpcResponse); // Prevent duplicates
                radioNpc.OnRadioResponse.AddListener(OnNpcResponse);

                Debug.Log($"MessageManager: Subscribed to responses from {npcObj.name}");
            }
        }

        /// <summary>
        /// Get the currently active NPC from the npcGameObjects array.
        /// Returns the first active GameObject with a RadioNpc component.
        /// </summary>
        private RadioNpc GetActiveNPC()
        {
            if (npcGameObjects == null || npcGameObjects.Length == 0)
            {
                Debug.LogError("MessageManager: Cannot get active NPC - npcGameObjects array is empty");
                return null;
            }

            // Search through all NPC GameObjects for an active one
            foreach (var npcObj in npcGameObjects)
            {
                if (npcObj != null && npcObj.activeSelf)
                {
                    var radioNpc = npcObj.GetComponent<RadioNpc>();
                    if (radioNpc != null)
                    {
                        Debug.Log($"MessageManager: Found active NPC: {npcObj.name}");
                        return radioNpc;
                    }
                }
            }

            Debug.LogWarning("MessageManager: No active NPC found in npcGameObjects array");
            return null;
        }

        /// <summary>
        /// Called when player submits a message via RadioInputHandler.
        /// Sends the message to the active NPC.
        /// </summary>
        private void OnPlayerMessageSubmitted(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                Debug.LogWarning("MessageManager: Received empty message");
                return;
            }

            Debug.Log($"MessageManager: Player message received: '{message}'");

            // Get active NPC
            RadioNpc activeNpc = GetActiveNPC();
            if (activeNpc == null)
            {
                Debug.LogError("MessageManager: Cannot send message - no active NPC");
                return;
            }

            // Send message to NPC
            string context = ""; // TODO: Phase 7 - Get from SurvivorProfile.knowledgeBase
            activeNpc.SendMessage(message, context);

            Debug.Log($"MessageManager: Sent message to NPC '{activeNpc.name}'");
        }

        /// <summary>
        /// Called when NPC response is received from RadioNpc.OnRadioResponse event.
        /// </summary>
        private void OnNpcResponse(string npcName, string responseMessage)
        {
            Debug.Log($"MessageManager: Received response from '{npcName}': '{responseMessage}'");

            // Fire event for other components (ChatView, RadioAudioPlayer)
            OnNpcResponseReceived?.Invoke(npcName, responseMessage);

            // TODO: Phase 5 - Extract audio and send to RadioAudioPlayer
        }

        // TODO: Phase 6 - Add STT methods
        // public void StartSTT()
        // {
        //     player2STT.StartSTT();
        // }
        //
        // public void StopSTT()
        // {
        //     player2STT.StopSTT();
        // }
    }
}
