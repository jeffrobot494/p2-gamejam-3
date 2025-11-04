using UnityEngine;
using player2_sdk;
using Newtonsoft.Json.Linq;

namespace EpsilonIV
{
    /// <summary>
    /// Handles function calls from the Player2 LLM for the dangling survivor.
    /// When the LLM decides the survivor is convinced to let go, it calls the "let_go" function.
    /// This handler intercepts that call and triggers the DanglingSurvivor's LetGo() method.
    ///
    /// Setup Instructions:
    /// 1. Attach this to the same GameObject as DanglingSurvivor
    /// 2. Connect NpcManager's functionHandler event to HandleFunctionCall() in Inspector
    /// 3. Assign the DanglingSurvivor reference (or leave blank for auto-find)
    /// </summary>
    public class DanglingNpcFunctionHandler : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The DanglingSurvivor this handler controls (auto-found if not assigned)")]
        [SerializeField] private DanglingSurvivor danglingSurvivor;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogging = true;

        #region Unity Lifecycle

        private void Awake()
        {
            // Auto-find DanglingSurvivor if not assigned
            if (danglingSurvivor == null)
            {
                danglingSurvivor = GetComponent<DanglingSurvivor>();
                if (danglingSurvivor == null)
                {
                    Debug.LogError($"[DanglingNpcFunctionHandler] No DanglingSurvivor component found on {gameObject.name}!");
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Main function call handler - called by NpcManager when ANY NPC executes a function.
        /// We filter to only handle calls from OUR specific dangling survivor.
        ///
        /// This should be connected to NpcManager's functionHandler UnityEvent in the Inspector.
        /// </summary>
        public void HandleFunctionCall(FunctionCall functionCall)
        {
            // Filter: Only handle calls from OUR specific survivor
            if (danglingSurvivor == null || functionCall.aiObject != danglingSurvivor.gameObject)
            {
                // This function call is for a different NPC, ignore it
                return;
            }

            if (enableDebugLogging)
            {
                Debug.Log($"[DanglingNpcFunctionHandler] Processing function call: '{functionCall.name}' from {danglingSurvivor.gameObject.name}");
            }

            // Route to appropriate handler based on function name
            switch (functionCall.name)
            {
                case "let_go":
                    HandleLetGo(functionCall);
                    break;

                default:
                    if (enableDebugLogging)
                        Debug.LogWarning($"[DanglingNpcFunctionHandler] Unknown function: {functionCall.name}");
                    break;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Handles the "let_go" function call from the LLM.
        /// This is invoked when the AI believes the survivor is convinced to come down.
        /// </summary>
        private void HandleLetGo(FunctionCall functionCall)
        {
            // Extract optional reason argument if the LLM provided one
            string reason = GetArgumentValue(functionCall.arguments, "reason", "I trust you");

            if (enableDebugLogging)
            {
                Debug.Log($"[DanglingNpcFunctionHandler] LLM called let_go! Reason: '{reason}'");
            }

            // Trigger the survivor's LetGo method
            danglingSurvivor.LetGo(reason);
        }

        /// <summary>
        /// Helper method to safely extract argument values from JObject with type conversion and defaults
        /// </summary>
        private T GetArgumentValue<T>(JObject arguments, string key, T defaultValue)
        {
            if (arguments == null) return defaultValue;

            if (arguments.TryGetValue(key, out var token))
            {
                try
                {
                    return token.Value<T>();
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[DanglingNpcFunctionHandler] Failed to convert argument '{key}' to {typeof(T).Name}: {ex.Message}");
                    return defaultValue;
                }
            }

            if (enableDebugLogging)
                Debug.Log($"[DanglingNpcFunctionHandler] Using default value for missing argument '{key}': {defaultValue}");

            return defaultValue;
        }

        #endregion

        #region Testing / Debug

        [ContextMenu("Test: Simulate LLM Let Go Call")]
        private void TestSimulateLetGo()
        {
            if (danglingSurvivor != null)
            {
                Debug.Log("[DanglingNpcFunctionHandler] Simulating LLM let_go call for testing...");
                danglingSurvivor.LetGo("Test call from context menu");
            }
            else
            {
                Debug.LogError("[DanglingNpcFunctionHandler] Cannot test - DanglingSurvivor not assigned!");
            }
        }

        #endregion
    }
}
