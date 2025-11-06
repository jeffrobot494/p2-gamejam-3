using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace EpsilonIV
{
    /// <summary>
    /// Quick test script to spawn a UI button for testing Menu/Gameplay state transitions.
    /// Press T to spawn a test button, then test clicking it while in different states.
    /// </summary>
    public class TestButtonSpawner : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Press this key to spawn a test button")]
        public KeyCode spawnKey = KeyCode.T;

        [Header("Button Appearance")]
        public Vector2 buttonSize = new Vector2(200, 50);
        public Vector2 buttonPosition = new Vector2(0, 100); // Offset from center

        private Canvas canvas;
        private bool buttonSpawned = false;

        void Update()
        {
            if (Input.GetKeyDown(spawnKey) && !buttonSpawned)
            {
                SpawnTestButton();
            }
        }

        void SpawnTestButton()
        {
            // Find or create Canvas
            canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("TestCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();

                // Add EventSystem if not present
                if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
                {
                    GameObject eventSystemObj = new GameObject("EventSystem");
                    eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                    eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                }
            }

            // Create button GameObject
            GameObject buttonObj = new GameObject("TestButton");
            buttonObj.transform.SetParent(canvas.transform, false);

            // Add RectTransform
            RectTransform rectTransform = buttonObj.AddComponent<RectTransform>();
            rectTransform.sizeDelta = buttonSize;
            rectTransform.anchoredPosition = buttonPosition;

            // Add Image component (button background)
            Image image = buttonObj.AddComponent<Image>();
            image.color = new Color(0.2f, 0.6f, 1f, 0.8f); // Nice blue color

            // Add Button component
            Button button = buttonObj.AddComponent<Button>();

            // Set button colors
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.2f, 0.6f, 1f, 0.8f);
            colors.highlightedColor = new Color(0.3f, 0.7f, 1f, 1f);
            colors.pressedColor = new Color(0.1f, 0.5f, 0.9f, 1f);
            colors.selectedColor = new Color(0.2f, 0.6f, 1f, 0.8f);
            button.colors = colors;

            // Add onClick event
            button.onClick.AddListener(OnTestButtonClicked);

            // Create text child
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);

            // Use TextMeshProUGUI
            TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
            tmpText.text = "Click Me!";
            tmpText.fontSize = 24;
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.color = Color.white;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            buttonSpawned = true;
            Debug.Log("[TestButtonSpawner] Test button spawned! Try clicking it in Menu state vs Gameplay state.");
        }

        void OnTestButtonClicked()
        {
            Debug.Log("[TestButtonSpawner] ✓ BUTTON CLICKED! UI interaction is working.");

            // Flash the button green to show it worked
            Button button = GameObject.Find("TestButton")?.GetComponent<Button>();
            if (button != null)
            {
                Image image = button.GetComponent<Image>();
                if (image != null)
                {
                    StartCoroutine(FlashButton(image));
                }
            }
        }

        System.Collections.IEnumerator FlashButton(Image image)
        {
            Color originalColor = image.color;
            image.color = Color.green;
            yield return new WaitForSeconds(0.2f);
            image.color = originalColor;
        }
    }
}
