using System;
using Network.Messages;
namespace Network.Transport
{
	public interface INetworkTransport
	{
		void SendToServer(NetworkMessage message);
		void SendToClient(int clientId, NetworkMessage message);
		void SendToAllClients(NetworkMessage message);
		event Action<NetworkMessage> OnServerMessageReceived;
		event Action<int, NetworkMessage> OnClientMessageReceived;
		void Update(float deltaTime);
		void SetLatency(float minLatency, float maxLatency);
	}
}