using GamePlay;
using UnityEngine;
using GridSystem.Core;
using GridSystem.Runtime;

namespace Gameplay
{
    public class StageManager : MonoBehaviour
    {
        public static StageManager Instance { get; private set; }

        [Header("Map Configuration")]
        public MapGridData mapData;
        [SerializeField] private MapSpawner mapSpawner;
        [SerializeField] private bool spawnMapOnStart = true;

        [Header("Game References")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private GameUI gameUI;

        [Header("Auto Setup")]
        [SerializeField] private bool autoCreateComponents = true;

        private bool isInitialized;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            ValidateReferences();
        }

        private void Start()
        {
            Initialize();
        }

        private void ValidateReferences()
        {
            if (mapData == null && autoCreateComponents)
            {
                mapData = ScriptableObject.CreateInstance<MapGridData>();
                mapData.GenerateDefaultMap();
            }

            if (mapSpawner == null)
            {
                mapSpawner = GetComponentInChildren<MapSpawner>();
                if (mapSpawner == null && autoCreateComponents)
                {
                    var spawnerObj = new GameObject("MapSpawner");
                    spawnerObj.transform.SetParent(transform);
                    mapSpawner = spawnerObj.AddComponent<MapSpawner>();
                }
            }

            if (mapSpawner != null)
            {
                mapSpawner.MapData = mapData;
            }

            if (gameManager == null)
            {
                gameManager = GetComponentInChildren<GameManager>();
                if (!gameManager)
                {
                    gameManager = FindFirstObjectByType<GameManager>();
                }
            }

            if (!gameUI)
            {
                gameUI = FindFirstObjectByType<GameUI>();
            }
        }

        public void Initialize()
        {
            if (isInitialized) return;

            if (spawnMapOnStart && mapSpawner != null)
            {
                mapSpawner.SpawnMap();
            }

            isInitialized = true;
        }

        public void ReloadMap()
        {
            if (mapSpawner)
            {
                mapSpawner.RefreshMap();
            }

        }

        public void SetMapData(MapGridData newMapData)
        {
            mapData = newMapData;

            if (mapSpawner)
            {
                mapSpawner.MapData = mapData;
            }

            ReloadMap();
        }

        public MapGridData GetMapData()
        {
            return mapData;
        }

        public MapSpawner GetMapSpawner()
        {
            return mapSpawner;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}