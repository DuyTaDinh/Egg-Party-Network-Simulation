using System;

namespace GridSystem.Core
{
    [Serializable]
    public enum CellType
    {
        Empty = 0,
        Ground = 1,
        Wall = 2,
        Crate = 3,
        Rock = 4,
        Water = 5,
        
        SpawnPoint = 10,
        EggSpawnPoint = 11
    }
}