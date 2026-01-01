using System.Collections.Generic;
using UnityEngine;

namespace GridSystem.Core
{
    [CreateAssetMenu(fileName = "CellDatabase", menuName = "Grid System/Cell Database")]
    public class CellDatabase : ScriptableObject
    {
        public List<CellData> cells = new List<CellData>();
    
        public CellData GetCellData(CellType type)
        {
            return cells.Find(c => c.cellType == type);
        }
    
        public bool IsWalkable(CellType type)
        {
            var data = GetCellData(type);
            return data is { isWalkable: true };
        }
    }
}