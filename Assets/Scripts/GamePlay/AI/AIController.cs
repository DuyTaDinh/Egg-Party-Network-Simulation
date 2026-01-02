using System;
using GridSystem.Core;
using GridSystem.PathFinding;
using UnityEngine;

namespace GamePlay.AI
{
	public class AIController
	{
		public event Action<Vector2Int> OnMoveRequested;

		private IState currentState;
		private readonly GridPathfinder pathfinder;
		private readonly MapGridData mapData;

		private Vector2Int currentGridPosition;
		private Vector2Int? targetEggPosition;
		private float moveTimer;
		private float moveCooldown = 0.25f;
		private readonly int playerId;

		public MapGridData MapData => mapData;
		public GridPathfinder Pathfinder => pathfinder;
		public Vector2Int CurrentGridPosition => currentGridPosition;

		public AIController(int id, MapGridData map, GridPathfinder finder)
		{
			playerId = id;
			mapData = map;
			pathfinder = finder;
			ChangeState(new IdleState());
		}

		public void Update(float deltaTime)
		{
			moveTimer += deltaTime;
			currentState?.Execute(deltaTime);
		}

		public void ChangeState(IState newState)
		{
			if (currentState != null && !currentState.CanTransitionTo(newState))
			{
				return;
			}

			currentState?.Exit();
			currentState = newState;

			if (newState is BaseState baseState)
			{
				baseState.SetController(this);
			}

			currentState.Enter();
		}

		public void SetPosition(Vector2Int gridPos)
		{
			currentGridPosition = gridPos;
		}

		public void SetTargetEgg(Vector2Int? eggPos)
		{
			targetEggPosition = eggPos;
		}

		public bool HasTargetEgg()
		{
			return targetEggPosition.HasValue;
		}

		public Vector2Int? GetTargetEgg()
		{
			return targetEggPosition;
		}

		public bool CanMove()
		{
			return moveTimer >= moveCooldown;
		}

		public void Move(Vector2Int direction)
		{
			if (!CanMove()) return;
			if (direction == Vector2Int.zero) return;

			direction = new Vector2Int(
				Mathf.Clamp(direction.x, -1, 1),
				Mathf.Clamp(direction.y, -1, 1)
			);

			if (Mathf.Abs(direction.x) > 0 && Mathf.Abs(direction.y) > 0)
			{
				direction = Mathf.Abs(direction.x) >= Mathf.Abs(direction.y)
					? new Vector2Int(direction.x, 0)
					: new Vector2Int(0, direction.y);
			}

			var targetPos = currentGridPosition + direction;

			if (mapData.IsWalkable(targetPos.x, targetPos.y))
			{
				moveTimer = 0;
				OnMoveRequested?.Invoke(direction);
			}
		}

		public IState CurrentState => currentState;
	}
}