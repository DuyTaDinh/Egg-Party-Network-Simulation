using System;
using System.Collections.Generic;
using Network.Messages;
using UnityEngine;
namespace Network.Transport
{
	public class SimulatedNetworkTransport : INetworkTransport
	{
		private class PendingMessage
		{
			public NetworkMessage Message;
			public float DeliveryTime;
			public int TargetClientId;
			public bool IsServerBound;
		}

		private readonly List<PendingMessage> pendingMessages = new List<PendingMessage>();
		private float minLatency = 0.05f;
		private float maxLatency = 0.15f;
		private float currentTime;
		private uint sequenceCounter;

		public event Action<NetworkMessage> OnServerMessageReceived;
		public event Action<int, NetworkMessage> OnClientMessageReceived;

		public float PacketLossChance { get; set; } = 0f;
		public float MinLatency
		{
			get
			{
				return minLatency;
			}
			set
			{
				minLatency = value;
			}
		}
		public float MaxLatency
		{
			get
			{
				return maxLatency;
			}
			set
			{
				maxLatency = value;
			}
		}
		public bool DebugMode { get; set; } = false;

		public void SetLatency(float min, float max)
		{
			minLatency = Mathf.Max(0, min);
			maxLatency = Mathf.Max(minLatency, max);
		}

		public float GetCurrentLatency()
		{
			return UnityEngine.Random.Range(minLatency, maxLatency);
		}

		public void SendToServer(NetworkMessage message)
		{
			if (UnityEngine.Random.value < PacketLossChance) return;

			message.SequenceNumber = ++sequenceCounter;
			message.Timestamp = currentTime;

			var pending = new PendingMessage
			{
				Message = message,
				DeliveryTime = currentTime + GetCurrentLatency(),
				IsServerBound = true
			};
			pendingMessages.Add(pending);

			if (DebugMode)
			{
				Debug.Log($"[Transport] Queued message to server: {message.Type} (delivery in {pending.DeliveryTime - currentTime:F3}s)");
			}
		}

		public void SendToClient(int clientId, NetworkMessage message)
		{
			if (UnityEngine.Random.value < PacketLossChance) return;

			message.SequenceNumber = ++sequenceCounter;
			message.Timestamp = currentTime;

			var pending = new PendingMessage
			{
				Message = message,
				DeliveryTime = currentTime + GetCurrentLatency(),
				TargetClientId = clientId,
				IsServerBound = false
			};
			pendingMessages.Add(pending);

			if (DebugMode)
			{
				Debug.Log($"[Transport] Queued message to client {clientId}: {message.Type}");
			}
		}

		public void SendToAllClients(NetworkMessage message)
		{
			SendToClient(-1, message);
		}

		public void Update(float deltaTime)
		{
			currentTime += deltaTime;

			for (int i = pendingMessages.Count - 1; i >= 0; i--)
			{
				var pending = pendingMessages[i];
				if (currentTime >= pending.DeliveryTime)
				{
					pendingMessages.RemoveAt(i);
					DeliverMessage(pending);
				}
			}
		}
		
		private void DeliverMessage(PendingMessage pending)
		{
			if (pending.IsServerBound)
			{
				if (DebugMode)
				{
					Debug.Log($"[Transport] Delivering to server: {pending.Message.Type}");
				}
				OnServerMessageReceived?.Invoke(pending.Message);
			}
			else
			{
				if (DebugMode)
				{
					Debug.Log($"[Transport] Delivering to client {pending.TargetClientId}: {pending.Message.Type}");
				}
				OnClientMessageReceived?.Invoke(pending.TargetClientId, pending.Message);
			}
		}

		public void Clear()
		{
			pendingMessages.Clear();
		}
	}
}