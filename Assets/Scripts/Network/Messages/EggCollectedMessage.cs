using System;
namespace Network.Messages
{
	[Serializable]
	public class EggCollectedMessage : NetworkMessage
	{
		public int EggId { get; set; }
		public int PlayerId { get; set; }
		public int NewEggCount { get; set; }

		public EggCollectedMessage()
		{
			Type = MessageType.EggCollected;
		}
	}
}