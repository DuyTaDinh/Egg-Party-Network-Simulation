using System.Collections.Generic;
using Network.Messages;
using UnityEngine;
namespace Network.Server
{
	public class ServerPlayerData
	{
		public int PlayerId;
		public Vector3 Position;
		public Vector2Int GridPosition;
		public Vector2Int TargetGridPosition;
		public int EggCount;
		public Color PlayerColor;
		public bool IsMoving;
		public Vector2Int MoveDirection;
		public float MoveProgress;
		public uint LastProcessedInputSequence;
		public Queue<InputMessage> PendingInputs = new Queue<InputMessage>();
	}
}