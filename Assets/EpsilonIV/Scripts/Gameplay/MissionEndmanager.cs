using UnityEngine;
using UnityEngine.SceneManagement;

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

        [Tooltip("Name of the credits scene")]
        [SerializeField] private string creditsSceneName = "Credits";

        private bool missionEnded = false;

        private void Awake()
        {
            if (gameTimer == null)
                gameTimer = FindObjectOfType<Unity.FPS.Gameplay.GameTimer>();

            if (survivorManager == null)
                survivorManager = FindObjectOfType<SurvivorManager>();
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

            SceneManager.LoadScene(creditsSceneName);
        }

        private void HandleMissionFailed()
        {
            if (missionEnded) return;
            missionEnded = true;

            Debug.Log("[MissionEndManager] Mission failed – timer expired.");

            PlayerPrefs.SetInt("MissionSuccess", 0);
            PlayerPrefs.Save();

            SceneManager.LoadScene(creditsSceneName);
        }
    }
}
