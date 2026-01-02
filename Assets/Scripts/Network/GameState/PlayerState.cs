using System;
using UnityEngine;
namespace Network.GameState
{
	[Serializable]
	public class PlayerState
	{
		public int PlayerId { get; set; }
		public Vector3 Position { get; set; }
		public Vector2Int GridPosition { get; set; }
		public int EggCount { get; set; }
		public Color PlayerColor { get; set; }
		public bool IsMoving { get; set; }
		public Vector2Int MoveDirection { get; set; }
		public uint LastProcessedInput { get; set; }
	}
}