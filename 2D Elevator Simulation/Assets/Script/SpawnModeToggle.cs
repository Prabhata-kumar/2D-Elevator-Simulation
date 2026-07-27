using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ElevatorSim
{
    /// <summary>
    /// Attach this script directly to a UI Button!
    /// It automatically finds the button, listens for clicks, toggles the PassengerSpawner,
    /// and updates its own TextMeshPro label to say "AUTO" or "MANUAL".
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class SpawnModeToggle : MonoBehaviour
    {
        private Button button;
        private TMP_Text buttonText;

        private void Awake()
        {
            button = GetComponent<Button>();
            buttonText = GetComponentInChildren<TMP_Text>();
            
            // Automatically listen for the button click
            button.onClick.AddListener(OnToggleClicked);
        }

        private void Start()
        {
            UpdateButtonUI();
        }

        private void OnToggleClicked()
        {
            if (ElevatorSystemManager.Instance != null)
            {
                ElevatorSystemManager.Instance.ToggleAutomated();
                UpdateButtonUI();
            }
        }

        private void UpdateButtonUI()
        {
            if (ElevatorSystemManager.Instance != null && buttonText != null)
            {
                if (ElevatorSystemManager.Instance.isAutomated)
                {
                    buttonText.text = "Mode: AUTO";
                    buttonText.color = new Color(0.1f, 0.8f, 0.1f); // Nice Green
                }
                else
                {
                    buttonText.text = "Mode: MANUAL";
                    buttonText.color = Color.red;
                }
            }
        }
    }
}
