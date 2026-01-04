using System;
using System.Collections.Generic;
using UnityEngine;
using Network.Messages;
using Network.Transport;
using GridSystem.Core;
using Network.GameState;

namespace Network.Server
{
	public class ServerSimulator : IServer
	{
		public event Action<NetworkMessage> OnBroadcastMessage;
		public event Action<int, NetworkMessage> OnSendToClient;

		private readonly MapGridData mapData;
		private readonly Dictionary<int, ServerPlayerData> players = new Dictionary<int, ServerPlayerData>();
		private readonly Dictionary<int, ServerEggData> eggs = new Dictionary<int, ServerEggData>();

		private float gameTime;
		private float gameDuration;
		private float remainingTime;
		private bool isGameRunning;
		private int nextEggId;
		private float lastUpdateTime;
		private float updateInterval;
		private float minUpdateInterval = 0.1f;
		private float maxUpdateInterval = 0.5f;

		private float eggSpawnTimer;
		private float eggSpawnInterval = 3f;
		private int maxEggs = 5;
		private float playerMoveSpeed = 4f;

		private readonly List<Vector2Int> spawnPoints = new List<Vector2Int>();
		private readonly List<Vector2Int> eggSpawnPoints = new List<Vector2Int>();
		private readonly Color[] playerColors =
		{
			Color.cyan, Color.magenta,
			Color.yellow, Color.green,
			Color.red
		};
		private readonly Color[] eggColors =
		{
			Color.cyan, Color.magenta,
			Color.yellow, Color.green,
			Color.white
		};

		public bool DebugMode { get; set; } = false;
		public float TickRate { get; set; } = 60f;

		public ServerSimulator(MapGridData map)
		{
			mapData = map;
			CacheSpawnPoints();
		}

		private void CacheSpawnPoints()
		{
			spawnPoints.Clear();
			eggSpawnPoints.Clear();

			for (int y = 0; y < mapData.height; y++)
			{
				for (int x = 0; x < mapData.width; x++)
				{
					var cell = mapData.GetCell(x, y);
					if (cell == CellType.SpawnPoint)
					{
						spawnPoints.Add(new Vector2Int(x, y));
					}
					else if (cell == CellType.EggSpawnPoint || (cell == CellType.Ground && mapData.IsWalkable(x, y)))
					{
						eggSpawnPoints.Add(new Vector2Int(x, y));
					}
				}
			}

			if (spawnPoints.Count == 0)
			{
				var walkable = mapData.GetWalkableCells();
				for (int i = 0; i < Mathf.Min(10, walkable.Count); i++)
				{
					spawnPoints.Add(walkable[UnityEngine.Random.Range(0, walkable.Count)]);
				}
			}

			if (eggSpawnPoints.Count == 0)
			{
				eggSpawnPoints.AddRange(mapData.GetWalkableCells());
			}
		}

		public void StartGame(int playerCount, float duration)
		{
			gameDuration = duration;
			remainingTime = duration;
			gameTime = 0;
			isGameRunning = true;
			nextEggId = 0;
			eggSpawnTimer = 0;

			players.Clear();
			eggs.Clear();

			var shuffledSpawns = new List<Vector2Int>(spawnPoints);
			ShuffleList(shuffledSpawns);

			for (int i = 0; i < playerCount; i++)
			{
				var spawnPos = shuffledSpawns[i % shuffledSpawns.Count];
				var worldPos = mapData.GridToWorld(spawnPos.x, spawnPos.y);

				var player = new ServerPlayerData
				{
					PlayerId = i,
					Position = worldPos,
					GridPosition = spawnPos,
					TargetGridPosition = spawnPos,
					EggCount = 0,
					PlayerColor = playerColors[i % playerColors.Length],
					IsMoving = false,
					MoveProgress = 0
				};
				players[i] = player;
			}

			var startMessage = new GameStartMessage
			{
				GameDuration = duration,
				LocalPlayerId = 0
			};

			foreach (var player in players.Values)
			{
				startMessage.InitialPlayers.Add(CreatePlayerState(player));
			}

			OnBroadcastMessage?.Invoke(startMessage);

			for (int i = 0; i < 3; i++)
			{
				SpawnEgg();
			}

			SetRandomUpdateInterval();

			if (DebugMode)
			{
				Debug.Log($"[Server] Game started with {playerCount} players for {duration}s");
			}
		}

		public void Update(float deltaTime)
		{
			if (!isGameRunning) return;

			gameTime += deltaTime;
			remainingTime = Mathf.Max(0, gameDuration - gameTime);

			ProcessAllPlayerInputs();
			UpdatePlayerMovement(deltaTime);
			CheckEggCollisions();
			UpdateEggSpawning(deltaTime);

			lastUpdateTime += deltaTime;
			if (lastUpdateTime >= updateInterval)
			{
				lastUpdateTime = 0;
				SetRandomUpdateInterval();
				BroadcastWorldState();
			}

			if (remainingTime <= 0)
			{
				EndGame();
			}
		}

		private void SetRandomUpdateInterval()
		{
			updateInterval = UnityEngine.Random.Range(minUpdateInterval, maxUpdateInterval);
		}

		public void ProcessInput(InputMessage input)
		{
			if (!players.TryGetValue(input.PlayerId, out var player)) return;
			player.PendingInputs.Enqueue(input);
		}

		private void ProcessAllPlayerInputs()
		{
			foreach (var player in players.Values)
			{
				while (player.PendingInputs.Count > 0)
				{
					var input = player.PendingInputs.Dequeue();
					ProcessPlayerInput(player, input);
				}
			}
		}

		private void ProcessPlayerInput(ServerPlayerData player, InputMessage input)
		{
			if (player.IsMoving) return;

			if (input.MoveDirection == Vector2Int.zero) return;

			var targetGrid = player.GridPosition + input.MoveDirection;

			if (mapData.IsWalkable(targetGrid.x, targetGrid.y))
			{
				player.TargetGridPosition = targetGrid;
				player.IsMoving = true;
				player.MoveDirection = input.MoveDirection;
				player.MoveProgress = 0;
			}

			player.LastProcessedInputSequence = input.SequenceNumber;
		}

		private void UpdatePlayerMovement(float deltaTime)
		{
			foreach (var player in players.Values)
			{
				if (!player.IsMoving) continue;

				player.MoveProgress += playerMoveSpeed * deltaTime;

				var startPos = mapData.GridToWorld(player.GridPosition.x, player.GridPosition.y);
				var endPos = mapData.GridToWorld(player.TargetGridPosition.x, player.TargetGridPosition.y);
				player.Position = Vector3.Lerp(startPos, endPos, player.MoveProgress);

				if (player.MoveProgress >= 1f)
				{
					player.GridPosition = player.TargetGridPosition;
					player.Position = endPos;
					player.IsMoving = false;
					player.MoveProgress = 0;
				}
			}
		}

		private void CheckEggCollisions()
		{
			var collectedEggs = new List<int>();

			foreach (var egg in eggs.Values)
			{
				if (!egg.IsActive) continue;

				foreach (var player in players.Values)
				{
					if (player.GridPosition == egg.GridPosition ||
					    (player.IsMoving && player.TargetGridPosition == egg.GridPosition))
					{
						egg.IsActive = false;
						player.EggCount++;
						collectedEggs.Add(egg.EggId);

						var collectMsg = new EggCollectedMessage
						{
							EggId = egg.EggId,
							PlayerId = player.PlayerId,
							NewEggCount = player.EggCount
						};
						OnBroadcastMessage?.Invoke(collectMsg);

						if (DebugMode)
						{
							Debug.Log($"[Server] Player {player.PlayerId} collected egg {egg.EggId}. Total: {player.EggCount}");
						}
						break;
					}
				}
			}

			foreach (var eggId in collectedEggs)
			{
				eggs.Remove(eggId);
			}
		}

		private void UpdateEggSpawning(float deltaTime)
		{
			eggSpawnTimer += deltaTime;

			int activeEggs = 0;
			foreach (var egg in eggs.Values)
			{
				if (egg.IsActive) activeEggs++;
			}

			if (activeEggs < maxEggs && eggSpawnTimer >= eggSpawnInterval)
			{
				eggSpawnTimer = 0;
				SpawnEgg();
			}
		}

		private void SpawnEgg()
		{
			if (eggSpawnPoints.Count == 0) return;

			Vector2Int spawnPos;
			int attempts = 0;
			do
			{
				spawnPos = eggSpawnPoints[UnityEngine.Random.Range(0, eggSpawnPoints.Count)];
				attempts++;
			} while (IsPositionOccupied(spawnPos) && attempts < 20);

			if (attempts >= 20) return;

			var eggId = nextEggId++;
			var worldPos = mapData.GridToWorld(spawnPos.x, spawnPos.y);
			var color = eggColors[UnityEngine.Random.Range(0, eggColors.Length)];

			var egg = new ServerEggData
			{
				EggId = eggId,
				Position = worldPos,
				GridPosition = spawnPos,
				EggColor = color,
				IsActive = true
			};
			eggs[eggId] = egg;

			var spawnMsg = new EggSpawnMessage
			{
				EggId = eggId,
				Position = worldPos,
				GridPosition = spawnPos,
				EggColor = color
			};
			OnBroadcastMessage?.Invoke(spawnMsg);

			if (DebugMode)
			{
				Debug.Log($"[Server] Spawned egg {eggId} at {spawnPos}");
			}
		}

		private bool IsPositionOccupied(Vector2Int pos)
		{
			foreach (var player in players.Values)
			{
				if (player.GridPosition == pos) return true;
			}
			foreach (var egg in eggs.Values)
			{
				if (egg.IsActive && egg.GridPosition == pos) return true;
			}
			return false;
		}

		private void BroadcastWorldState()
		{
			var state = new WorldStateMessage
			{
				GameTime = gameTime,
				RemainingTime = remainingTime
			};

			foreach (var player in players.Values)
			{
				state.Players.Add(CreatePlayerState(player));
			}

			foreach (var egg in eggs.Values)
			{
				if (egg.IsActive)
				{
					state.Eggs.Add(new EggState
					{
						EggId = egg.EggId,
						Position = egg.Position,
						GridPosition = egg.GridPosition,
						EggColor = egg.EggColor,
						IsActive = true
					});
				}
			}

			OnBroadcastMessage?.Invoke(state);
		}

		private PlayerState CreatePlayerState(ServerPlayerData player)
		{
			return new PlayerState
			{
				PlayerId = player.PlayerId,
				Position = player.Position,
				GridPosition = player.GridPosition,
				EggCount = player.EggCount,
				PlayerColor = player.PlayerColor,
				IsMoving = player.IsMoving,
				MoveDirection = player.MoveDirection,
				LastProcessedInput = player.LastProcessedInputSequence
			};
		}

		private void EndGame()
		{
			isGameRunning = false;

			int winnerId = -1;
			int maxEggCount = -1;

			foreach (var player in players.Values)
			{
				if (player.EggCount > maxEggCount)
				{
					maxEggCount = player.EggCount;
					winnerId = player.PlayerId;
				}
			}

			var endMsg = new GameEndMessage
			{
				WinnerId = winnerId
			};

			foreach (var player in players.Values)
			{
				endMsg.FinalScores.Add(CreatePlayerState(player));
			}

			OnBroadcastMessage?.Invoke(endMsg);

			if (DebugMode)
			{
				Debug.Log($"[Server] Game ended. Winner: Player {winnerId} with {maxEggCount} eggs");
			}
		}

		private void ShuffleList<T>(List<T> list)
		{
			for (int i = list.Count - 1; i > 0; i--)
			{
				int j = UnityEngine.Random.Range(0, i + 1);
				(list[i], list[j]) = (list[j], list[i]);
			}
		}

		public ServerPlayerData GetPlayer(int playerId)
		{
			return players.GetValueOrDefault(playerId);
		}

		public bool IsRunning => isGameRunning;
		public float RemainingTime => remainingTime;
	}
}