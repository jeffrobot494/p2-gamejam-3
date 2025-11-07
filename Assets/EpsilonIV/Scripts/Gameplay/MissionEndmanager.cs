using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace EpsilonIV
{
    /// <summary>
    /// Listens for mission completion or failure events and loads the Credits scene
    /// with an appropriate message.
    /// </summary>
    public class MissionEndManager : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Reference to the GameTimer component")]
        [SerializeField] private Unity.FPS.Gameplay.GameTimer gameTimer;

        [Tooltip("Reference to the SurvivorManager component")]
        [SerializeField] private SurvivorManager survivorManager;

        [Tooltip("Canvas group for fade to black effect")]
        [SerializeField] private CanvasGroup fadeCanvasGroup;

        [Header("Settings")]
        [Tooltip("Name of the credits scene")]
        [SerializeField] private string creditsSceneName = "Credits";

        [Tooltip("Duration of fade to black in seconds")]
        [SerializeField] private float fadeDuration = 1.5f;

        private bool missionEnded = false;

        private void Awake()
        {
            if (gameTimer == null)
                gameTimer = FindObjectOfType<Unity.FPS.Gameplay.GameTimer>();

            if (survivorManager == null)
                survivorManager = FindObjectOfType<SurvivorManager>();

            // Ensure fade canvas starts transparent and disabled
            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = 0f;
                fadeCanvasGroup.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (gameTimer != null)
                gameTimer.OnTimerExpired.AddListener(HandleMissionFailed);

            if (survivorManager != null)
                survivorManager.OnAllSurvivorsRescued.AddListener(HandleMissionSuccess);
        }

        private void OnDisable()
        {
            if (gameTimer != null)
                gameTimer.OnTimerExpired.RemoveListener(HandleMissionFailed);

            if (survivorManager != null)
                survivorManager.OnAllSurvivorsRescued.RemoveListener(HandleMissionSuccess);
        }

        private void HandleMissionSuccess()
        {
            if (missionEnded) return;
            missionEnded = true;

            Debug.Log("[MissionEndManager] Mission success – all survivors rescued!");

            PlayerPrefs.SetInt("MissionSuccess", 1);
            PlayerPrefs.Save();

            StartCoroutine(FadeToBlackAndLoadScene(creditsSceneName));
        }

        private void HandleMissionFailed()
        {
            if (missionEnded) return;
            missionEnded = true;

            Debug.Log("[MissionEndManager] Mission failed – timer expired.");

            PlayerPrefs.SetInt("MissionSuccess", 0);
            PlayerPrefs.Save();

            StartCoroutine(FadeToBlackAndLoadScene(creditsSceneName));
        }

        private IEnumerator FadeToBlackAndLoadScene(string sceneName)
        {
            if (fadeCanvasGroup == null)
            {
                Debug.LogWarning("[MissionEndManager] No fade canvas group assigned. Loading scene immediately.");
                SceneManager.LoadScene(sceneName);
                yield break;
            }

            Debug.Log($"[MissionEndManager] Starting fade to black (duration: {fadeDuration}s)");

            // Ensure canvas group is active and starts fully transparent
            fadeCanvasGroup.gameObject.SetActive(true);
            fadeCanvasGroup.alpha = 0f;

            float elapsed = 0f;

            // Fade from transparent (alpha 0) to opaque (alpha 1)
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Clamp01(elapsed / fadeDuration);
                fadeCanvasGroup.alpha = alpha;
                yield return null;
            }

            // Ensure fully opaque
            fadeCanvasGroup.alpha = 1f;

            // Load scene after fade completes
            SceneManager.LoadScene(sceneName);
        }
    }
}
