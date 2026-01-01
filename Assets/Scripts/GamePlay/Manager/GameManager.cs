using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core;
using DropSystem;
using GridSystem.Core;
using Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GamePlay
{
    public class GameManager : Singleton<GameManager>
    {
        [Header("Game Settings")] public float gameDuration = 60f;
        public int numberOfBots = 3;
        public MapGridData MapData => StageManager.Instance.mapData;

        [Header("Prefabs")] public GameObject localPlayerPrefab;
        public List<GameObject> botPlayerPrefabs;


        // Events
        public event Action<GameState> OnGameStateChanged;
        public event Action<float> OnTimeChanged;
        public event Action<PlayerController, int> OnPlayerScoreChanged;
        public event Action<PlayerController> OnGameWinner;

        // Game state
        private GameState currentState = GameState.Waiting;
        private float timeRemaining;
        private List<PlayerController> players = new List<PlayerController>();
        private PlayerController localPlayer;

        public GameState CurrentState => currentState;
        public float TimeRemaining => timeRemaining;
        public List<PlayerController> Players => players;
        public PlayerController LocalPlayer => localPlayer;

        void Start()
        {
            StartCoroutine(InitializeGame());
        }

        IEnumerator InitializeGame()
        {
            yield return new WaitForSeconds(0.5f);

            SpawnPlayers();

            yield return new WaitForSeconds(1f);

            StartGame();
        }

        void SpawnPlayers()
        {
            var spawnPoints = MapData.GetCellsOfType(CellType.SpawnPoint);

            if (spawnPoints.Count == 0)
            {
                Debug.LogError("No spawn points found!");
                return;
            }

            SpawnLocalPlayer(spawnPoints[0]);

            for (int i = 0; i < numberOfBots; i++)
            {
                var spawnPos = spawnPoints[(i + 1) % spawnPoints.Count];
                SpawnBot(i + 1, spawnPos);
            }
        }

        void SpawnLocalPlayer(Vector2Int spawnPos)
        {
            Vector3 worldPos = MapData.GridToWorld(spawnPos.x, spawnPos.y);
            
            GameObject playerObj = Instantiate(localPlayerPrefab, worldPos , Quaternion.identity);

            LocalPlayer player = playerObj.GetComponent<LocalPlayer>();
            player.playerId = 0;
            player.playerName = "YOU";
            player.SetGridPosition(spawnPos);

            players.Add(player);
            localPlayer = player;
        }

        void SpawnBot(int botId, Vector2Int spawnPos)
        {
            Vector3 worldPos = MapData.GridToWorld(spawnPos.x, spawnPos.y);
            
            GameObject botObj = Instantiate(botPlayerPrefabs[botId%botPlayerPrefabs.Count], worldPos , Quaternion.identity);

            BotPlayer bot = botObj.GetComponent<BotPlayer>();
            bot.playerId = botId;
            bot.playerName = $"Bot {botId}";
            bot.SetGridPosition(spawnPos);

            players.Add(bot);
        }

        void StartGame()
        {
            ChangeState(GameState.Playing);
            timeRemaining = gameDuration;

            if (EggManager.Instance)
            {
                EggManager.Instance.StartSpawning();
            }

            StartCoroutine(GameLoop());
        }

        IEnumerator GameLoop()
        {
            while (timeRemaining > 0 && currentState == GameState.Playing)
            {
                timeRemaining -= Time.deltaTime;
                OnTimeChanged?.Invoke(timeRemaining);
                yield return null;
            }

            EndGame();
        }

        void EndGame()
        {
            ChangeState(GameState.Ended);

            if (EggManager.Instance)
            {
                EggManager.Instance.StopSpawning();
            }

            PlayerController winner = players.OrderByDescending(p => p.Score).FirstOrDefault();
            OnGameWinner?.Invoke(winner);
        }

        void ChangeState(GameState newState)
        {
            currentState = newState;
            OnGameStateChanged?.Invoke(newState);
        }

        public void RestartGame()
        {
            SceneManager.LoadScene(
                SceneManager.GetActiveScene().name
            );
        }

    }
}