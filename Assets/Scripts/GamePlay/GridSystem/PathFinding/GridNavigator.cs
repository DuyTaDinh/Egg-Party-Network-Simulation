using System.Collections.Generic;
using GridSystem.Core;
using UnityEngine;

namespace GridSystem.PathFinding
{
    public class GridNavigator : MonoBehaviour
    {
        public MapGridData mapData;
        public float moveSpeed = 5f;

        private GridPathfinder pathfinder;
        private List<Vector2Int> currentPath;
        private int pathIndex;
        private bool isMoving;

        void Start()
        {
            if (mapData)
            {
                pathfinder = new GridPathfinder(mapData);
            }
        }

        public void MoveTo(Vector2Int targetGrid)
        {
            if (pathfinder == null) return;

            var currentGrid = mapData.WorldToGrid(transform.position);
            currentPath = pathfinder.FindPath(currentGrid, targetGrid);

            if (currentPath is { Count: > 0 })
            {
                pathIndex = 0;
                isMoving = true;
            }
        }

        public void MoveTo(Vector3 worldPosition)
        {
            var targetGrid = mapData.WorldToGrid(worldPosition);
            MoveTo(targetGrid);
        }

        void Update()
        {
            if (!isMoving || currentPath == null || pathIndex >= currentPath.Count)
            {
                isMoving = false;
                return;
            }

            var targetWorld = mapData.GridToWorld(currentPath[pathIndex].x, currentPath[pathIndex].y);
            targetWorld.y = transform.position.y;

            transform.position = Vector3.MoveTowards(
                transform.position,
                targetWorld,
                moveSpeed * Time.deltaTime
            );

            if (Vector3.Distance(transform.position, targetWorld) < 0.1f)
            {
                pathIndex++;
            }
        }

        public bool HasReachedDestination()
        {
            return !isMoving;
        }

        void OnDrawGizmos()
        {
            if (currentPath == null || !mapData) return;

            Gizmos.color = Color.green;
            for (int i = 0; i < currentPath.Count - 1; i++)
            {
                var start = mapData.GridToWorld(currentPath[i].x, currentPath[i].y);
                var end = mapData.GridToWorld(currentPath[i + 1].x, currentPath[i + 1].y);
                start.y = 0.5f;
                end.y = 0.5f;
                Gizmos.DrawLine(start, end);
            }
        }
    }
}