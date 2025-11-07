using UnityEngine;
using TMPro; // remove if using UnityEngine.UI.Text

namespace EpsilonIV
{
    /// <summary>
    /// Displays the appropriate message depending on whether the player succeeded or failed.
    /// </summary>
    public class CreditsSceneController : MonoBehaviour
    {
        [SerializeField] private TMP_Text messageText;

        [TextArea]
        [SerializeField] private string successMessage = "You found all the survivors and escaped in time.";
        
        [TextArea]
        [SerializeField] private string failureMessage = "You failed to rescue all the workers, maybe give it another try.";

        void Start()
        {
            int success = PlayerPrefs.GetInt("MissionSuccess", 0);
            messageText.text = success == 1 ? successMessage : failureMessage;

            // Optional: clear PlayerPrefs so it doesn't persist between runs
            PlayerPrefs.DeleteKey("MissionSuccess");
        }
    }
}
