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
        public List<ElevatorController> Elevators => elevators;

        [Header("Floor Markers")]
        [SerializeField] private Transform[] floorMarkers;
        [SerializeField] private float defaultFloorHeight = 150f;

        public event System.Action<int, ElevatorController> OnElevatorDoorsOpened;

        public void NotifyDoorsOpened(int floor, ElevatorController elevator)
        {
            OnElevatorDoorsOpened?.Invoke(floor, elevator);
        }

        public float GetFloorY(int floor)
        {
            if (floorMarkers != null && floor >= 0 && floor < floorMarkers.Length && floorMarkers[floor] != null)
                return floorMarkers[floor].position.y;
            return floor * defaultFloorHeight; 
        }

        public int GetNearestFloor(float currentY)
        {
            if (floorMarkers == null || floorMarkers.Length == 0)
                return Mathf.RoundToInt(currentY / defaultFloorHeight);
            
            int nearest = 0;
            float minD = float.MaxValue;
            for (int i = 0; i < floorMarkers.Length; i++) 
            {
                if (floorMarkers[i] == null) continue;
                float d = Mathf.Abs(floorMarkers[i].position.y - currentY);
                if (d < minD) 
                {
                    minD = d;
                    nearest = i;
                }
            }
            return nearest;
        }

        private readonly Dictionary<(int floor, Direction dir), CallButton> buttonRegistry = new();
        private readonly HashSet<(int floor, Direction dir)> pendingRequests = new();
        
        private WaitForSeconds assignWait = new WaitForSeconds(0.5f);

        private void Awake()
        {
            Instance = this;
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

            pendingRequests.Add(key);

            if (buttonRegistry.TryGetValue(key, out var btn))
                btn.SetState(CallButtonState.Waiting);

            StartCoroutine(TryAssignRequest(floor, direction, key));
        }

        private System.Collections.IEnumerator TryAssignRequest(int floor, Direction direction, (int floor, Direction dir) key)
        {
            ElevatorController best = null;
            
            // Keep trying until we find an elevator that has room!
            while (best == null)
            {
                best = FindBestElevator(floor, direction);
                if (best == null)
                    yield return assignWait;
            }

            // FIX: Check if the elevator is already perfectly idle on this floor!
            if (best.CurrentFloor == floor && best.IsIdle)
            {
                best.AddRequest(floor); // Triggers doors to open
                
                // Immediately reset the button
                if (buttonRegistry.TryGetValue(key, out var button))
                    button.SetState(CallButtonState.Idle);
                
                pendingRequests.Remove(key);
                yield break; 
            }

            best.AddRequest(floor);

            // Red -> Green: wait until this floor becomes the elevator's next stop.
            while (best != null && best.gameObject.activeInHierarchy && best.CurrentTargetFloor != floor)
                yield return null;

            // If the elevator was deactivated while we were waiting, abort safely!
            if (best == null || !best.gameObject.activeInHierarchy)
            {
                pendingRequests.Remove(key);
                if (buttonRegistry.TryGetValue(key, out var btn)) btn.SetState(CallButtonState.Idle);
                yield break;
            }

            if (buttonRegistry.TryGetValue(key, out var activeBtn))
                activeBtn.SetState(CallButtonState.Active);

            // Green -> Idle: wait until the elevator actually arrives.
            while (best != null && best.gameObject.activeInHierarchy && best.CurrentFloor != floor)
                yield return null;

            pendingRequests.Remove(key);
            if (buttonRegistry.TryGetValue(key, out var idleBtn))
                idleBtn.SetState(CallButtonState.Idle);
        }

        private ElevatorController FindBestElevator(int floor, Direction dir)
        {
            ElevatorController bestElevator = null;
            int bestCost = int.MaxValue;

            foreach (var elevator in elevators)
            {
                // Ignore elevators that are disabled in the Hierarchy!
                if (!elevator.gameObject.activeInHierarchy) 
                    continue;

                int cost = elevator.GetEtaCost(floor, dir);
                if (cost < bestCost)
                {
                    bestCost = cost;
                    bestElevator = elevator;
                }
            }

            return bestElevator;
        }
    }
}
