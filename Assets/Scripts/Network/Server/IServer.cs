using System;
using Network.Messages;
namespace Network.Server
{
	public interface IServer
	{
		event Action<NetworkMessage> OnBroadcastMessage;
		event Action<int, NetworkMessage> OnSendToClient;
        
		void StartGame(int playerCount, float duration);
		void Update(float deltaTime);
		void ProcessInput(InputMessage input);
        
		bool IsRunning { get; }
		float RemainingTime { get; }
		bool DebugMode { get; set; }
	}
}