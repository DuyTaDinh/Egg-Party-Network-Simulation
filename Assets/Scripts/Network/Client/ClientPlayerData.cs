using System.Collections.Generic;
using Network.Messages;
using UnityEngine;
namespace Network.Client
{
	public class ClientPlayerData
	{
		public int PlayerId;
		public Vector3 Position;
		public Vector3 InterpolatedPosition;
		public Vector2Int GridPosition;
		public int EggCount;
		public Color PlayerColor;
		public bool IsMoving;
		public Vector2Int MoveDirection;
		public bool IsLocal;
        
		public Queue<InterpolationState> InterpolationBuffer = new Queue<InterpolationState>();
		public List<InputMessage> PendingInputs = new List<InputMessage>();
		public Vector3 PredictedPosition;
		public Vector2Int PredictedGridPosition;
	}
}