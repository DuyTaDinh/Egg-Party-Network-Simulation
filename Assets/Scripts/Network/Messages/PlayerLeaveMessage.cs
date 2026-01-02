using System;
namespace Network.Messages
{
	[Serializable]
	public class PlayerLeaveMessage : NetworkMessage
	{
		public int PlayerId { get; set; }

		public PlayerLeaveMessage()
		{
			Type = MessageType.PlayerLeave;
		}
	}
}