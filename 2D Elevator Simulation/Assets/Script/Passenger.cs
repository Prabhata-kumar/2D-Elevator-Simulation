using System.Collections;
using UnityEngine;
using TMPro;

namespace ElevatorSim
{
    public class Passenger : MonoBehaviour
    {
        public enum PassengerState { Spawning, WalkingToWait, Waiting, Boarding, Riding, Exiting }

        [Header("Patience Settings")]
        [SerializeField] private float orangeThreshold = 30f;
        [SerializeField] private float redThreshold = 45f;
        
        [Header("Visuals")]
        [SerializeField] private SpriteRenderer spriteRenderer; 
        [SerializeField] private TMP_Text floorLabel; // Displays the destination floor
        [SerializeField] private Color greenColor = Color.green;
        [SerializeField] private Color orangeColor = new Color(1f, 0.5f, 0f); // Orange
        [SerializeField] private Color redColor = Color.red;
        [SerializeField] private float walkSpeed = 100f;

        [Header("Debug")]
        [SerializeField] private PassengerState state = PassengerState.Spawning;
        public PassengerState State => state;

        [SerializeField] private int currentFloor;
        [SerializeField] private int targetFloor;
        
        public int CurrentFloor => currentFloor;
        public int TargetFloor => targetFloor;
        
        private float waitTimer = 0f;
        private ElevatorController boardedElevator;
        private int myElevatorSlot = -1;

#if UNITY_EDITOR
        private void OnValidate()
        {
            // This will automatically run in the Unity Editor and fix the Prefab for the user!
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null || this.gameObject == null) return;
                
                Transform bubble = transform.Find("though bobbule_0");
                if (bubble != null)
                {
                    Canvas badCanvas = bubble.GetComponentInChildren<Canvas>();
                    if (badCanvas != null)
                    {
                        DestroyImmediate(badCanvas.gameObject, true);
                        Debug.Log("Antigravity: Automatically cleaned up the UI Canvas from the Passenger Prefab!");
                    }
                }
            };
        }
#endif
        private void OnEnable()
        {
            if (ElevatorSystemManager.Instance != null)
                ElevatorSystemManager.Instance.OnElevatorDoorsOpened += HandleElevatorDoorsOpened;
        }

        private void OnDisable()
        {
            if (ElevatorSystemManager.Instance != null)
                ElevatorSystemManager.Instance.OnElevatorDoorsOpened -= HandleElevatorDoorsOpened;
        }

        public void Initialize(int startFloor, int destination)
        {
            currentFloor = startFloor;
            targetFloor = destination;
            
            if (spriteRenderer != null)
                spriteRenderer.color = greenColor;

            // Auto-generate the text label if you didn't set it up manually!
            if (floorLabel == null)
            {
                // Find your specific thought bubble object!
                Transform bubble = transform.Find("though bobbule_0");
                GameObject textObj;

                if (bubble != null)
                {
                    textObj = bubble.gameObject;
                }
                else
                {
                    textObj = new GameObject("AutoFloorLabel");
                    textObj.transform.SetParent(this.transform);
                    textObj.transform.localPosition = new Vector3(0, 1.5f, 0); // Fallback
                }
                
                var tmp = textObj.GetComponent<TextMeshPro>();
                if (tmp == null)
                    tmp = textObj.AddComponent<TextMeshPro>();
                    
                tmp.fontSize = 5;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.black; // Black text usually looks better inside a white thought bubble!
                tmp.sortingOrder = 15; // Ensure it renders on top
                
                floorLabel = tmp;
            }

            if (floorLabel != null)
                floorLabel.text = targetFloor.ToString();

            // Start by walking to the wait point
            Transform waitPoint = FloorManager.Instance.GetWaitPoint(currentFloor);
            if (waitPoint != null)
            {
                StartCoroutine(WalkToPoint(waitPoint.position, PassengerState.Waiting, () => 
                {
                    // Arrived at wait point, check if an elevator is ALREADY here!
                    if (!CheckAndBoardAvailableElevator())
                    {
                        // Otherwise, press the button and wait for the event
                        Direction dir = targetFloor > currentFloor ? Direction.Up : Direction.Down;
                        ElevatorSystemManager.Instance.RequestFloor(currentFloor, dir);
                    }
                }));
            }
            else
            {
                // Fallback if no points configured
                state = PassengerState.Waiting;
                if (!CheckAndBoardAvailableElevator())
                {
                    Direction dir = targetFloor > currentFloor ? Direction.Up : Direction.Down;
                    ElevatorSystemManager.Instance.RequestFloor(currentFloor, dir);
                }
            }
        }

        private bool CheckAndBoardAvailableElevator()
        {
            foreach (var elevator in ElevatorSystemManager.Instance.Elevators)
            {
                if (elevator != null && elevator.gameObject.activeInHierarchy && 
                    elevator.CurrentFloor == currentFloor && elevator.Doors == DoorState.Open &&
                    elevator.CurrentPassengers < elevator.MaxCapacity)
                {
                    BoardElevator(elevator);
                    return true;
                }
            }
            return false;
        }

        private void HandleElevatorDoorsOpened(int floor, ElevatorController elevator)
        {
            if (state == PassengerState.Waiting && currentFloor == floor)
            {
                if (elevator.CurrentPassengers < elevator.MaxCapacity)
                {
                    BoardElevator(elevator);
                }
            }
        }

        private void Update()
        {
            if (state == PassengerState.Waiting)
            {
                // Patience logic
                waitTimer += Time.deltaTime;
                if (spriteRenderer != null)
                {
                    if (waitTimer >= redThreshold)
                        spriteRenderer.color = redColor;
                    else if (waitTimer >= orangeThreshold)
                        spriteRenderer.color = orangeColor;
                }

                if (waitTimer >= redThreshold)
                {
                    // Give up and leave
                    Transform exitPoint = FloorManager.Instance.GetExitPoint(currentFloor);
                    if (exitPoint != null)
                    {
                        StartCoroutine(WalkToPoint(exitPoint.position, PassengerState.Exiting, Despawn));
                    }
                    else
                    {
                        Despawn();
                    }
                    return; // Stop checking for elevators
                }
            }
            else if (state == PassengerState.Riding)
            {
                // Follow the slot perfectly
                if (boardedElevator != null && myElevatorSlot != -1)
                {
                    transform.position = boardedElevator.passengerSlots[myElevatorSlot].position;
                }

                // Wait to arrive at destination
                if (boardedElevator.CurrentFloor == targetFloor && boardedElevator.Doors == DoorState.Open)
                {
                    ExitElevator();
                }
            }
        }

        private void BoardElevator(ElevatorController elevator)
        {
            state = PassengerState.Boarding; // Pause patience timer
            boardedElevator = elevator;
            
            myElevatorSlot = elevator.PassengerEntered(); // returns slot index!
            elevator.AddInternalRequest(targetFloor);

            if (myElevatorSlot != -1 && elevator.passengerSlots != null && myElevatorSlot < elevator.passengerSlots.Length)
            {
                Transform slot = elevator.passengerSlots[myElevatorSlot];
                StartCoroutine(WalkToPoint(slot.position, PassengerState.Riding, null));
            }
            else
            {
                // Fallback
                state = PassengerState.Riding;
                transform.SetParent(elevator.CarRect); 
            }
        }

        private void ExitElevator()
        {
            boardedElevator.PassengerExited(myElevatorSlot);
            
            Transform exitPoint = FloorManager.Instance.GetExitPoint(targetFloor);
            if (exitPoint != null)
            {
                StartCoroutine(WalkToPoint(exitPoint.position, PassengerState.Exiting, Despawn));
            }
            else
            {
                Despawn();
            }
        }

        private void Despawn()
        {
            if (PassengerSpawner.Instance != null)
                PassengerSpawner.Instance.ReturnPassenger(this);
            else
                Destroy(gameObject);
        }

        private IEnumerator WalkToPoint(Vector3 targetPos, PassengerState nextState, System.Action onComplete)
        {
            state = PassengerState.WalkingToWait; // Generic walking state
            if (nextState == PassengerState.Riding) state = PassengerState.Boarding;
            if (nextState == PassengerState.Exiting) state = PassengerState.Exiting;

            Vector3 velocity = Vector3.zero;
            float smoothTime = 0.3f;

            while (Vector3.Distance(transform.position, targetPos) > 0.05f)
            {
                // Update target if we are boarding a moving slot
                if (state == PassengerState.Boarding && boardedElevator != null)
                {
                    boardedElevator.KeepDoorsOpen(); // Ensure elevator waits for us to walk inside!
                    if (myElevatorSlot != -1 && boardedElevator.passengerSlots != null && myElevatorSlot < boardedElevator.passengerSlots.Length)
                        targetPos = boardedElevator.passengerSlots[myElevatorSlot].position;
                }

                // Smooth, easing movement instead of a robotic constant speed
                transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, smoothTime, walkSpeed);
                
                yield return null;
            }

            transform.position = targetPos;
            state = nextState;
            onComplete?.Invoke();
        }
    }
}
