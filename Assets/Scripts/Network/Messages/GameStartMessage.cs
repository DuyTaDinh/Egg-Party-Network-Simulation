using System;
using System.Collections.Generic;
using Network.GameState;
namespace Network.Messages
{
	[Serializable]
	public class GameStartMessage : NetworkMessage
	{
		public float GameDuration { get; set; }
		public List<PlayerState> InitialPlayers { get; set; }
		public int LocalPlayerId { get; set; }

		public GameStartMessage()
		{
			Type = MessageType.GameStart;
			InitialPlayers = new List<PlayerState>();
		}
	}
}