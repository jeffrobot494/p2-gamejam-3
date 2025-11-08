using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;
using Newtonsoft.Json;
using System.Collections;

namespace EpsilonIV
{
    /// <summary>
    /// Response from GET /joules endpoint
    /// </summary>
    [System.Serializable]
    public class JoulesResponse
    {
        public int joules;
        public string patron_tier;
    }

    /// <summary>
    /// Manages polling the Player2 API /joules endpoint to track user's joule balance.
    /// Fires events when joule balance changes.
    /// </summary>
    public class JoulesManager : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Reference to NpcManager for API key and base URL")]
        [SerializeField] private player2_sdk.NpcManager npcManager;

        [Header("Polling Settings")]
        [Tooltip("How often to poll the joules endpoint (seconds)")]
        [SerializeField] private float pollInterval = 30f;

        [Tooltip("Start polling automatically on Start")]
        [SerializeField] private bool autoPoll = true;

        [Header("Events")]
        [Tooltip("Fired when joule balance is updated. Parameters: (joules, patronTier)")]
        public UnityEvent<int, string> OnJoulesUpdated = new UnityEvent<int, string>();

        [Tooltip("Fired when joules are low (below threshold)")]
        public UnityEvent<int> OnJoulesLow = new UnityEvent<int>();

        [Header("Low Joules Warning")]
        [Tooltip("Joule threshold for low warning")]
        [SerializeField] private int lowJoulesThreshold = 50;

        [Header("Debug")]
        [SerializeField] private bool debugMode = true;

        // State
        private int currentJoules = 0;
        private string currentPatronTier = "";
        private bool isPolling = false;
        private Coroutine pollCoroutine;

        void Start()
        {
            if (npcManager == null)
            {
                npcManager = FindFirstObjectByType<player2_sdk.NpcManager>();
            }

            if (npcManager == null)
            {
                Debug.LogError("[JoulesManager] No NpcManager found! Cannot poll joules endpoint.");
                return;
            }

            if (autoPoll)
            {
                // Wait for authentication to complete before starting polling
                if (string.IsNullOrEmpty(npcManager.ApiKey) && !npcManager.ShouldSkipAuthentication())
                {
                    if (debugMode)
                        Debug.Log("[JoulesManager] Waiting for authentication to complete before polling...");

                    npcManager.apiTokenReady.AddListener(OnAuthenticationReady);
                }
                else
                {
                    // Already authenticated or auth not needed (WebGL on player2.game)
                    StartPolling();
                }
            }
        }

        /// <summary>
        /// Called when NpcManager authentication is ready
        /// </summary>
        private void OnAuthenticationReady()
        {
            if (debugMode)
                Debug.Log("[JoulesManager] Authentication ready, starting polling");

            npcManager.apiTokenReady.RemoveListener(OnAuthenticationReady);
            StartPolling();
        }

        void OnDestroy()
        {
            StopPolling();
        }

        /// <summary>
        /// Starts polling the joules endpoint at the configured interval
        /// </summary>
        public void StartPolling()
        {
            if (isPolling)
            {
                if (debugMode)
                    Debug.LogWarning("[JoulesManager] Already polling!");
                return;
            }

            if (npcManager == null)
            {
                Debug.LogError("[JoulesManager] Cannot start polling - no NpcManager assigned!");
                return;
            }

            isPolling = true;
            pollCoroutine = StartCoroutine(PollJoulesRoutine());

            if (debugMode)
                Debug.Log($"[JoulesManager] Started polling every {pollInterval} seconds");
        }

        /// <summary>
        /// Stops polling the joules endpoint
        /// </summary>
        public void StopPolling()
        {
            if (!isPolling) return;

            isPolling = false;
            if (pollCoroutine != null)
            {
                StopCoroutine(pollCoroutine);
                pollCoroutine = null;
            }

            if (debugMode)
                Debug.Log("[JoulesManager] Stopped polling");
        }

        /// <summary>
        /// Coroutine that polls the joules endpoint at regular intervals
        /// </summary>
        private IEnumerator PollJoulesRoutine()
        {
            // Poll immediately on start
            yield return FetchJoulesAsync();

            // Then poll at intervals
            while (isPolling)
            {
                yield return new WaitForSeconds(pollInterval);
                yield return FetchJoulesAsync();
            }
        }

        /// <summary>
        /// Fetches joule balance from the API
        /// </summary>
        private IEnumerator FetchJoulesAsync()
        {
            if (npcManager == null)
            {
                Debug.LogError("[JoulesManager] Cannot fetch joules - no NpcManager!");
                yield break;
            }

            string url = $"{npcManager.GetBaseUrl()}/joules";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                // Set authentication header (unless on player2.game domain)
                if (!npcManager.ShouldSkipAuthentication())
                {
                    request.SetRequestHeader("Authorization", $"Bearer {npcManager.ApiKey}");
                }

                request.SetRequestHeader("Accept", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        string json = request.downloadHandler.text;
                        JoulesResponse response = JsonConvert.DeserializeObject<JoulesResponse>(json);

                        if (response != null)
                        {
                            int previousJoules = currentJoules;
                            currentJoules = response.joules;
                            currentPatronTier = response.patron_tier ?? "";

                            if (debugMode)
                            {
                                string tierInfo = string.IsNullOrEmpty(currentPatronTier)
                                    ? "No patron tier"
                                    : $"Patron tier: {currentPatronTier}";
                                Debug.Log($"[JoulesManager] Joules: {currentJoules} ({tierInfo})");
                            }

                            // Fire update event
                            OnJoulesUpdated?.Invoke(currentJoules, currentPatronTier);

                            // Check for low joules warning
                            if (currentJoules <= lowJoulesThreshold && (previousJoules > lowJoulesThreshold || previousJoules == 0))
                            {
                                if (debugMode)
                                    Debug.LogWarning($"[JoulesManager] Low joules! Current: {currentJoules}, Threshold: {lowJoulesThreshold}");

                                OnJoulesLow?.Invoke(currentJoules);
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[JoulesManager] Failed to parse joules response: {ex.Message}");
                    }
                }
                else
                {
                    Debug.LogError($"[JoulesManager] Failed to fetch joules: {request.error} - Response: {request.downloadHandler.text}");
                }
            }
        }

        /// <summary>
        /// Manually triggers a joule fetch (outside of polling interval)
        /// </summary>
        public void RefreshJoules()
        {
            StartCoroutine(FetchJoulesAsync());
        }

        /// <summary>
        /// Gets the current joule balance (last fetched value)
        /// </summary>
        public int GetCurrentJoules()
        {
            return currentJoules;
        }

        /// <summary>
        /// Gets the current patron tier (last fetched value)
        /// </summary>
        public string GetCurrentPatronTier()
        {
            return currentPatronTier;
        }

        /// <summary>
        /// Returns true if joules are below the low threshold
        /// </summary>
        public bool IsJoulesLow()
        {
            return currentJoules <= lowJoulesThreshold;
        }
    }
}
