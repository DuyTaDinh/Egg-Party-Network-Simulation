using System;
using UnityEngine;
namespace Network.Messages
{
	[Serializable]
	public class InputMessage : NetworkMessage
	{
		public int PlayerId { get; set; }
		public Vector2Int MoveDirection { get; set; }
		public float ClientTimestamp { get; set; }

		public InputMessage()
		{
			Type = MessageType.Input;
		}
	}
}