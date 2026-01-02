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
            public Vector2Int Position;
            public float GCost;
            public float HCost;
            public float FCost => GCost + HCost;
            public PathNode Parent;
            public bool InOpenSet;
            public bool InClosedSet;

            public int CompareTo(PathNode other)
            {
                int compare = FCost.CompareTo(other.FCost);
                if (compare == 0)
                {
                    compare = HCost.CompareTo(other.HCost);
                }
                return compare;
            }
        }

        private readonly MapGridData gridData;
        private readonly Dictionary<Vector2Int, PathNode> nodeCache = new Dictionary<Vector2Int, PathNode>();

        private static readonly Vector2Int[] CardinalDirections =
        {
            new Vector2Int(0, 1),
            new Vector2Int(1, 0),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0),
        };

        public GridPathfinder(MapGridData mapData)
        {
            gridData = mapData;
        }

        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int end)
        {
            if (start == end)
            {
                return new List<Vector2Int> { start };
            }

            if (!gridData.IsValidPosition(end.x, end.y) || !gridData.IsWalkable(end.x, end.y))
            {
                return null;
            }

            if (!gridData.IsValidPosition(start.x, start.y))
            {
                return null;
            }

            nodeCache.Clear();

            var openSet = new List<PathNode>();
            
            var startNode = GetOrCreateNode(start);
            startNode.GCost = 0;
            startNode.HCost = CalculateHeuristic(start, end);
            startNode.InOpenSet = true;
            openSet.Add(startNode);

            int maxIterations = gridData.width * gridData.height * 2;
            int iterations = 0;

            while (openSet.Count > 0 && iterations < maxIterations)
            {
                iterations++;

                var current = GetLowestCostNode(openSet);
                
                if (current.Position == end)
                {
                    return ReconstructPath(current);
                }

                openSet.Remove(current);
                current.InOpenSet = false;
                current.InClosedSet = true;

                ProcessNeighbors(current, end, openSet);
            }

            return null;
        }

        public List<Vector2Int> FindPathAvoidingPositions(Vector2Int start, Vector2Int end, HashSet<Vector2Int> avoid)
        {
            if (start == end)
            {
                return new List<Vector2Int> { start };
            }

            if (!gridData.IsValidPosition(end.x, end.y) || !gridData.IsWalkable(end.x, end.y))
            {
                return null;
            }

            nodeCache.Clear();

            var openSet = new List<PathNode>();
            
            var startNode = GetOrCreateNode(start);
            startNode.GCost = 0;
            startNode.HCost = CalculateHeuristic(start, end);
            startNode.InOpenSet = true;
            openSet.Add(startNode);

            int maxIterations = gridData.width * gridData.height * 2;
            int iterations = 0;

            while (openSet.Count > 0 && iterations < maxIterations)
            {
                iterations++;

                var current = GetLowestCostNode(openSet);
                
                if (current.Position == end)
                {
                    return ReconstructPath(current);
                }

                openSet.Remove(current);
                current.InOpenSet = false;
                current.InClosedSet = true;

                ProcessNeighborsAvoiding(current, end, openSet, avoid);
            }

            return null;
        }

        private PathNode GetOrCreateNode(Vector2Int position)
        {
            if (!nodeCache.TryGetValue(position, out var node))
            {
                node = new PathNode
                {
                    Position = position,
                    GCost = float.MaxValue,
                    HCost = 0,
                    Parent = null,
                    InOpenSet = false,
                    InClosedSet = false
                };
                nodeCache[position] = node;
            }
            return node;
        }

        private PathNode GetLowestCostNode(List<PathNode> nodes)
        {
            PathNode lowest = nodes[0];
            for (int i = 1; i < nodes.Count; i++)
            {
                if (nodes[i].CompareTo(lowest) < 0)
                {
                    lowest = nodes[i];
                }
            }
            return lowest;
        }

        private void ProcessNeighbors(PathNode current, Vector2Int end, List<PathNode> openSet)
        {
            foreach (var dir in CardinalDirections)
            {
                var neighborPos = current.Position + dir;

                if (!gridData.IsWalkable(neighborPos.x, neighborPos.y))
                {
                    continue;
                }

                var neighbor = GetOrCreateNode(neighborPos);

                if (neighbor.InClosedSet)
                {
                    continue;
                }

                float newGCost = current.GCost + 1;

                if (newGCost < neighbor.GCost)
                {
                    neighbor.GCost = newGCost;
                    neighbor.HCost = CalculateHeuristic(neighborPos, end);
                    neighbor.Parent = current;

                    if (!neighbor.InOpenSet)
                    {
                        neighbor.InOpenSet = true;
                        openSet.Add(neighbor);
                    }
                }
            }
        }

        private void ProcessNeighborsAvoiding(PathNode current, Vector2Int end, List<PathNode> openSet, HashSet<Vector2Int> avoid)
        {
            foreach (var dir in CardinalDirections)
            {
                var neighborPos = current.Position + dir;

                if (!gridData.IsWalkable(neighborPos.x, neighborPos.y))
                {
                    continue;
                }

                var neighbor = GetOrCreateNode(neighborPos);

                if (neighbor.InClosedSet)
                {
                    continue;
                }

                float moveCost = 1f;
                if (avoid.Contains(neighborPos))
                {
                    moveCost += 10f;
                }

                float newGCost = current.GCost + moveCost;

                if (newGCost < neighbor.GCost)
                {
                    neighbor.GCost = newGCost;
                    neighbor.HCost = CalculateHeuristic(neighborPos, end);
                    neighbor.Parent = current;

                    if (!neighbor.InOpenSet)
                    {
                        neighbor.InOpenSet = true;
                        openSet.Add(neighbor);
                    }
                }
            }
        }

        private float CalculateHeuristic(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        private List<Vector2Int> ReconstructPath(PathNode endNode)
        {
            var path = new List<Vector2Int>();
            var current = endNode;

            while (current != null)
            {
                path.Add(current.Position);
                current = current.Parent;
            }

            path.Reverse();
            return path;
        }

        public bool HasPath(Vector2Int start, Vector2Int end)
        {
            return FindPath(start, end) != null;
        }

        public float GetPathLength(Vector2Int start, Vector2Int end)
        {
            var path = FindPath(start, end);
            return path?.Count - 1 ?? -1;
        }
    }
}