using System.Collections.Generic;
using DropSystem;
using GamePlay;
using GridSystem.PathFinding;
using UnityEngine;

namespace Player
{
    public class BotPlayer : PlayerController
    {
        [Header("AI Settings")] 
        public float thinkInterval = 0.5f;
        public float moveDelay = 0.2f;
        public float randomWalkChangeInterval = 2f; 

        private GridPathfinder pathfinder;
        private List<Vector2Int> currentPath;
        private int pathIndex;
        private float lastThinkTime;
        private float lastMoveTime;
        private float lastRandomWalkTime;
        private Egg targetEgg;
        private bool isRandomWalking; 

        protected override void Start()
        {
            base.Start();

            pathfinder = new GridPathfinder(MapData);

            if (EggManager.Instance)
            {
                EggManager.Instance.OnEggSpawnedEvent += OnEggSpawned;
                EggManager.Instance.OnEggCollectedEvent += OnEggCollectedByAnyone;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (EggManager.Instance)
            {
                EggManager.Instance.OnEggSpawnedEvent -= OnEggSpawned;
                EggManager.Instance.OnEggCollectedEvent -= OnEggCollectedByAnyone;
            }
        }

        void OnEggSpawned(Egg egg)
        {
            if (isRandomWalking || !targetEgg || currentPath == null || currentPath.Count == 0)
            {
                isRandomWalking = false;
                FindNewTarget();
            }
        }

        void OnEggCollectedByAnyone(Egg egg, PlayerController collector)
        {
            if (targetEgg == egg)
            {
                targetEgg = null;
                currentPath = null;
                isRandomWalking = false; 
            }
        }

        protected override void Update()
        {
            base.Update();

            if (GameManager.Instance?.CurrentState != GameState.Playing)
                return;

            if (Time.time - lastThinkTime > thinkInterval)
            {
                lastThinkTime = Time.time;
                Think();
            }

            if (!isMoving && Time.time - lastMoveTime > moveDelay)
            {
                lastMoveTime = Time.time;
                
                if (isRandomWalking)
                {
                    PerformRandomWalk();
                }
                else
                {
                    MoveAlongPath();
                }
            }
        }

        void Think()
        {
            if (!targetEgg || targetEgg.IsCollected)
            {
                FindNewTarget();
                return;
            }

            if (currentPath == null || pathIndex >= currentPath.Count)
            {
                CalculatePathToTarget();
                return;
            }

            if (currentPath != null && currentPath.Count > 0)
            {
                var lastPos = currentPath[^1];
                if (targetEgg && lastPos != targetEgg.GridPosition)
                {
                    CalculatePathToTarget();
                }
            }
        }

        void FindNewTarget()
        {
            if (!EggManager.Instance) 
            {
                EnterRandomWalkMode();
                return;
            }

            // Try to find nearest egg
            targetEgg = EggManager.Instance.GetNearestEgg(currentGridPos);

            if (targetEgg)
            {
                isRandomWalking = false;
                CalculatePathToTarget();
            }
            else
            {
                EnterRandomWalkMode();
            }
        }

        void EnterRandomWalkMode()
        {
            isRandomWalking = true;
            targetEgg = null;
            currentPath = null;
            lastRandomWalkTime = Time.time;
        }

        void CalculatePathToTarget()
        {
            if (!targetEgg || targetEgg.IsCollected) 
            {
                FindNewTarget();
                return;
            }

            currentPath = pathfinder.FindPath(currentGridPos, targetEgg.GridPosition);
            pathIndex = 0;

            if (currentPath == null || currentPath.Count == 0)
            {
                targetEgg = null;
                FindNewTarget();
            }
        }

        void MoveAlongPath()
        {
            if (currentPath == null || pathIndex >= currentPath.Count)
            {
                if (!targetEgg || targetEgg.IsCollected)
                {
                    EnterRandomWalkMode();
                }
                return;
            }

            if (pathIndex < currentPath.Count && currentPath[pathIndex] == currentGridPos)
            {
                pathIndex++;
            }

            if (pathIndex >= currentPath.Count)
            {
                currentPath = null;
                return;
            }

            var nextPos = currentPath[pathIndex];
            var direction = nextPos - currentGridPos;

            if (TryMove(direction))
            {
                pathIndex++;
            }
            else
            {
                currentPath = null;
                
                if (targetEgg && !targetEgg.IsCollected)
                {
                    CalculatePathToTarget();
                }
                else
                {
                    FindNewTarget();
                }
            }
        }

        void PerformRandomWalk()
        {
            bool shouldChangeDirection = Time.time - lastRandomWalkTime > randomWalkChangeInterval;
            
            if (shouldChangeDirection)
            {
                lastRandomWalkTime = Time.time;
            }

            Vector2Int[] directions =
            {
                Vector2Int.up, 
                Vector2Int.down, 
                Vector2Int.left, 
                Vector2Int.right
            };

            for (int i = 0; i < directions.Length; i++)
            {
                int randomIndex = Random.Range(i, directions.Length);
                (directions[i], directions[randomIndex]) = (directions[randomIndex], directions[i]);
            }

            bool moved = false;
            foreach (var direction in directions)
            {
                if (TryMove(direction))
                {
                    moved = true;
                    break;
                }
            }

            if (!moved)
            {
                TryFindAlternativePath();
            }
        }

        void TryFindAlternativePath()
        {
            for (int attempt = 0; attempt < 10; attempt++)
            {
                int offsetX = Random.Range(-3, 4);
                int offsetY = Random.Range(-3, 4);
                
                Vector2Int targetPos = currentGridPos + new Vector2Int(offsetX, offsetY);
                
                if (MapData.IsWalkable(targetPos.x, targetPos.y))
                {
                    // Found a walkable position, create path to it
                    var path = pathfinder.FindPath(currentGridPos, targetPos);
                    
                    if (path != null && path.Count > 1)
                    {
                        currentPath = path;
                        pathIndex = 0;
                        isRandomWalking = false; // Temporarily follow this path
                        break;
                    }
                }
            }
        }

        void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            if (currentPath != null && MapData != null)
            {
                Gizmos.color = Color.yellow;
                for (int i = 0; i < currentPath.Count - 1; i++)
                {
                    var start = MapData.GridToWorld(currentPath[i].x, currentPath[i].y);
                    var end = MapData.GridToWorld(currentPath[i + 1].x, currentPath[i + 1].y);
                    start.y = 0.5f;
                    end.y = 0.5f;
                    Gizmos.DrawLine(start, end);
                }
            }

            if (targetEgg != null && MapData != null)
            {
                Gizmos.color = Color.red;
                var targetPos = MapData.GridToWorld(targetEgg.GridPosition.x, targetEgg.GridPosition.y);
                targetPos.y = 0.5f;
                Gizmos.DrawWireSphere(targetPos, 0.3f);
            }

            if (isRandomWalking && MapData != null)
            {
                Gizmos.color = Color.cyan;
                var botPos = MapData.GridToWorld(currentGridPos.x, currentGridPos.y);
                botPos.y = 1f;
                Gizmos.DrawWireCube(botPos, Vector3.one * 0.5f);
            }
        }
    }
}