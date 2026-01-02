using System;
using GridSystem.Core;
using Network.Client;
using Network.Messages;
using Network.Server;
using Network.Transport;
using UnityEngine;
namespace Network
{
	// Bridges the server and client simulators.
	public class NetworkManager : MonoBehaviour
	{
		public static NetworkManager Instance { get; private set; }

		[Header("Network Settings")]
		[SerializeField] private float minLatency = 0.05f;
		[SerializeField] private float maxLatency = 0.15f;
		[SerializeField] private float packetLossChance = 0f;

		[Header("Debug")]
		[SerializeField] private bool debugMessages = false;
		[SerializeField] private bool enablePrediction = true;
		[SerializeField] private bool enableReconciliation = true;
		[SerializeField] private bool enableInterpolation = true;
		[SerializeField] private float interpolationDelay = 0.1f;

		private SimulatedNetworkTransport transport;
		private ServerSimulator server;
		private ClientSimulator client;
		private MapGridData mapData;

		private bool isInitialized;

		public ServerSimulator Server => server;
		public ClientSimulator Client => client;
		public SimulatedNetworkTransport Transport => transport;
		public bool IsInitialized => isInitialized;

		public event Action OnNetworkReady;

		private void Awake()
		{
			if (Instance != null && Instance != this)
			{
				Destroy(gameObject);
				return;
			}
			Instance = this;
		}

		public void Initialize(MapGridData map)
		{
			mapData = map;

			transport = new SimulatedNetworkTransport();
			transport.SetLatency(minLatency, maxLatency);
			transport.PacketLossChance = packetLossChance;
			transport.DebugLogMessages = debugMessages;

			server = new ServerSimulator(mapData);
			server.DebugMode = debugMessages;
			server.OnBroadcastMessage += OnServerBroadcast;
			server.OnSendToClient += OnServerSendToClient;

			client = new ClientSimulator(mapData);
			client.EnablePrediction = enablePrediction;
			client.EnableReconciliation = enableReconciliation;
			client.EnableInterpolation = enableInterpolation;
			client.InterpolationDelay = interpolationDelay;
			client.DebugMode = debugMessages;

			transport.OnServerMessageReceived += OnTransportServerMessage;
			transport.OnClientMessageReceived += OnTransportClientMessage;

			isInitialized = true;
			OnNetworkReady?.Invoke();
		}

		private void Update()
		{
			if (!isInitialized) return;

			transport.Update(Time.deltaTime);
			server.Update(Time.deltaTime);
			client.Update(Time.deltaTime);
		}

		public void SendInputToServer(InputMessage input)
		{
			transport.SendToServer(input);
		}

		private void OnServerBroadcast(NetworkMessage message)
		{
			transport.SendToAllClients(message);
		}

		private void OnServerSendToClient(int clientId, NetworkMessage message)
		{
			transport.SendToClient(clientId, message);
		}

		private void OnTransportServerMessage(NetworkMessage message)
		{
			if (message is InputMessage inputMsg)
			{
				server.ProcessInput(inputMsg);
			}
		}

		private void OnTransportClientMessage(int clientId, NetworkMessage message)
		{
			client.ProcessMessage(message);
		}

		public void StartGame(int playerCount, float duration)
		{
			if (!isInitialized)
			{
				Debug.LogError("NetworkManager not initialized!");
				return;
			}

			server.StartGame(playerCount, duration);
		}

		public void SetLatencySettings(float min, float max)
		{
			minLatency = min;
			maxLatency = max;
			transport?.SetLatency(min, max);
		}

		public void SetPacketLoss(float chance)
		{
			packetLossChance = Mathf.Clamp01(chance);
			if (transport != null)
			{
				transport.PacketLossChance = packetLossChance;
			}
		}

		public void SetPredictionEnabled(bool enabled)
		{
			enablePrediction = enabled;
			if (client != null)
			{
				client.EnablePrediction = enabled;
			}
		}

		public void SetReconciliationEnabled(bool enabled)
		{
			enableReconciliation = enabled;
			if (client != null)
			{
				client.EnableReconciliation = enabled;
			}
		}

		public void SetInterpolationEnabled(bool enabled)
		{
			enableInterpolation = enabled;
			if (client != null)
			{
				client.EnableInterpolation = enabled;
			}
		}

		public void SetInterpolationDelay(float delay)
		{
			interpolationDelay = delay;
			if (client != null)
			{
				client.InterpolationDelay = delay;
			}
		}

		public void SetDebugMode(bool enabled)
		{
			debugMessages = enabled;
			if (transport != null) transport.DebugLogMessages = enabled;
			if (server != null) server.DebugMode = enabled;
			if (client != null) client.DebugMode = enabled;
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