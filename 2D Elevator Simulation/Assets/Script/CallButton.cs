using UnityEngine;
using UnityEngine.UI;

namespace ElevatorSim
{
    public enum CallButtonState { Idle, Waiting, Active }

    /// <summary>
    /// Attach to a floor call button. Set floorIndex + direction in the
    /// Inspector per button (Ground floor only needs "Up", top floor only "Down").
    ///
    /// Visual states:
    ///   Idle    - indicator hidden, button clickable
    ///   Waiting - indicator visible RED, request sent but elevator hasn't
    ///             committed to this floor as its next stop yet, button locked
    ///   Active  - indicator visible GREEN, an elevator is now heading here next
    /// Returns to Idle automatically once the elevator arrives.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class CallButton : MonoBehaviour
    {
        [Header("Request")]
        [SerializeField] private int floorIndex;
        [SerializeField] private Direction direction = Direction.Up;

        [Header("Visual feedback")]
        [SerializeField] private Image indicatorImage;   // child Image on the button
        [SerializeField] private Color waitingColor = Color.red;
        [SerializeField] private Color activeColor = Color.green;

        private Button button;
        public int FloorIndex => floorIndex;
        public Direction RequestDirection => direction;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(OnPressed);
            SetState(CallButtonState.Idle);
        }

        private void Start()
        {
            // Registered in Start (not Awake) so ElevatorSystemManager.Instance
            // is guaranteed to exist first, regardless of script execution order.
            ElevatorSystemManager.Instance.RegisterButton(floorIndex, direction, this);
        }

        private void OnPressed()
        {
            ElevatorSystemManager.Instance.RequestFloor(floorIndex, direction);
        }

        /// <summary>
        /// Called only by ElevatorSystemManager as the request progresses.
        /// </summary>
        public void SetState(CallButtonState state)
        {
            switch (state)
            {
                case CallButtonState.Idle:
                    if (indicatorImage != null) indicatorImage.enabled = false;
                    button.interactable = true;
                    break;

                case CallButtonState.Waiting:
                    if (indicatorImage != null)
                    {
                        indicatorImage.enabled = true;
                        indicatorImage.color = waitingColor;
                    }
                    button.interactable = false;
                    break;

                case CallButtonState.Active:
                    if (indicatorImage != null)
                    {
                        indicatorImage.enabled = true;
                        indicatorImage.color = activeColor;
                    }
                    button.interactable = false;
                    break;
            }
        }
    }
}
