using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ElevatorSim
{
    public class PassengerSpawner : MonoBehaviour
    {
        public static PassengerSpawner Instance { get; private set; }

        [Header("Setup")]
        public GameObject passengerPrefab;
        public bool autoSpawnEnabled = true;
        
        [Header("Spawn Timing")]
        [Tooltip("Use the slider to make them spawn faster or slower")]
        [Range(0.1f, 10f)] public float spawnInterval = 2f;
        public bool useRandomInterval = true;
        [Range(0.1f, 5f)] public float minSpawnInterval = 0.5f;
        [Range(1f, 10f)] public float maxSpawnInterval = 2.5f;

        [Header("Settings")]
        public int maxFloors = 4;

        [Header("Stress Test")]
        [Tooltip("Spawns a massive burst of passengers to test the pool!")]
        public int burstSpawnCount = 20;
        public bool triggerBurstNow = false;

        // Object Pool
        private Queue<Passenger> pool = new Queue<Passenger>();
        private WaitForSeconds burstWait = new WaitForSeconds(0.1f);

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            // Pre-warm the pool with 20 passengers so it's super fast!
            for (int i = 0; i < 20; i++)
            {
                GameObject pObj = Instantiate(passengerPrefab);
                Passenger p = pObj.GetComponent<Passenger>();
                pObj.SetActive(false);
                pool.Enqueue(p);
            }

            StartCoroutine(SpawnRoutine());
        }

        private void Update()
        {
            if (triggerBurstNow)
            {
                triggerBurstNow = false;
                StartCoroutine(BurstSpawnRoutine());
            }
        }

        // Added this so you can drag the PassengerSpawner into the Unity Button OnClick event!
        public void ToggleAutoSpawn()
        {
            autoSpawnEnabled = !autoSpawnEnabled;
            Debug.Log("Antigravity: Spawner is now " + (autoSpawnEnabled ? "AUTO" : "MANUAL"));
        }

        public Passenger GetPassenger(Vector3 position)
        {
            if (pool.Count > 0)
            {
                Passenger p = pool.Dequeue();
                p.transform.position = position;
                p.gameObject.SetActive(true);
                return p;
            }
            GameObject pObj = Instantiate(passengerPrefab, position, Quaternion.identity);
            return pObj.GetComponent<Passenger>();
        }

        public void ReturnPassenger(Passenger p)
        {
            p.gameObject.SetActive(false);
            p.transform.SetParent(null); // Detach from any elevators
            pool.Enqueue(p);
        }

        private IEnumerator BurstSpawnRoutine()
        {
            for (int i = 0; i < burstSpawnCount; i++)
            {
                SpawnSinglePassenger();
                yield return burstWait; // stagger them slightly without garbage collection!
            }
        }

        private IEnumerator SpawnRoutine()
        {
            while (true)
            {
                float waitTime = useRandomInterval ? Random.Range(minSpawnInterval, maxSpawnInterval) : spawnInterval;
                yield return new WaitForSeconds(waitTime);

                if (autoSpawnEnabled)
                {
                    SpawnSinglePassenger();
                }
            }
        }

        private void SpawnSinglePassenger()
        {
            if (FloorManager.Instance != null && FloorManager.Instance.floors.Length == maxFloors)
            {
                int startFloor = Random.Range(0, maxFloors);
                int destFloor = Random.Range(0, maxFloors);
                while (destFloor == startFloor)
                    destFloor = Random.Range(0, maxFloors);

                Transform spawnPoint = FloorManager.Instance.GetSpawnPoint(startFloor);
                if (spawnPoint != null)
                {
                    Passenger passenger = GetPassenger(spawnPoint.position);
                    passenger.Initialize(startFloor, destFloor);
                }
            }
        }
    }
}
