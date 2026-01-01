using System.Collections.Generic;
using GamePlay;
using GridSystem.Core;
using UnityEngine;

namespace GridSystem.Runtime
{
    public class MapSpawner : MonoBehaviour
    {
        public MapGridData MapData => StageManager.Instance.mapData;

        [Header("Settings")] public bool spawnOnStart = true;
        public bool usePooling = true;
        public bool visualizeGrid = false;


        private Dictionary<CellType, MapPool> pools = new();
        private List<GameObject> spawnedObjects = new List<GameObject>();
        private Transform objectsParent;

        void Start()
        {
            if (spawnOnStart)
            {
                SpawnMap();
            }
        }

        public void SpawnMap()
        {
            ClearMap();

            if (!MapData || !MapData.cellDatabase)
            {
                Debug.LogError("MapData or CellDatabase is not assigned!");
                return;
            }

            objectsParent = new GameObject("Map Objects").transform;

            objectsParent.SetParent(transform, false);

            objectsParent.localPosition = Vector3.zero;
            objectsParent.localRotation = Quaternion.identity;
            objectsParent.localScale = Vector3.one;

            InitializePools();

            for (int y = 0; y < MapData.height; y++)
            {
                for (int x = 0; x < MapData.width; x++)
                {
                    var cellType = MapData.GetCell(x, y);
                    if (cellType == CellType.Empty) continue;

                    SpawnCell(x, y, cellType);
                }
            }
        }

        private void InitializePools()
        {
            if (!usePooling) return;

            foreach (var cellData in MapData.cellDatabase.cells)
            {
                if (cellData.prefab && !pools.ContainsKey(cellData.cellType))
                {
                    pools[cellData.cellType] = new MapPool(
                        cellData.prefab,
                        objectsParent,
                        10
                    );
                }
            }
        }

        private void SpawnCell(int x, int y, CellType cellType)
        {
            var cellData = MapData.cellDatabase.GetCellData(cellType);
            if (cellData == null || !cellData.prefab) return;

            var basePos = MapData.GridToWorld(x, y);

            Vector3 worldPos = transform.TransformPoint(basePos);
            SpawnAt(cellType, worldPos, transform.rotation);
        }

        private void SpawnAt(CellType cellType, Vector3 worldPos, Quaternion rotation)
        {
            GameObject obj;

            if (usePooling && pools.ContainsKey(cellType))
            {
                obj = pools[cellType].Get(worldPos, rotation);
            }
            else
            {
                var prefab = MapData.cellDatabase.GetCellData(cellType).prefab;
                obj = Instantiate(prefab, worldPos, rotation, objectsParent);
            }

            spawnedObjects.Add(obj);

        }

        public void ClearMap()
        {
            if (usePooling)
            {
                foreach (var pool in pools.Values)
                {
                    pool.ReturnAll();
                }
            }
            else
            {
                foreach (var obj in spawnedObjects)
                {
                    if (obj) Destroy(obj);
                }
            }

            spawnedObjects.Clear();

            if (objectsParent != null)
            {
                Destroy(objectsParent.gameObject);
            }
        }

        void OnDrawGizmos()
        {
            if (!visualizeGrid || !MapData) return;

            Gizmos.color = Color.gray;

            for (int y = 0; y <= MapData.height; y++)
            {
                var start = transform.TransformPoint(MapData.GridToWorld(0, y));
                var end = transform.TransformPoint(MapData.GridToWorld(MapData.width, y));
                Gizmos.DrawLine(start, end);
            }

            for (int x = 0; x <= MapData.width; x++)
            {
                var start = transform.TransformPoint(MapData.GridToWorld(x, 0));
                var end = transform.TransformPoint(MapData.GridToWorld(x, MapData.height));
                Gizmos.DrawLine(start, end);
            }
        }
    }
}
