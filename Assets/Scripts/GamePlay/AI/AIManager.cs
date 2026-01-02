using System;
using System.Collections.Generic;
using GridSystem.Core;
using GridSystem.PathFinding;
using UnityEngine;
namespace GamePlay.AI
{
	// Handle multiple bot players.
	public class AIManager
	{
		public event Action<int, Vector2Int> OnBotMoveRequested;

		private readonly Dictionary<int, AIController> botControllers = new Dictionary<int, AIController>();
		private readonly MapGridData mapData;
		private readonly GridPathfinder pathfinder;
		private readonly List<Vector2Int> activeEggPositions = new List<Vector2Int>();
		private readonly Dictionary<int, Vector2Int> playerPositions = new Dictionary<int, Vector2Int>();

		private int localPlayerId = -1;
		private bool isEnabled = true;

		public AIManager(MapGridData map)
		{
			mapData = map;
			pathfinder = new GridPathfinder(map);
		}

		public void Initialize(int localPlayer, IEnumerable<int> botPlayerIds)
		{
			localPlayerId = localPlayer;
			botControllers.Clear();

			foreach (var botId in botPlayerIds)
			{
				if (botId == localPlayerId) continue;

				var controller = new AIController(botId, mapData, pathfinder);
				controller.OnMoveRequested += (dir) => HandleBotMove(botId, dir);
				botControllers[botId] = controller;
			}
		}

		public void Update(float deltaTime)
		{
			if (!isEnabled) return;

			UpdateBotTargets();

			foreach (var kvp in botControllers)
			{
				kvp.Value.Update(deltaTime);
			}
		}

		private void UpdateBotTargets()
		{
			if (activeEggPositions.Count == 0) return;

			foreach (var kvp in botControllers)
			{
				var bot = kvp.Value;
				var botPos = bot.CurrentGridPosition;

				Vector2Int? bestEgg = null;
				float bestScore = float.MaxValue;

				foreach (var eggPos in activeEggPositions)
				{
					float distance = Vector2Int.Distance(botPos, eggPos);

					float competitionPenalty = 0;
					foreach (var otherBot in botControllers)
					{
						if (otherBot.Key == kvp.Key) continue;
						var otherTarget = otherBot.Value.GetTargetEgg();
						if (otherTarget.HasValue && otherTarget.Value == eggPos)
						{
							competitionPenalty += 2f;
						}
					}

					if (playerPositions.TryGetValue(localPlayerId, out var localPos))
					{
						float localDist = Vector2Int.Distance(localPos, eggPos);
						if (localDist < distance)
						{
							competitionPenalty += 1.5f;
						}
					}

					float score = distance + competitionPenalty + UnityEngine.Random.Range(0f, 1f);

					if (score < bestScore)
					{
						bestScore = score;
						bestEgg = eggPos;
					}
				}

				bot.SetTargetEgg(bestEgg);
			}
		}

		private void HandleBotMove(int botId, Vector2Int direction)
		{
			OnBotMoveRequested?.Invoke(botId, direction);
		}

		public void UpdatePlayerPosition(int playerId, Vector2Int gridPos)
		{
			playerPositions[playerId] = gridPos;

			if (botControllers.TryGetValue(playerId, out var controller))
			{
				controller.SetPosition(gridPos);
			}
		}

		public void UpdateEggPositions(IEnumerable<Vector2Int> eggs)
		{
			activeEggPositions.Clear();
			activeEggPositions.AddRange(eggs);
		}

		public void RemoveEgg(Vector2Int pos)
		{
			activeEggPositions.Remove(pos);
		}

		public void AddBot(int botId, Vector2Int startPos)
		{
			if (botControllers.ContainsKey(botId)) return;

			var controller = new AIController(botId, mapData, pathfinder);
			controller.SetPosition(startPos);
			controller.OnMoveRequested += (dir) => HandleBotMove(botId, dir);
			botControllers[botId] = controller;
		}

		public void RemoveBot(int botId)
		{
			botControllers.Remove(botId);
		}

		public void SetEnabled(bool enabled)
		{
			isEnabled = enabled;
		}

		public int BotCount => botControllers.Count;
	}
}