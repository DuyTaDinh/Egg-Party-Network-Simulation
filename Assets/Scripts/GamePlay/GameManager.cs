using System;
using System.Collections.Generic;
using Gameplay;
using GamePlay.AI;
using Gameplay.Egg;
using Gameplay.Player;
using GamePlay.UserInput;
using GridSystem.Core;
using GridSystem.Runtime;
using Network;
using Network.Client;
using Network.Messages;
using UnityEngine;

namespace GamePlay
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game Settings")]
        [SerializeField] private int playerCount = 4;
        [SerializeField] private float gameDuration = 60f;
        [SerializeField] private bool autoStart = true;

        [Header("References")]
        [SerializeField] private MapGridData mapData;
        [SerializeField] private Transform playerContainer;
        [SerializeField] private Transform eggContainer;
        [SerializeField] private GameUI gameUI;

        [Header("Prefabs")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject eggPrefab;

        private NetworkManager networkManager;
        private InputHandler inputHandler;
        private AIManager aiManager;

        private readonly Dictionary<int, PlayerView> playerViews = new Dictionary<int, PlayerView>();
        private readonly Dictionary<int, EggView> eggViews = new Dictionary<int, EggView>();

        private bool isGameRunning;
        private int localPlayerId;

        public event Action OnGameStarted;
        public event Action OnGameEnded;
        public event Action<int, int> OnScoreChanged;
        public event Action<float> OnTimeChanged;

        public bool IsGameRunning => isGameRunning;
        public int LocalPlayerId => localPlayerId;
        public MapGridData MapData => mapData;

        private void Awake()
        {
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            ValidateReferences();
            InitializeSystems();
        }

        private void ValidateReferences()
        {
            if (!mapData && StageManager.Instance)
            {
                mapData = StageManager.Instance.mapData;
            }

            if (mapData == null)
            {
                var spawner = FindFirstObjectByType<MapSpawner>();
                if (spawner)
                {
                    mapData = spawner.MapData;
                }
            }

            if (!playerContainer)
            {
                playerContainer = new GameObject("Players").transform;
                playerContainer.SetParent(transform);
            }

            if (eggContainer == null)
            {
                eggContainer = new GameObject("Eggs").transform;
                eggContainer.SetParent(transform);
            }
        }

        private void InitializeSystems()
        {
            networkManager = GetComponent<NetworkManager>();
            if (networkManager == null)
            {
                networkManager = gameObject.AddComponent<NetworkManager>();
            }

            inputHandler = new InputHandler();
            inputHandler.OnMoveInput += HandleLocalPlayerInput;
            inputHandler.SetEnabled(false);

            aiManager = new AIManager(mapData);
            aiManager.OnBotMoveRequested += HandleBotInput;
            aiManager.SetEnabled(false);
        }

        private void Start()
        {
            networkManager.Initialize(mapData);

            networkManager.Client.OnGameStarted += HandleGameStarted;
            networkManager.Client.OnGameEnded += HandleGameEnded;
            networkManager.Client.OnPlayerUpdated += HandlePlayerUpdated;
            networkManager.Client.OnEggSpawned += HandleEggSpawned;
            networkManager.Client.OnEggCollected += HandleEggCollected;
            networkManager.Client.OnTimeUpdated += HandleTimeUpdated;
            networkManager.Client.OnScoreUpdated += HandleScoreUpdated;

            if (autoStart)
            {
                StartGame();
            }
        }

        private void Update()
        {
            if (!isGameRunning) return;

            inputHandler.Update();
            aiManager.Update(Time.deltaTime);
        }

        public void StartGame()
        {
            if (isGameRunning) return;

            networkManager.StartGame(playerCount, gameDuration);
        }

        private void HandleGameStarted()
        {
            isGameRunning = true;
            localPlayerId = networkManager.Client.LocalPlayerId;

            var botIds = new List<int>();
            foreach (var player in networkManager.Client.GetAllPlayers())
            {
                CreatePlayerView(player);
                if (!player.IsLocal)
                {
                    botIds.Add(player.PlayerId);
                }
            }

            aiManager.Initialize(localPlayerId, botIds);
            aiManager.SetEnabled(true);
            inputHandler.SetEnabled(true);

            if (gameUI != null)
            {
                gameUI.ShowGameHUD();
            }

            OnGameStarted?.Invoke();
        }

        private void HandleGameEnded()
        {
            isGameRunning = false;
            inputHandler.SetEnabled(false);
            aiManager.SetEnabled(false);

            if (gameUI)
            {
                var scores = new List<(int playerId, int score, Color color)>();
                foreach (var player in networkManager.Client.GetAllPlayers())
                {
                    scores.Add((player.PlayerId, player.EggCount, player.PlayerColor));
                }
                gameUI.ShowGameOver(scores);
            }

            OnGameEnded?.Invoke();
        }

        private void HandlePlayerUpdated(ClientPlayerData playerData)
        {
            if (!playerViews.TryGetValue(playerData.PlayerId, out var view))
            {
                CreatePlayerView(playerData);
                return;
            }

            view.UpdatePosition(playerData.Position);
            aiManager.UpdatePlayerPosition(playerData.PlayerId, playerData.GridPosition);
        }

        private void HandleEggSpawned(ClientEggData eggData)
        {
            if (eggViews.ContainsKey(eggData.EggId)) return;

            CreateEggView(eggData);
            UpdateAIEggTargets();
        }

        private void HandleEggCollected(int eggId, int playerId)
        {
            if (eggViews.TryGetValue(eggId, out var view))
            {
                view.Collect();
            }

            UpdateAIEggTargets();
        }

        private void HandleTimeUpdated(float remainingTime)
        {
            OnTimeChanged?.Invoke(remainingTime);

            if (gameUI != null)
            {
                gameUI.UpdateTimer(remainingTime);
            }
        }

        private void HandleScoreUpdated(int playerId, int score)
        {
            if (playerViews.TryGetValue(playerId, out var view))
            {
                view.UpdateScoreLabel(score);
            }

            OnScoreChanged?.Invoke(playerId, score);

            if (gameUI != null)
            {
                gameUI.UpdateScore(playerId, score);
            }
        }

        private void HandleLocalPlayerInput(Vector2Int direction)
        {
            if (!isGameRunning) return;

            var input = networkManager.Client.CreateInput(direction);
            networkManager.SendInputToServer(input);
        }

        private void HandleBotInput(int botId, Vector2Int direction)
        {
            if (!isGameRunning) return;

            var input = new InputMessage
            {
                PlayerId = botId,
                MoveDirection = direction,
                ClientTimestamp = Time.time
            };

            networkManager.SendInputToServer(input);
        }

        private void CreatePlayerView(ClientPlayerData playerData)
        {
            GameObject playerObj;
            if (playerPrefab)
            {
                playerObj = Instantiate(playerPrefab, playerContainer);
            }
            else
            {
                playerObj = new GameObject($"Player_{playerData.PlayerId}");
                playerObj.transform.SetParent(playerContainer);
            }

            var view = playerObj.GetComponent<PlayerView>();
            if (!view)
            {
                view = playerObj.AddComponent<PlayerView>();
            }

            view.Initialize(playerData.PlayerId, playerData.PlayerColor, playerData.IsLocal);
            view.SetPositionImmediate(playerData.Position);
            playerViews[playerData.PlayerId] = view;
        }

        private void CreateEggView(ClientEggData eggData)
        {
            GameObject eggObj;
            if (eggPrefab != null)
            {
                eggObj = Instantiate(eggPrefab, eggContainer);
            }
            else
            {
                eggObj = new GameObject($"Egg_{eggData.EggId}");
                eggObj.transform.SetParent(eggContainer);
            }

            var view = eggObj.GetComponent<EggView>();
            if (view == null)
            {
                view = eggObj.AddComponent<EggView>();
            }

            view.Initialize(eggData.EggId, eggData.Position, eggData.EggColor);
            eggViews[eggData.EggId] = view;
        }

        private void UpdateAIEggTargets()
        {
            var eggPositions = new List<Vector2Int>();
            foreach (var egg in networkManager.Client.GetAllEggs())
            {
                if (egg.IsActive)
                {
                    eggPositions.Add(egg.GridPosition);
                }
            }
            aiManager.UpdateEggPositions(eggPositions);
        }

        public void RestartGame()
        {
            foreach (var view in playerViews.Values)
            {
                if (view != null)
                {
                    Destroy(view.gameObject);
                }
            }
            playerViews.Clear();

            foreach (var view in eggViews.Values)
            {
                if (view != null)
                {
                    Destroy(view.gameObject);
                }
            }
            eggViews.Clear();

            StartGame();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            if (inputHandler != null)
            {
                inputHandler.OnMoveInput -= HandleLocalPlayerInput;
            }

            if (aiManager != null)
            {
                aiManager.OnBotMoveRequested -= HandleBotInput;
            }
        }
    }
}
