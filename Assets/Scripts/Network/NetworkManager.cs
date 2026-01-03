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

		[SerializeField] NetworkSettings settings = new NetworkSettings();

		private INetworkTransport transport;
		private IServer server;
		private IClient client;
		private INetworkFactory factory;
		private MapGridData mapData;

		private bool isInitialized;

		public IServer Server => server;
		public IClient Client => client;
		public INetworkTransport Transport => transport;
		public bool IsInitialized => isInitialized;

		public event Action OnNetworkReady;

		private void Awake()
		{
			if (Instance && Instance != this)
			{
				Destroy(gameObject);
				return;
			}
			Instance = this;
		}

		public void Initialize(MapGridData map)
		{
			Initialize(map, CreateFactory());
		}

		void Initialize(MapGridData map, INetworkFactory customFactory)
		{
			mapData = map;
			factory = customFactory;

			transport = factory.CreateTransport();
			server = factory.CreateServer(mapData);
			client = factory.CreateClient(mapData);

			BindEvents();

			isInitialized = true;
			OnNetworkReady?.Invoke();
		}

		private INetworkFactory CreateFactory()
		{
			switch (settings.networkMode)
			{
				case NetworkMode.Simulated:
				default:
					return new SimulatedNetworkFactory(settings);
			}
		}

		private void BindEvents()
		{
			server.OnBroadcastMessage += OnServerBroadcast;
			server.OnSendToClient += OnServerSendToClient;
			transport.OnServerMessageReceived += OnTransportServerMessage;
			transport.OnClientMessageReceived += OnTransportClientMessage;
		}

		private void Update()
		{
			if (!isInitialized) return;

			float deltaTime = Time.deltaTime;
			transport.Update(deltaTime);
			server.Update(deltaTime);
			client.Update(deltaTime);
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
			settings.minLatency = min;
			settings.maxLatency = max;
			if (transport != null)
			{
				transport.MinLatency = min;
				transport.MaxLatency = max;
			}
		}

		public void SetPacketLoss(float chance)
		{
			settings.packetLossChance = Mathf.Clamp01(chance);
			if (transport != null) transport.PacketLossChance = settings.packetLossChance;
		}

		public void SetPredictionEnabled(bool enabled)
		{
			settings.enablePrediction = enabled;
			if (client != null) client.EnablePrediction = enabled;
		}

		public void SetReconciliationEnabled(bool enabled)
		{
			settings.enableReconciliation = enabled;
			if (client != null) client.EnableReconciliation = enabled;
		}

		public void SetInterpolationEnabled(bool enabled)
		{
			settings.enableInterpolation = enabled;
			if (client != null) client.EnableInterpolation = enabled;
		}

		public void SetInterpolationDelay(float delay)
		{
			settings.interpolationDelay = delay;
			if (client != null) client.InterpolationDelay = delay;
		}

		public void SetDebugMode(bool enabled)
		{
			settings.debugMode = enabled;
			if (transport != null) transport.DebugMode = enabled;
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