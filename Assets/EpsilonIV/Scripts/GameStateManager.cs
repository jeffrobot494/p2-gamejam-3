using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;
using TMPro;

namespace Unity.FPS.Gameplay
{
    /// <summary>
    /// Manages game state transitions for single-scene WebGL game
    /// Flow: StartScreen → OpeningCutscene → Gameplay → EndingCutscene
    /// </summary>
    public class GameStateManager : MonoBehaviour
    {
        public enum GameState
        {
            StartScreen,
            OpeningCutscene,
            Gameplay,
            EndingCutscene,
            GameOver
        }

        [Header("Debug")]
        [Tooltip("State to start in when Play is pressed (for testing)")]
        public GameState DebugStartState = GameState.StartScreen;

        [Header("References")]
        [Tooltip("The player character controller")]
        public PlayerCharacterController PlayerController;

        [Tooltip("Timeline for opening cutscene")]
        public PlayableDirector OpeningCutsceneTimeline;

        [Tooltip("Timeline for ending cutscene")]
        public PlayableDirector EndingCutsceneTimeline;

        [Header("UI Panels")]
        [Tooltip("Start screen UI panel")]
        public GameObject StartScreenPanel;

        [Tooltip("Gameplay HUD panel")]
        public GameObject GameplayHUDPanel;

        [Tooltip("Game over UI panel")]
        public GameObject GameOverPanel;

        [Header("Start Screen Settings")]
        [Tooltip("Text prompt on start screen")]
        public TextMeshProUGUI StartScreenPrompt;

        [Tooltip("Text to display on start screen")]
        public string StartPromptText = "Press ENTER to begin";

        [Header("Fade Settings")]
        [Tooltip("Canvas group for screen fades")]
        public CanvasGroup FadeCanvasGroup;

        [Tooltip("Duration of fade transitions")]
        public float FadeDuration = 1f;

        // Current state
        private GameState m_CurrentState;
        private bool m_IsTransitioning = false;
        private float m_FadeTimer = 0f;
        private bool m_IsFadingOut = false;

        void Start()
        {
            // Initialize to debug start state
            TransitionToState(DebugStartState);
        }

        void Update()
        {
            // Handle state-specific updates
            switch (m_CurrentState)
            {
                case GameState.StartScreen:
                    UpdateStartScreen();
                    break;

                case GameState.OpeningCutscene:
                    UpdateOpeningCutscene();
                    break;

                case GameState.Gameplay:
                    UpdateGameplay();
                    break;

                case GameState.EndingCutscene:
                    UpdateEndingCutscene();
                    break;
            }

            // Handle fade transitions
            if (m_IsTransitioning)
            {
                UpdateFade();
            }
        }

        #region State Updates

        void UpdateStartScreen()
        {
            // Wait for Enter key to start
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                StartGame();
            }
        }

        void UpdateOpeningCutscene()
        {
            // Cutscene is handled by Timeline
            // We just wait for it to finish (handled by OnCutsceneComplete callback)
        }

        void UpdateGameplay()
        {
            // Main gameplay - managed by other systems
            // Check for win/lose conditions here if needed
        }

        void UpdateEndingCutscene()
        {
            // Ending cutscene - wait for Timeline to finish
        }

        #endregion

        #region State Transitions

        public void TransitionToState(GameState newState)
        {
            if (m_CurrentState == newState)
                return;

            Debug.Log($"[GameStateManager] Transitioning from {m_CurrentState} to {newState}");

            // Exit current state
            ExitState(m_CurrentState);

            // Update state
            m_CurrentState = newState;

            // Enter new state
            EnterState(newState);
        }

        void EnterState(GameState state)
        {
            switch (state)
            {
                case GameState.StartScreen:
                    EnterStartScreen();
                    break;

                case GameState.OpeningCutscene:
                    EnterOpeningCutscene();
                    break;

                case GameState.Gameplay:
                    EnterGameplay();
                    break;

                case GameState.EndingCutscene:
                    EnterEndingCutscene();
                    break;

                case GameState.GameOver:
                    EnterGameOver();
                    break;
            }
        }

        void ExitState(GameState state)
        {
            switch (state)
            {
                case GameState.StartScreen:
                    ExitStartScreen();
                    break;

                case GameState.OpeningCutscene:
                    ExitOpeningCutscene();
                    break;

                case GameState.Gameplay:
                    ExitGameplay();
                    break;

                case GameState.EndingCutscene:
                    ExitEndingCutscene();
                    break;
            }
        }

        #endregion

        #region State Enter/Exit Logic

        void EnterStartScreen()
        {
            // Show start screen UI
            SetUIPanel(StartScreenPanel, true);
            SetUIPanel(GameplayHUDPanel, false);
            SetUIPanel(GameOverPanel, false);

            if (StartScreenPrompt != null)
            {
                StartScreenPrompt.text = StartPromptText;
            }

            // Disable player control
            if (PlayerController != null)
            {
                PlayerController.enabled = false;
            }

            // Unlock cursor for start screen
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void ExitStartScreen()
        {
            // Hide start screen UI
            SetUIPanel(StartScreenPanel, false);
        }

        void EnterOpeningCutscene()
        {
            // Disable player control during cutscene
            if (PlayerController != null)
            {
                PlayerController.enabled = false;
            }

            // Play opening cutscene timeline
            if (OpeningCutsceneTimeline != null)
            {
                // Subscribe to timeline completion
                OpeningCutsceneTimeline.stopped += OnOpeningCutsceneComplete;
                OpeningCutsceneTimeline.Play();
            }
            else
            {
                Debug.LogWarning("[GameStateManager] No opening cutscene timeline assigned! Skipping to gameplay.");
                TransitionToState(GameState.Gameplay);
            }
        }

        void ExitOpeningCutscene()
        {
            // Unsubscribe from timeline
            if (OpeningCutsceneTimeline != null)
            {
                OpeningCutsceneTimeline.stopped -= OnOpeningCutsceneComplete;
            }
        }

        void EnterGameplay()
        {
            // Show gameplay HUD
            SetUIPanel(StartScreenPanel, false);
            SetUIPanel(GameplayHUDPanel, true);
            SetUIPanel(GameOverPanel, false);

            // Enable player control
            if (PlayerController != null)
            {
                PlayerController.enabled = true;
            }

            // Lock cursor for gameplay
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void ExitGameplay()
        {
            // Disable player control
            if (PlayerController != null)
            {
                PlayerController.enabled = false;
            }
        }

        void EnterEndingCutscene()
        {
            // Hide gameplay HUD
            SetUIPanel(GameplayHUDPanel, false);

            // Play ending cutscene timeline
            if (EndingCutsceneTimeline != null)
            {
                EndingCutsceneTimeline.stopped += OnEndingCutsceneComplete;
                EndingCutsceneTimeline.Play();
            }
            else
            {
                Debug.LogWarning("[GameStateManager] No ending cutscene timeline assigned! Going to game over.");
                TransitionToState(GameState.GameOver);
            }
        }

        void ExitEndingCutscene()
        {
            // Unsubscribe from timeline
            if (EndingCutsceneTimeline != null)
            {
                EndingCutsceneTimeline.stopped -= OnEndingCutsceneComplete;
            }
        }

        void EnterGameOver()
        {
            // Show game over UI
            SetUIPanel(GameOverPanel, true);
            SetUIPanel(GameplayHUDPanel, false);

            // Unlock cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Called when player presses Enter on start screen
        /// </summary>
        public void StartGame()
        {
            TransitionToState(GameState.OpeningCutscene);
        }

        /// <summary>
        /// Triggers the ending cutscene (call when player completes objective)
        /// </summary>
        public void TriggerEnding()
        {
            TransitionToState(GameState.EndingCutscene);
        }

        /// <summary>
        /// Triggers game over (call when player dies)
        /// </summary>
        public void TriggerGameOver()
        {
            TransitionToState(GameState.GameOver);
        }

        /// <summary>
        /// Restarts the game from the beginning
        /// </summary>
        public void RestartGame()
        {
            // You could reload the scene, or just reset to start screen
            TransitionToState(GameState.StartScreen);
        }

        #endregion

        #region Timeline Callbacks

        void OnOpeningCutsceneComplete(PlayableDirector director)
        {
            // Opening cutscene finished, transition to gameplay
            TransitionToState(GameState.Gameplay);
        }

        void OnEndingCutsceneComplete(PlayableDirector director)
        {
            // Ending cutscene finished, go to game over
            TransitionToState(GameState.GameOver);
        }

        #endregion

        #region Fade System

        void UpdateFade()
        {
            m_FadeTimer += Time.deltaTime;
            float progress = m_FadeTimer / FadeDuration;

            if (m_IsFadingOut)
            {
                FadeCanvasGroup.alpha = Mathf.Lerp(0f, 1f, progress);
            }
            else
            {
                FadeCanvasGroup.alpha = Mathf.Lerp(1f, 0f, progress);
            }

            if (progress >= 1f)
            {
                m_IsTransitioning = false;
            }
        }

        public void FadeOut()
        {
            m_IsTransitioning = true;
            m_IsFadingOut = true;
            m_FadeTimer = 0f;
        }

        public void FadeIn()
        {
            m_IsTransitioning = true;
            m_IsFadingOut = false;
            m_FadeTimer = 0f;
        }

        #endregion

        #region Utility

        void SetUIPanel(GameObject panel, bool active)
        {
            if (panel != null)
            {
                panel.SetActive(active);
            }
        }

        #endregion
    }
}
