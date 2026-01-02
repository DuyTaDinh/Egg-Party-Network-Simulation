using System.Collections.Generic;
using UnityEngine;

namespace GamePlay.AI
{
	public class WanderState : BaseState
	{
		private Vector2Int targetPosition;
		private List<Vector2Int> currentPath;
		private int pathIndex;
		private float wanderDuration;
		private float maxWanderTime = 5f;

		public override void Enter()
		{
			base.Enter();
			wanderDuration = UnityEngine.Random.Range(2f, maxWanderTime);
			ChooseRandomTarget();
		}

		private void ChooseRandomTarget()
		{
			var walkableCells = Controller.MapData.GetWalkableCells();
			if (walkableCells.Count == 0) return;

			int attempts = 0;
			do
			{
				targetPosition = walkableCells[UnityEngine.Random.Range(0, walkableCells.Count)];
				attempts++;
			} while (targetPosition == Controller.CurrentGridPosition && attempts < 10);

			currentPath = Controller.Pathfinder.FindPath(Controller.CurrentGridPosition, targetPosition);
			pathIndex = 0;
		}

		public override void Execute(float deltaTime)
		{
			base.Execute(deltaTime);

			if (Controller.HasTargetEgg())
			{
				Controller.ChangeState(new ChaseState());
				return;
			}

			if (StateTime >= wanderDuration || currentPath == null || pathIndex >= currentPath.Count)
			{
				Controller.ChangeState(new IdleState());
				return;
			}

			if (Controller.CanMove())
			{
				MoveAlongPath();
			}
		}

		private void MoveAlongPath()
		{
			if (currentPath == null || pathIndex >= currentPath.Count) return;

			var nextPos = currentPath[pathIndex];
			var direction = nextPos - Controller.CurrentGridPosition;

			if (direction.magnitude <= 1)
			{
				Controller.Move(direction);
				pathIndex++;
			}
			else
			{
				pathIndex++;
			}
		}
	}
}