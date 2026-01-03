using System;
using Network.Messages;
namespace Network.Transport
{
	public interface INetworkTransport
	{
		event Action<NetworkMessage> OnServerMessageReceived;
		event Action<int, NetworkMessage> OnClientMessageReceived;
		
		void SendToServer(NetworkMessage message);
		void SendToClient(int clientId, NetworkMessage message);
		void SendToAllClients(NetworkMessage message);
		void Update(float deltaTime);
		
		float MinLatency { get; set; }
		float MaxLatency { get; set; }
		bool DebugMode { get; set; }
		float PacketLossChance { get; set; }
	}
}