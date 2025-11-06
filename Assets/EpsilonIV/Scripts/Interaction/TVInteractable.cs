using UnityEngine;
using UnityEngine.Video;
using UnityEngine.Events;

namespace EpsilonIV
{
    /// <summary>
    /// Makes a TV interactable - plays a video when the player interacts with it.
    /// Requires a VideoPlayer component to function.
    /// </summary>
    [RequireComponent(typeof(VideoPlayer))]
    public class TVInteractable : MonoBehaviour, IInteractable
    {
        [Header("Interaction")]
        [Tooltip("Text shown when TV is off")]
        [SerializeField] private string playPrompt = "[E] PLAY VIDEO";

        [Tooltip("Text shown when TV is playing")]
        [SerializeField] private string stopPrompt = "[E] STOP VIDEO";

        [Header("Video Settings")]
        [Tooltip("Video clip to play (can also be set on VideoPlayer component)")]
        [SerializeField] private VideoClip videoClip;

        [Tooltip("Should the video loop when it finishes?")]
        [SerializeField] private bool loopVideo = false;

        [Tooltip("Auto-play video when scene starts")]
        [SerializeField] private bool autoPlay = false;

        [Header("Screen GameObjects")]
        [Tooltip("GameObject with video screen (enabled when TV is on)")]
        [SerializeField] private GameObject onScreen;

        [Tooltip("GameObject with black screen (enabled when TV is off)")]
        [SerializeField] private GameObject offScreen;

        [Header("Audio")]
        [Tooltip("Audio source for video sound (optional - VideoPlayer can use its own)")]
        [SerializeField] private AudioSource audioSource;

        [Header("Events")]
        [Tooltip("Fired when video starts playing")]
        public UnityEvent OnVideoStart;

        [Tooltip("Fired when video stops")]
        public UnityEvent OnVideoStop;

        [Tooltip("Fired when video finishes (only if not looping)")]
        public UnityEvent OnVideoEnd;

        [Header("Debug")]
        [Tooltip("Enable debug logging")]
        [SerializeField] private bool debugMode = false;

        private VideoPlayer videoPlayer;
        private bool isPlaying = false;

        #region Unity Lifecycle

        void Start()
        {
            // Get VideoPlayer component
            videoPlayer = GetComponent<VideoPlayer>();
            if (videoPlayer == null)
            {
                Debug.LogError($"[TVInteractable] No VideoPlayer component found on {gameObject.name}!");
                return;
            }

            // Setup video player
            SetupVideoPlayer();

            // Start with TV off
            if (!autoPlay)
            {
                SetTVState(false);
            }
            else
            {
                PlayVideo();
            }
        }

        void OnDestroy()
        {
            // Unsubscribe from video player events
            if (videoPlayer != null)
            {
                videoPlayer.loopPointReached -= OnVideoFinished;
            }
        }

        #endregion

        #region IInteractable Implementation

        public void Interact()
        {
            if (videoPlayer == null)
            {
                Debug.LogError("[TVInteractable] Cannot interact - VideoPlayer is null!");
                return;
            }

            // Toggle video playback
            if (isPlaying)
            {
                StopVideo();
            }
            else
            {
                PlayVideo();
            }
        }

        public Transform GetTransform()
        {
            return transform;
        }

        public string GetInteractionPrompt()
        {
            return isPlaying ? stopPrompt : playPrompt;
        }

        #endregion

        #region Video Control

        /// <summary>
        /// Sets up the VideoPlayer component with configured settings
        /// </summary>
        void SetupVideoPlayer()
        {
            if (videoPlayer == null) return;

            // Set video clip if provided
            if (videoClip != null)
            {
                videoPlayer.clip = videoClip;
            }

            // Configure looping
            videoPlayer.isLooping = loopVideo;

            // Setup audio if provided
            if (audioSource != null)
            {
                videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
                videoPlayer.SetTargetAudioSource(0, audioSource);
            }

            // Subscribe to events
            videoPlayer.loopPointReached += OnVideoFinished;

            // IMPORTANT: Don't prepare the video (prevents first frame from showing)
            videoPlayer.playOnAwake = false;
            videoPlayer.skipOnDrop = true;
            videoPlayer.waitForFirstFrame = false;

            if (debugMode)
                Debug.Log($"[TVInteractable] VideoPlayer setup complete on {gameObject.name}");
        }

        /// <summary>
        /// Starts playing the video (turns TV on)
        /// </summary>
        public void PlayVideo()
        {
            if (videoPlayer == null)
            {
                Debug.LogError("[TVInteractable] Cannot play - VideoPlayer is null!");
                return;
            }

            if (videoPlayer.clip == null)
            {
                Debug.LogWarning("[TVInteractable] Cannot play - no video clip assigned!");
                return;
            }

            if (debugMode)
                Debug.Log($"[TVInteractable] Turning TV ON on {gameObject.name}");

            // Turn on the TV (show video screen, hide black screen)
            SetTVState(true);

            // Play video
            videoPlayer.Play();
            isPlaying = true;

            OnVideoStart?.Invoke();
        }

        /// <summary>
        /// Stops the video playback (turns TV off)
        /// </summary>
        public void StopVideo()
        {
            if (videoPlayer == null) return;

            if (debugMode)
                Debug.Log($"[TVInteractable] Turning TV OFF on {gameObject.name}");

            // Stop video
            videoPlayer.Stop();
            isPlaying = false;

            // Turn off the TV (show black screen, hide video screen)
            SetTVState(false);

            OnVideoStop?.Invoke();
        }

        /// <summary>
        /// Sets the TV state (on/off) by enabling/disabling screen GameObjects
        /// </summary>
        private void SetTVState(bool isOn)
        {
            if (onScreen != null)
                onScreen.SetActive(isOn);

            if (offScreen != null)
                offScreen.SetActive(!isOn);

            if (debugMode)
                Debug.Log($"[TVInteractable] TV state: {(isOn ? "ON" : "OFF")}");
        }

        /// <summary>
        /// Pauses the video (can be resumed)
        /// </summary>
        public void PauseVideo()
        {
            if (videoPlayer == null) return;

            if (debugMode)
                Debug.Log($"[TVInteractable] Pausing video on {gameObject.name}");

            videoPlayer.Pause();
            isPlaying = false;
        }

        /// <summary>
        /// Called when video finishes playing (if not looping)
        /// </summary>
        void OnVideoFinished(VideoPlayer vp)
        {
            if (debugMode)
                Debug.Log($"[TVInteractable] Video finished on {gameObject.name}");

            // When video finishes (and not looping), turn TV off
            if (!loopVideo)
            {
                StopVideo();
            }

            OnVideoEnd?.Invoke();
        }

        #endregion

        #region Public Accessors

        /// <summary>
        /// Check if video is currently playing
        /// </summary>
        public bool IsPlaying => isPlaying;

        /// <summary>
        /// Get the VideoPlayer component
        /// </summary>
        public VideoPlayer VideoPlayer => videoPlayer;

        #endregion

        #region Testing / Debug

        [ContextMenu("Test: Play Video")]
        void TestPlay()
        {
            PlayVideo();
        }

        [ContextMenu("Test: Stop Video")]
        void TestStop()
        {
            StopVideo();
        }

        [ContextMenu("Test: Toggle Video")]
        void TestToggle()
        {
            Interact();
        }

        #endregion
    }
}
