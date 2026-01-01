using System;
using Player;
using Unity.VisualScripting;
using UnityEngine;

namespace DropSystem
{
    public class Egg : MonoBehaviour
    {
        [Header("Egg Properties")] public int eggId;

        private Vector2Int gridPosition;
        private bool isCollected = false;
        private EggData eggData;
        
        public Vector2Int GridPosition => gridPosition;
        public bool IsCollected => isCollected;
        public int Points => eggData.points;
        public void Initialize(int id,  Vector2Int gridPos, EggData eggData)
        {
            eggId = id;
            gridPosition = gridPos;
            this.eggData = eggData;
            isCollected = false;
        }

        public void Collect(PlayerController collector)
        {
            if (isCollected) return;

            isCollected = true;


            if (EggManager.Instance)
            {
                EggManager.Instance.OnEggCollected(this, collector);
            }


            Destroy(gameObject, 0.5f);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isCollected) return;

            var player = other.gameObject.GetComponent<PlayerController>();
            if (player)
            {
                Collect(player);
            }
        }

    }
}