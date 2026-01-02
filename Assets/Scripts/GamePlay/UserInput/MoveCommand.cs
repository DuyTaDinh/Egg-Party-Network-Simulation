using System;
using UnityEngine;
namespace GamePlay.UserInput
{
	public class MoveCommand : ICommand
	{
		public Vector2Int Direction { get; }
		public float Timestamp { get; }
        
		private readonly Action<Vector2Int> onExecute;
		private readonly Action<Vector2Int> onUndo;

		public MoveCommand(Vector2Int direction, Action<Vector2Int> execute, Action<Vector2Int> undo = null)
		{
			Direction = direction;
			Timestamp = Time.time;
			onExecute = execute;
			onUndo = undo;
		}

		public void Execute()
		{
			onExecute?.Invoke(Direction);
		}

		public void Undo()
		{
			onUndo?.Invoke(-Direction);
		}
	}
}