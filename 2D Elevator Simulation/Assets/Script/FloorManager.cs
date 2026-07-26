using UnityEngine;

namespace ElevatorSim
{
    [System.Serializable]
    public class FloorData
    {
        public Transform spawnPoint;
        public Transform waitPoint;
        public Transform exitPoint;
    }

    public class FloorManager : MonoBehaviour
    {
        public static FloorManager Instance { get; private set; }

        public FloorData[] floors;

        private void Awake()
        {
            Instance = this;
        }

        public Transform GetSpawnPoint(int floorIndex)
        {
            if (floorIndex >= 0 && floorIndex < floors.Length)
                return floors[floorIndex].spawnPoint;
            return null;
        }

        public Transform GetWaitPoint(int floorIndex)
        {
            if (floorIndex >= 0 && floorIndex < floors.Length)
                return floors[floorIndex].waitPoint;
            return null;
        }

        public Transform GetExitPoint(int floorIndex)
        {
            if (floorIndex >= 0 && floorIndex < floors.Length)
                return floors[floorIndex].exitPoint;
            return null;
        }
    }
}
