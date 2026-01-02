using System;
using UnityEngine;
namespace Network.Messages
{
	[Serializable]
	public class EggSpawnMessage : NetworkMessage
	{
		public int EggId { get; set; }
		public Vector3 Position { get; set; }
		public Vector2Int GridPosition { get; set; }
		public Color EggColor { get; set; }

		public EggSpawnMessage()
		{
			Type = MessageType.EggSpawn;
		}
	}
}