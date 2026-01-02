using System;
using UnityEngine;
namespace Network.GameState
{
	[Serializable]
	public class EggState
	{
		public int EggId { get; set; }
		public Vector3 Position { get; set; }
		public Vector2Int GridPosition { get; set; }
		public Color EggColor { get; set; }
		public bool IsActive { get; set; }
	}
}