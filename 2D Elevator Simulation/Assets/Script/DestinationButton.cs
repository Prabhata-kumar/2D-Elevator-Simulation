using UnityEngine;
using UnityEngine.UI;

namespace ElevatorSim
{
    /// <summary>
    /// Attach this script to UI Buttons to act as the "Inside Elevator" destination buttons.
    /// Example: A button for "Floor 3". When pressed, it tells the elevator to travel to Floor 3.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class DestinationButton : MonoBehaviour
    {
        [Header("Manual Control")]
        [Tooltip("Drag the Elevator GameObject you want to control here")]
        public ElevatorController targetElevator;
        
        [Tooltip("The floor number this button should send the elevator to (0, 1, 2, 3)")]
        public int destinationFloor;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(OnPressed);
        }

        private void OnPressed()
        {
            if (targetElevator != null)
            {
                // This simulates a person inside the elevator pressing the floor button!
                targetElevator.AddInternalRequest(destinationFloor);
                Debug.Log($"Antigravity: Manual Button Pressed! Sending {targetElevator.gameObject.name} to Floor {destinationFloor}");
            }
            else
            {
                Debug.LogWarning("Antigravity: You forgot to assign the Target Elevator to this button in the Inspector!");
            }
        }
    }
}
