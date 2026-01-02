using System;
using System.Collections.Generic;
using Network.GameState;
namespace Network.Messages
{
	[Serializable]
	public class GameEndMessage : NetworkMessage
	{
		public int WinnerId { get; set; }
		public List<PlayerState> FinalScores { get; set; }

		public GameEndMessage()
		{
			Type = MessageType.GameEnd;
			FinalScores = new List<PlayerState>();
		}
	}
}