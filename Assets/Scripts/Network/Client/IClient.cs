using System;
using System.Collections.Generic;
using Network.Messages;
using UnityEngine;
namespace Network.Client
{
	public interface IClient
	{
		event Action OnGameStarted;
		event Action OnGameEnded;
		event Action<ClientPlayerData> OnPlayerUpdated;
		event Action<ClientEggData> OnEggSpawned;
		event Action<int, int> OnEggCollected;
		event Action<float> OnTimeUpdated;
		event Action<int, int> OnScoreUpdated;

		void ProcessMessage(NetworkMessage message);
		void Update(float deltaTime);
		InputMessage CreateInput(Vector2Int direction);
        
		ClientPlayerData GetLocalPlayer();
		ClientPlayerData GetPlayer(int playerId);
		IEnumerable<ClientPlayerData> GetAllPlayers();
		IEnumerable<ClientEggData> GetAllEggs();

		bool IsRunning { get; }
		float RemainingTime { get; }
		int LocalPlayerId { get; }
        
		bool EnablePrediction { get; set; }
		bool EnableReconciliation { get; set; }
		bool EnableInterpolation { get; set; }
		float InterpolationDelay { get; set; }
		bool DebugMode { get; set; }
	}
}