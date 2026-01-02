using System.Collections.Generic;
using GridSystem.Core;
using UnityEngine;

namespace GridSystem.Runtime
{

    public class MapSpawner : MonoBehaviour
    {
        [Header("Map Data")]
        [SerializeField] private MapGridData mapData;

        [Header("Settings")]
        [SerializeField] private bool spawnOnStart = true;
        [SerializeField] private bool usePooling = true;
        [SerializeField] private bool visualizeGrid = false;
        [SerializeField] private bool generateDefaultMapIfEmpty = true;

        private Dictionary<CellType, MapPool> pools = new Dictionary<CellType, MapPool>();
        private List<GameObject> spawnedObjects = new List<GameObject>();
        private Transform objectsParent;

        public MapGridData MapData
        {
            get => mapData;
            set => mapData = value;
        }

        private void Awake()
        {
            if (mapData == null)
            {
                mapData = ScriptableObject.CreateInstance<MapGridData>();
            }
        }

        private void Start()
        {
            if (spawnOnStart)
            {
                SpawnMap();
            }
        }

        public void SpawnMap()
        {
            ClearMap();

            if (!mapData)
            {
                Debug.LogError("MapData is not assigned!");
                return;
            }

            if (!mapData.HasCells() && generateDefaultMapIfEmpty)
            {
                mapData.GenerateDefaultMap();
            }

            objectsParent = new GameObject("Map Objects").transform;
            objectsParent.SetParent(transform, false);
            objectsParent.localPosition = Vector3.zero;
            objectsParent.localRotation = Quaternion.identity;
            objectsParent.localScale = Vector3.one;

            InitializePools();
            SpawnAllCells();
        }

        private void InitializePools()
        {
            if (!usePooling || mapData.cellDatabase == null) return;

            foreach (var cellData in mapData.cellDatabase.cells)
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

        private void SpawnAllCells()
        {
            for (int y = 0; y < mapData.height; y++)
            {
                for (int x = 0; x < mapData.width; x++)
                {
                    var cellType = mapData.GetCell(x, y);
                    if (cellType == CellType.Empty) continue;

                    SpawnCell(x, y, cellType);
                }
            }
        }

        private void SpawnCell(int x, int y, CellType cellType)
        {
            if (mapData.cellDatabase == null) return;

            var cellData = mapData.cellDatabase.GetCellData(cellType);
            if (cellData == null || cellData.prefab == null) return;

            var basePos = mapData.GridToWorld(x, y);
            Vector3 worldPos = transform.TransformPoint(basePos);
            SpawnAt(cellType, worldPos, transform.rotation);
        }

        private void SpawnAt(CellType cellType, Vector3 worldPos, Quaternion rotation)
        {
            GameObject obj;

            if (usePooling && pools.TryGetValue(cellType, out var pool))
            {
                obj = pool.Get(worldPos, rotation);
            }
            else
            {
                if (mapData.cellDatabase == null) return;
                var cellData = mapData.cellDatabase.GetCellData(cellType);
                if (cellData?.prefab == null) return;

                obj = Instantiate(cellData.prefab, worldPos, rotation, objectsParent);
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
                objectsParent = null;
            }
        }

        public void RefreshMap()
        {
            SpawnMap();
        }

        private void OnDrawGizmos()
        {
            if (!visualizeGrid || !mapData) return;

            DrawGridLines();
            DrawCells();
        }

        private void DrawGridLines()
        {
            Gizmos.color = Color.gray;

            for (int y = 0; y <= mapData.height; y++)
            {
                var start = transform.TransformPoint(mapData.GridToWorld(0, y));
                var end = transform.TransformPoint(mapData.GridToWorld(mapData.width, y));
                Gizmos.DrawLine(start, end);
            }

            for (int x = 0; x <= mapData.width; x++)
            {
                var start = transform.TransformPoint(mapData.GridToWorld(x, 0));
                var end = transform.TransformPoint(mapData.GridToWorld(x, mapData.height));
                Gizmos.DrawLine(start, end);
            }
        }

        private void DrawCells()
        {
            if (mapData.cellDatabase == null) return;

            float cellSize = mapData.cellSize;
            Vector3 cellOffset = new Vector3(cellSize * 0.5f, 0.05f, cellSize * 0.5f);

            for (int y = 0; y < mapData.height; y++)
            {
                for (int x = 0; x < mapData.width; x++)
                {
                    var cellType = mapData.GetCell(x, y);
                    if (cellType == CellType.Empty) continue;

                    var cellData = mapData.cellDatabase.GetCellData(cellType);
                    if (cellData == null) continue;

                    Gizmos.color = cellData.editorColor;
                    var worldPos = transform.TransformPoint(mapData.GridToWorld(x, y));
                    Gizmos.DrawCube(worldPos + Vector3.up * 0.05f, new Vector3(cellSize * 0.9f, 0.1f, cellSize * 0.9f));
                }
            }
        }
    }
}