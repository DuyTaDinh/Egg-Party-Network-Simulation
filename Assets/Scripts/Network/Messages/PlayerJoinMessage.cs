using System;
using Network.GameState;
namespace Network.Messages
{
	[Serializable]
	public class PlayerJoinMessage : NetworkMessage
	{
		public PlayerState Player { get; set; }

		public PlayerJoinMessage()
		{
			Type = MessageType.PlayerJoin;
		}
	}
}