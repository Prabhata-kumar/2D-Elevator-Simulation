using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace ElevatorSim
{
    public enum Direction { Idle, Up, Down }
    public enum DoorState { Closed, Open }

    /// <summary>
    /// Controls a single elevator car: movement, its own request queue,
    /// and direction logic. Has no idea other elevators exist —
    /// dispatch decisions belong to ElevatorSystemManager.
    /// </summary>
    public class ElevatorController : MonoBehaviour
    {
        [Header("Identity")]
        public int elevatorId;

        [Header("Movement")]
        [SerializeField] private float floorHeight = 150f;   // px per floor, matches UI layout
        [SerializeField] private float moveSpeed = 200f;      // px/sec
        [SerializeField] private float doorOpenDuration = 1.5f;
        [SerializeField] private float minMoveDuration = 1.5f;

        [Header("Capacity")]
        [SerializeField] private int maxCapacity = 3;
        private int currentPassengers = 0;
        public Transform[] passengerSlots;
        [HideInInspector] public bool[] slotOccupied;

        [Header("UI")]
        [SerializeField] private TMP_Text floorLabel;
        [SerializeField] private Transform carTransform;

        public int MaxCapacity => maxCapacity;
        public int CurrentPassengers => currentPassengers;
        public Transform CarRect => carTransform;

        public int CurrentFloor { get; private set; }
        public Direction CurrentDirection { get; private set; } = Direction.Idle;
        public DoorState Doors { get; private set; } = DoorState.Closed;

        /// <summary>
        /// The floor this elevator is actively heading to right now (next stop),
        /// or null if idle. Used by the dispatcher to light up buttons green
        /// only once this elevator has actually committed to that floor next.
        /// </summary>
        public int? CurrentTargetFloor => requestQueue.Count > 0 ? requestQueue[0] : (int?)null;

        // Own queue - only this elevator reads/writes it.
        private readonly List<int> requestQueue = new List<int>();
        private float doorTimer;
        private bool isMoving;
        
        // Static cache to prevent .ToString() garbage allocation when crossing floors
        private static readonly string[] floorStringCache = new string[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10" };

        public bool IsIdle => requestQueue.Count == 0 && Doors == DoorState.Closed;

        private void Start()
        {
            CurrentFloor = 0;
            slotOccupied = new bool[maxCapacity];
            
            bool needsSlots = passengerSlots == null || passengerSlots.Length < maxCapacity;
            if (!needsSlots)
            {
                for (int i = 0; i < maxCapacity; i++)
                {
                    if (passengerSlots[i] == null) needsSlots = true;
                }
            }
            
            // Auto-generate the 3 standing slots so you don't have to create them manually!
            if (needsSlots)
            {
                passengerSlots = new Transform[maxCapacity];
                float spacing = 1.0f; // Distance between each NPC
                float startX = -((maxCapacity - 1) * spacing) / 2f;
                
                for (int i = 0; i < maxCapacity; i++)
                {
                    GameObject slotObj = new GameObject($"PassengerSlot_{i}");
                    if (carTransform != null)
                        slotObj.transform.SetParent(carTransform);
                    else
                        slotObj.transform.SetParent(this.transform);
                        
                    slotObj.transform.localPosition = new Vector3(startX + (i * spacing), 0, 0);
                    passengerSlots[i] = slotObj.transform;
                }
            }

            UpdateLabel();
            SnapToFloor(CurrentFloor);
        }

        private void Update()
        {
            if (Doors == DoorState.Open)
            {
                doorTimer -= Time.deltaTime;
                if (doorTimer <= 0f)
                {
                    Doors = DoorState.Closed;
                    ProceedToNextTarget();
                }
                return;
            }
        }

        /// <summary>
        /// Called by the dispatcher. Never called twice for a floor already queued.
        /// </summary>
        public void AddRequest(int floor)
        {
            if (floor == CurrentFloor && !isMoving && Doors == DoorState.Closed)
            {
                OpenDoors();
                return;
            }

            if (requestQueue.Contains(floor)) return;

            requestQueue.Add(floor);

            // First request sets our direction.
            if (CurrentDirection == Direction.Idle)
                CurrentDirection = floor > CurrentFloor ? Direction.Up : Direction.Down;

            SortQueueForCurrentDirection();

            if (!isMoving && Doors == DoorState.Closed)
                ProceedToNextTarget();
        }

        /// <summary>
        /// Cost estimate the dispatcher uses to compare elevators.
        /// Lower = better candidate for this request.
        /// </summary>
        public int GetEtaCost(int requestedFloor, Direction requestedDirection)
        {
            // If completely full, refuse new hallway pickups
            if (currentPassengers >= maxCapacity)
                return int.MaxValue;
                
            // If we are already exactly here with doors open, and going the right way (or empty), pick them up immediately!
            if (CurrentFloor == requestedFloor && Doors == DoorState.Open)
            {
                if (requestQueue.Count == 0 || CurrentDirection == requestedDirection)
                    return -1;
            }

            // Multiply distance by 10 so we can use single digits (queue count) as a tie-breaker
            if (requestQueue.Count == 0)
                return Mathf.Abs(CurrentFloor - requestedFloor) * 10;

            bool sameDirection = CurrentDirection == requestedDirection;
            bool isOnTheWay = sameDirection &&
                ((CurrentDirection == Direction.Up && requestedFloor >= CurrentFloor) ||
                 (CurrentDirection == Direction.Down && requestedFloor <= CurrentFloor));

            int baseCost = Mathf.Abs(CurrentFloor - requestedFloor) * 10;

            if (isOnTheWay)
            {
                // Adding the queue count forces the system to pick a less busy elevator if distances are tied!
                return baseCost + requestQueue.Count; 
            }

            // Not on the way: penalize heavily
            return baseCost + 1000 + requestQueue.Count;
        }

        // ---------- Passenger Logic ----------

        /// <summary>
        /// Called when a passenger presses a floor button INSIDE this elevator.
        /// </summary>
        public void AddInternalRequest(int floor)
        {
            if (requestQueue.Contains(floor)) return;
            
            requestQueue.Add(floor);

            if (CurrentDirection == Direction.Idle)
                CurrentDirection = floor > CurrentFloor ? Direction.Up : Direction.Down;

            SortQueueForCurrentDirection();

            if (!isMoving && Doors == DoorState.Closed)
                ProceedToNextTarget();
        }

        /// <summary>
        /// Hook these up to your passenger simulation or UI buttons.
        /// Returns the slot index assigned to the passenger.
        /// </summary>
        public int PassengerEntered()
        {
            if (currentPassengers < maxCapacity)
            {
                currentPassengers++;
                if (slotOccupied != null)
                {
                    for (int i = 0; i < slotOccupied.Length; i++)
                    {
                        if (!slotOccupied[i])
                        {
                            slotOccupied[i] = true;
                            return i;
                        }
                    }
                }
            }
            return -1;
        }

        public void PassengerExited(int slotIndex)
        {
            if (currentPassengers > 0)
                currentPassengers--;
            
            if (slotOccupied != null && slotIndex >= 0 && slotIndex < slotOccupied.Length)
                slotOccupied[slotIndex] = false;
        }

        // ---------- internals ----------

        private void ProceedToNextTarget()
        {
            if (requestQueue.Count == 0)
            {
                CurrentDirection = Direction.Idle;
                isMoving = false;
                return;
            }

            SortQueueForCurrentDirection();
            isMoving = true;

            int targetFloor = requestQueue[0];
            StartCoroutine(SmoothMoveRoutine(targetFloor));
        }

        private System.Collections.IEnumerator SmoothMoveRoutine(int targetFloor)
        {
            float targetY = ElevatorSystemManager.Instance.GetFloorY(targetFloor);
            Vector3 startPos = carTransform.position;
            float distance = Mathf.Abs(startPos.y - targetY);
            
            float duration = Mathf.Max(minMoveDuration, distance / moveSpeed); 
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                float easeT = t * t * (3f - 2f * t); 

                Vector3 newPos = startPos;
                newPos.y = Mathf.Lerp(startPos.y, targetY, easeT);
                carTransform.position = newPos;

                int nearestFloor = ElevatorSystemManager.Instance.GetNearestFloor(carTransform.position.y);
                if (nearestFloor != CurrentFloor)
                {
                    CurrentFloor = nearestFloor;
                    UpdateLabel();
                }

                yield return null;
            }

            carTransform.position = new Vector3(startPos.x, targetY, startPos.z);
            CurrentFloor = targetFloor;
            UpdateLabel();
            requestQueue.RemoveAt(0);
            isMoving = false;
            OpenDoors();
        }

        private void SortQueueForCurrentDirection()
        {
            if (CurrentDirection == Direction.Up)
                requestQueue.Sort();                      // serve ascending floors first
            else if (CurrentDirection == Direction.Down)
                requestQueue.Sort((a, b) => b.CompareTo(a)); // serve descending floors first
        }



        private void OpenDoors()
        {
            Doors = DoorState.Open;
            doorTimer = doorOpenDuration;
            ElevatorSystemManager.Instance.NotifyDoorsOpened(CurrentFloor, this);
        }

        public void KeepDoorsOpen()
        {
            if (Doors == DoorState.Open)
                doorTimer = doorOpenDuration;
        }

        private void SnapToFloor(int floor)
        {
            if (carTransform != null)
                carTransform.position = new Vector3(carTransform.position.x, ElevatorSystemManager.Instance.GetFloorY(floor), carTransform.position.z);
        }

        private void UpdateLabel()
        {
            if (floorLabel != null)
            {
                if (CurrentFloor >= 0 && CurrentFloor < floorStringCache.Length)
                    floorLabel.text = floorStringCache[CurrentFloor];
                else
                    floorLabel.text = CurrentFloor.ToString(); // Fallback for extremely tall buildings
            }
        }
    }
}
