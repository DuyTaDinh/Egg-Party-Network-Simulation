using System;
using UnityEngine;

namespace DropSystem
{
    [Serializable]
    public class EggData
    {
        public EggType type;
        public int points;
        public GameObject prefab;
        public float spawnWeight = 1f; 
    }

    public enum EggType
    {
        Bronze = 0,   
        Silver = 1,  
        Gold = 2     
    }
}