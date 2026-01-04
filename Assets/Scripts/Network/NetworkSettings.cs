using System;
using UnityEngine;
namespace Network
{
	[Serializable]
	public class NetworkSettings
	{
		[Header("Network Settings")]
		public NetworkMode networkMode = NetworkMode.Simulated;
		public bool debugMode = false;

		[Header("Transport")]
		public float minLatency = 0.05f;
		public float maxLatency = 0.15f;
		public float packetLossChance = 0f;

		[Header("Client")]
		public bool enablePrediction = true;
		public bool enableReconciliation = true;
		public bool enableInterpolation = true;
		public float interpolationDelay = 0.1f;
		
	}
}