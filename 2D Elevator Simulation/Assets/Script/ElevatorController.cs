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
        [SerializeField] private float floorHeight = 2f;      // world units per floor
        [SerializeField] private float moveSpeed = 3f;        // world units/sec
        [SerializeField] private float doorOpenDuration = 1.5f;

        [Header("World space")]
        [SerializeField] private TMP_Text floorLabel;
        [SerializeField] private Transform carTransform;

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

        public bool IsIdle => requestQueue.Count == 0 && Doors == DoorState.Closed;

        private void Awake()
        {
            CurrentFloor = 0;
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

            if (isMoving)
                MoveTowardsTarget();
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
            if (IsIdle)
                return Mathf.Abs(CurrentFloor - requestedFloor);

            bool sameDirection = CurrentDirection == requestedDirection;
            bool isOnTheWay = sameDirection &&
                ((CurrentDirection == Direction.Up && requestedFloor >= CurrentFloor) ||
                 (CurrentDirection == Direction.Down && requestedFloor <= CurrentFloor));

            if (isOnTheWay)
                return Mathf.Abs(CurrentFloor - requestedFloor);

            // Not on the way: penalize so on-the-way elevators always win first,
            // but still comparable against other equally-bad elevators.
            return Mathf.Abs(CurrentFloor - requestedFloor) + 1000 + requestQueue.Count * 10;
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
        }

        private void SortQueueForCurrentDirection()
        {
            if (CurrentDirection == Direction.Up)
                requestQueue.Sort();                      // serve ascending floors first
            else if (CurrentDirection == Direction.Down)
                requestQueue.Sort((a, b) => b.CompareTo(a)); // serve descending floors first
        }

        private void MoveTowardsTarget()
        {
            int targetFloor = requestQueue[0];
            Vector3 pos = carTransform.position;
            Vector3 targetPos = new Vector3(pos.x, targetFloor * floorHeight, pos.z);

            carTransform.position = Vector3.MoveTowards(
                carTransform.position, targetPos, moveSpeed * Time.deltaTime);

            // Update "current floor" as we pass through it, for accurate display + dispatch scoring.
            int nearestFloor = Mathf.RoundToInt(carTransform.position.y / floorHeight);
            if (nearestFloor != CurrentFloor)
            {
                CurrentFloor = nearestFloor;
                UpdateLabel();
            }

            if (Vector3.Distance(carTransform.position, targetPos) < 0.05f)
            {
                carTransform.position = targetPos;
                CurrentFloor = targetFloor;
                UpdateLabel();
                requestQueue.RemoveAt(0);
                isMoving = false;
                OpenDoors();
            }
        }

        private void OpenDoors()
        {
            Doors = DoorState.Open;
            doorTimer = doorOpenDuration;
        }

        private void SnapToFloor(int floor)
        {
            Vector3 pos = carTransform.position;
            carTransform.position = new Vector3(pos.x, floor * floorHeight, pos.z);
        }

        private void UpdateLabel()
        {
            if (floorLabel != null)
                floorLabel.text = CurrentFloor.ToString();
        }
    }
}