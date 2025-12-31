using System.Collections.Generic;
using GridSystem.Core;
using UnityEngine;

namespace DropSystem
{
    public class EggSpawner : MonoBehaviour
    {
        public MapGridData mapData;
        public GameObject eggPrefab;
        public int maxEggs = 5;
        public float spawnInterval = 3f;

        private List<GameObject> activeEggs = new List<GameObject>();
        private List<Vector2Int> spawnPoints;

        void Start()
        {
            spawnPoints = mapData.GetCellsOfType(CellType.EggSpawnPoint);
            if (spawnInterval == 0)
            {
                spawnPoints = mapData.GetWalkableCells();
            }

            InvokeRepeating(nameof(SpawnEgg), 1f, spawnInterval);
        }

        void SpawnEgg()
        {
            if (activeEggs.Count >= maxEggs) return;
            if (spawnPoints.Count == 0) return;

            var spawnPos = spawnPoints[Random.Range(0, spawnPoints.Count)];
            var worldPos = mapData.GridToWorld(spawnPos.x, spawnPos.y);

            var egg = Instantiate(eggPrefab, worldPos, Quaternion.identity);
            activeEggs.Add(egg);
        }

        public void OnEggCollected(GameObject egg)
        {
            activeEggs.Remove(egg);
            Destroy(egg);
        }
    }
}