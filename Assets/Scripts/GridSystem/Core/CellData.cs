using System;
using UnityEngine;

namespace GridSystem.Core
{
    [Serializable]
    public class CellData
    {
        public CellType cellType;
        public string cellName;
        public GameObject prefab;
        public bool isWalkable = true;
        public Color editorColor = Color.white;
        public Sprite editorIcon;
    }
}