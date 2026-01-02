using System;
using System.Collections.Generic;
using Network.GameState;

namespace Network.Messages
{
	[Serializable]
	public class WorldStateMessage : NetworkMessage
	{
		public List<PlayerState> Players { get; set; }
		public List<EggState> Eggs { get; set; }
		public float GameTime { get; set; }
		public float RemainingTime { get; set; }

		public WorldStateMessage()
		{
			Type = MessageType.WorldState;
			Players = new List<PlayerState>();
			Eggs = new List<EggState>();
		}
	}
}