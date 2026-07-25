using System.Collections.Generic;
using UnityEngine;

namespace ElevatorSim
{
    /// <summary>
    /// Central dispatcher. Owns no elevator state directly — just decides
    /// WHICH elevator gets a request, then hands it off. Also drives each
    /// CallButton's visual state (Idle/Waiting/Active) as the assigned
    /// elevator progresses toward that floor.
    /// </summary>
    public class ElevatorSystemManager : MonoBehaviour
    {
        public static ElevatorSystemManager Instance { get; private set; }

        [SerializeField] private List<ElevatorController> elevators;

        [Header("Floor positions")]
        [Tooltip("Drag one empty GameObject per floor, positioned exactly where you want elevators to stop. Index 0 = Ground.")]
        [SerializeField] private Transform[] floorMarkers;

        private readonly Dictionary<(int floor, Direction dir), CallButton> buttonRegistry = new();
        private readonly HashSet<(int floor, Direction dir)> pendingRequests = new();

        private void Awake()
        {
            Instance = this;
        }

        public int FloorCount => floorMarkers.Length;

        /// <summary>
        /// World Y position an elevator should stop at for a given floor index.
        /// </summary>
        public float GetFloorY(int floor)
        {
            if (floor < 0 || floor >= floorMarkers.Length)
            {
                Debug.LogError($"Floor {floor} has no marker assigned in ElevatorSystemManager.");
                return 0f;
            }
            return floorMarkers[floor].position.y;
        }

        /// <summary>
        /// Which floor index is closest to a given world Y — used by
        /// ElevatorController to know what floor it's currently passing.
        /// </summary>
        public int GetNearestFloorIndex(float worldY)
        {
            int nearest = 0;
            float bestDist = float.MaxValue;
            for (int i = 0; i < floorMarkers.Length; i++)
            {
                float dist = Mathf.Abs(floorMarkers[i].position.y - worldY);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    nearest = i;
                }
            }
            return nearest;
        }

        public void RegisterButton(int floor, Direction direction, CallButton button)
        {
            buttonRegistry[(floor, direction)] = button;
        }

        /// <summary>
        /// Entry point called by CallButton.
        /// </summary>
        public void RequestFloor(int floor, Direction direction)
        {
            var key = (floor, direction);
            if (pendingRequests.Contains(key))
                return; // already being handled

            ElevatorController best = FindBestElevator(floor, direction);
            if (best == null) return;

            pendingRequests.Add(key);
            best.AddRequest(floor);

            if (buttonRegistry.TryGetValue(key, out var button))
                button.SetState(CallButtonState.Waiting);

            StartCoroutine(TrackRequestLifecycle(best, floor, key));
        }

        private ElevatorController FindBestElevator(int floor, Direction direction)
        {
            ElevatorController best = null;
            int bestCost = int.MaxValue;

            foreach (var elevator in elevators)
            {
                int cost = elevator.GetEtaCost(floor, direction);
                if (cost < bestCost)
                {
                    bestCost = cost;
                    best = elevator;
                }
            }
            return best;
        }

        /// <summary>
        /// Waits for the assigned elevator to commit to this floor as its
        /// immediate next stop (button turns green), then waits for arrival
        /// (button resets to idle, request cleared, button re-enabled).
        /// </summary>
        private System.Collections.IEnumerator TrackRequestLifecycle(
            ElevatorController elevator, int floor, (int floor, Direction dir) key)
        {
            buttonRegistry.TryGetValue(key, out var button);

            // Red -> Green: wait until this floor becomes the elevator's next stop.
            while (elevator.CurrentTargetFloor != floor)
                yield return null;

            button?.SetState(CallButtonState.Active);

            // Green -> Idle: wait until the elevator actually arrives.
            // (CurrentFloor only updates to the target AFTER it's popped off
            // the queue, so this alone is a reliable arrival signal.)
            while (elevator.CurrentFloor != floor)
                yield return null;

            pendingRequests.Remove(key);
            button?.SetState(CallButtonState.Idle);
        }
    }
}