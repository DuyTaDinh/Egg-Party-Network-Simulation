using System;
using System.Collections.Generic;
using UnityEngine;

namespace GridSystem.Core
{
    [Serializable]
    public class GridCell
    {
        public CellType cellType;
        public Vector2Int position;
    }


    [CreateAssetMenu(fileName = "MapGridData", menuName = "Grid System/Map Grid Data")]
    public class MapGridData : ScriptableObject
    {
        public int width = 19;
        public int height = 13;
        public float cellSize = 1f;
        public Vector3 worldOffset = Vector3.zero;

        [SerializeField] private CellType[] cells;

        public CellDatabase cellDatabase;

        public void Initialize()
        {
            if (cells == null || cells.Length != width * height)
            {
                cells = new CellType[width * height];
            }
        }

        public CellType GetCell(int x, int y)
        {
            if (!IsValidPosition(x, y)) return CellType.Empty;
            return cells[y * width + x];
        }

        public void SetCell(int x, int y, CellType type)
        {
            if (!IsValidPosition(x, y)) return;
            cells[y * width + x] = type;
        }

        public bool IsValidPosition(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }

        public bool IsWalkable(int x, int y)
        {
            if (!IsValidPosition(x, y)) return false;
            var cellType = GetCell(x, y);
            return cellDatabase != null && cellDatabase.IsWalkable(cellType);
        }

        public List<Vector2Int> GetWalkableCells()
        {
            var walkableCells = new List<Vector2Int>();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (IsWalkable(x, y))
                    {
                        walkableCells.Add(new Vector2Int(x, y));
                    }
                }
            }
            return walkableCells;
        }

        public Vector3 GridToWorld(int x, int y)
        {
            return new Vector3(x * cellSize, 0, y * cellSize) + worldOffset;
        }

        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            var localPos = worldPos - worldOffset;
            return new Vector2Int(
                Mathf.RoundToInt(localPos.x / cellSize),
                Mathf.RoundToInt(localPos.z / cellSize)
            );
        }

        public void Clear()
        {
            if (cells != null)
            {
                Array.Clear(cells, 0, cells.Length);
            }
        }

        public void Resize(int newWidth, int newHeight)
        {
            var newCells = new CellType[newWidth * newHeight];

            if (cells != null)
            {
                for (int y = 0; y < Mathf.Min(height, newHeight); y++)
                {
                    for (int x = 0; x < Mathf.Min(width, newWidth); x++)
                    {
                        newCells[y * newWidth + x] = cells[y * width + x];
                    }
                }
            }

            width = newWidth;
            height = newHeight;
            cells = newCells;
        }

        public List<Vector2Int> GetCellsOfType(CellType type)
        {
            var result = new List<Vector2Int>();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (GetCell(x, y) == type)
                    {
                        result.Add(new Vector2Int(x, y));
                    }
                }
            }

            return result;
        }
    }
}