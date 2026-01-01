using System;
using System.Collections.Generic;
using GridSystem.Core;
using UnityEngine;

namespace GridSystem.PathFinding
{
    public class GridPathfinder
    {
        private class PathNode : IComparable<PathNode>
        {
            public Vector2Int position;
            public float gCost;
            public float hCost;
            public float fCost => gCost + hCost;
            public PathNode parent;

            public int CompareTo(PathNode other)
            {
                return fCost.CompareTo(other.fCost);
            }
        }

        private MapGridData mapData;

        private static readonly Vector2Int[] directions =
        {
            new Vector2Int(0, 1), // Up
            new Vector2Int(1, 0), // Right
            new Vector2Int(0, -1), // Down
            new Vector2Int(-1, 0), // Left
        };

        public GridPathfinder(MapGridData data)
        {
            mapData = data;
        }

        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int end)
        {
            if (!mapData.IsWalkable(end.x, end.y))
                return null;

            var openSet = new List<PathNode>();
            var closedSet = new HashSet<Vector2Int>();
            var allNodes = new Dictionary<Vector2Int, PathNode>();

            var startNode = new PathNode
            {
                position = start,
                gCost = 0,
                hCost = GetDistance(start, end)
            };

            openSet.Add(startNode);
            allNodes[start] = startNode;

            while (openSet.Count > 0)
            {
                openSet.Sort();
                var current = openSet[0];
                openSet.RemoveAt(0);

                if (current.position == end)
                {
                    return ReconstructPath(current);
                }

                closedSet.Add(current.position);

                foreach (var dir in directions)
                {
                    var neighborPos = current.position + dir;

                    if (!mapData.IsWalkable(neighborPos.x, neighborPos.y))
                        continue;

                    if (closedSet.Contains(neighborPos))
                        continue;

                    float newGCost = current.gCost + 1;

                    if (!allNodes.TryGetValue(neighborPos, out var neighbor))
                    {
                        neighbor = new PathNode
                        {
                            position = neighborPos,
                            hCost = GetDistance(neighborPos, end)
                        };
                        allNodes[neighborPos] = neighbor;
                    }

                    if (newGCost < neighbor.gCost || !openSet.Contains(neighbor))
                    {
                        neighbor.gCost = newGCost;
                        neighbor.parent = current;

                        if (!openSet.Contains(neighbor))
                        {
                            openSet.Add(neighbor);
                        }
                    }
                }
            }

            return null;
        }

        private float GetDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        private List<Vector2Int> ReconstructPath(PathNode endNode)
        {
            var path = new List<Vector2Int>();
            var current = endNode;

            while (current != null)
            {
                path.Add(current.position);
                current = current.parent;
            }

            path.Reverse();
            return path;
        }
    }
}