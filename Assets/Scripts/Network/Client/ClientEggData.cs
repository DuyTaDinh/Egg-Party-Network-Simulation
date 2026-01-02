using UnityEngine;
namespace Network.Client
{
	public class ClientEggData
	{
		public int EggId;
		public Vector3 Position;
		public Vector2Int GridPosition;
		public Color EggColor;
		public bool IsActive;
		public float SpawnTime;
	}
}