using System.Collections.Generic;
using UnityEngine;

namespace GamePlay.AI
{
    public class ChaseState : BaseState
    {
        private List<Vector2Int> currentPath;
        private int pathIndex;
        private float repathTimer;
        private float repathInterval = 0.5f;

        public override void Enter()
        {
            base.Enter();
            CalculatePath();
        }

        private void CalculatePath()
        {
            var targetEgg = Controller.GetTargetEgg();
            if (targetEgg == null) return;

            currentPath = Controller.Pathfinder.FindPath(Controller.CurrentGridPosition, targetEgg.Value);
            pathIndex = currentPath != null && currentPath.Count > 0 ? 1 : 0;
        }

        public override void Execute(float deltaTime)
        {
            base.Execute(deltaTime);

            if (!Controller.HasTargetEgg())
            {
                Controller.ChangeState(new WanderState());
                return;
            }

            repathTimer += deltaTime;
            if (repathTimer >= repathInterval)
            {
                repathTimer = 0;
                CalculatePath();
            }

            if (Controller.CanMove())
            {
                MoveAlongPath();
            }
        }

        private void MoveAlongPath()
        {
            if (currentPath == null || pathIndex >= currentPath.Count)
            {
                CalculatePath();
                return;
            }

            var nextPos = currentPath[pathIndex];
            var direction = nextPos - Controller.CurrentGridPosition;

            if (direction.sqrMagnitude <= 2)
            {
                Controller.Move(new Vector2Int(
                    Mathf.Clamp(direction.x, -1, 1),
                    Mathf.Clamp(direction.y, -1, 1)
                ));
                
                if (Controller.CurrentGridPosition == nextPos || 
                    Vector2Int.Distance(Controller.CurrentGridPosition, nextPos) < 0.5f)
                {
                    pathIndex++;
                }
            }
            else
            {
                CalculatePath();
            }
        }
    }

}