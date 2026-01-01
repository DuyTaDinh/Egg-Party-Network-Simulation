using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core;
using GamePlay;
using GridSystem.Core;
using Player;
using UnityEngine;

namespace DropSystem
{
    public class EggManager : Singleton<EggManager>
    {

        [Header("Egg Configuration")] public List<EggData> eggDatabase = new List<EggData>();

        [Header("Spawn Settings")] public int maxEggsOnMap = 5;
        public float spawnInterval = 3f;
        public float firstSpawnDelay = 1f;

        public MapGridData MapData => StageManager.Instance.mapData;


        public event Action<Egg, PlayerController> OnEggCollectedEvent;
        public event Action<Egg> OnEggSpawnedEvent;

        private Dictionary<int, Egg> activeEggs = new Dictionary<int, Egg>();
        private List<Vector2Int> availableSpawnPoints = new List<Vector2Int>();
        private int nextEggId = 0;
        private Coroutine spawnCoroutine;
        private Transform eggsContainer;


        void Start()
        {
            if (!eggsContainer)
            {
                eggsContainer = new GameObject("Eggs Container").transform;
                eggsContainer.SetParent(transform);
            }

            InitializeSpawnPoints();
        }

        void InitializeSpawnPoints()
        {
            if (!MapData) return;

            availableSpawnPoints = MapData.GetCellsOfType(CellType.EggSpawnPoint);

            if (availableSpawnPoints.Count == 0)
            {
                availableSpawnPoints = MapData.GetWalkableCells();
            }
        }

        public void StartSpawning()
        {
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
            }

            spawnCoroutine = StartCoroutine(SpawnLoop());
        }

        public void StopSpawning()
        {
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }
        }

        IEnumerator SpawnLoop()
        {
            yield return new WaitForSeconds(firstSpawnDelay);

            while (true)
            {
                if (activeEggs.Count < maxEggsOnMap)
                {
                    SpawnEgg();
                }

                yield return new WaitForSeconds(spawnInterval);
            }
        }

        void SpawnEgg()
        {
            if (availableSpawnPoints.Count == 0) return;

            Vector2Int spawnPos = GetRandomAvailableSpawnPoint();
            if (spawnPos.x < 0) return;

            EggData eggData = SelectRandomEggType();
            if (eggData == null || !eggData.prefab) return;

            Vector3 worldPos = MapData.GridToWorld(spawnPos.x, spawnPos.y);
            GameObject eggObj = Instantiate(eggData.prefab, worldPos, Quaternion.identity, eggsContainer);

            Egg egg = eggObj.GetComponent<Egg>();
            if (egg)
            {
                egg.Initialize(nextEggId++, spawnPos, eggData);
                activeEggs[egg.eggId] = egg;
                OnEggSpawnedEvent?.Invoke(egg);
            }
        }

        Vector2Int GetRandomAvailableSpawnPoint()
        {
            List<Vector2Int> validPoints = new List<Vector2Int>();

            foreach (var point in availableSpawnPoints)
            {
                bool occupied = false;
                foreach (var egg in activeEggs.Values)
                {
                    if (egg && egg.GridPosition == point)
                    {
                        occupied = true;
                        break;
                    }
                }

                if (!occupied)
                {
                    validPoints.Add(point);
                }
            }

            if (validPoints.Count == 0)
                return new Vector2Int(-1, -1);

            return validPoints[UnityEngine.Random.Range(0, validPoints.Count)];
        }

        EggData SelectRandomEggType()
        {
            if (eggDatabase.Count == 0) return null;

            float totalWeight = eggDatabase.Sum(e => e.spawnWeight);
            float randomValue = UnityEngine.Random.Range(0f, totalWeight);

            float currentWeight = 0f;
            foreach (var eggData in eggDatabase)
            {
                currentWeight += eggData.spawnWeight;
                if (randomValue <= currentWeight)
                {
                    return eggData;
                }
            }

            return eggDatabase[0];
        }

        public void OnEggCollected(Egg egg, PlayerController collector)
        {
            activeEggs.Remove(egg.eggId);

            OnEggCollectedEvent?.Invoke(egg, collector);
        }

        public List<Egg> GetAllActiveEggs()
        {
            return activeEggs.Values.Where(e => e != null && !e.IsCollected).ToList();
        }

        public Egg GetNearestEgg(Vector2Int fromPosition)
        {
            Egg nearestEgg = null;
            float shortestDistance = float.MaxValue;

            foreach (var egg in activeEggs.Values)
            {
                if (!egg || egg.IsCollected) continue;

                float distance = Vector2Int.Distance(fromPosition, egg.GridPosition);
                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    nearestEgg = egg;
                }
            }

            return nearestEgg;
        }

        public void ClearAllEggs()
        {
            foreach (var egg in activeEggs.Values)
            {
                if (egg)
                {
                    Destroy(egg.gameObject);
                }
            }

            activeEggs.Clear();
        }

    }
}