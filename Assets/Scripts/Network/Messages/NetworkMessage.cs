using System;
namespace Network.Messages
{
	[Serializable]
	public class NetworkMessage
	{
		public MessageType Type { get; protected set; }
		public float Timestamp { get; set; }
		public uint SequenceNumber { get; set; }
	}
}