using DropSystem;
using GamePlay;
using GridSystem.Core;
using GridSystem.PathFinding;
using UnityEngine;

namespace Player
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Player Info")] public int playerId;
        public string playerName;
        public MapGridData MapData => StageManager.Instance.mapData;

        [Header("Movement")] public float moveSpeed = 5f;

        protected Vector2Int currentGridPos;
        protected Vector3 targetWorldPos;
        protected bool isMoving = false;
        protected int score = 0;

        public int Score => score;
        public Vector2Int GridPosition => currentGridPos;
        public bool IsMoving => isMoving;

        protected virtual void Start()
        {

            if (EggManager.Instance)
            {
                EggManager.Instance.OnEggCollectedEvent += OnEggCollected;
            }
        }

        protected virtual void OnDestroy()
        {
            if (EggManager.Instance)
            {
                EggManager.Instance.OnEggCollectedEvent -= OnEggCollected;
            }
        }

        protected virtual void Update()
        {
            if (isMoving)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetWorldPos,
                    moveSpeed * Time.deltaTime
                );

                if (Vector3.Distance(transform.position, targetWorldPos) < 0.01f)
                {
                    transform.position = targetWorldPos;
                    isMoving = false;
                    OnReachedDestination();
                }
            }
        }

        protected virtual void OnReachedDestination()
        {
        }

        protected bool TryMove(Vector2Int direction)
        {
            if (isMoving) return false;

            Vector2Int targetPos = currentGridPos + direction;

            if (MapData.IsWalkable(targetPos.x, targetPos.y))
            {
                currentGridPos = targetPos;
                targetWorldPos = MapData.GridToWorld(currentGridPos.x, currentGridPos.y);
                isMoving = true;

                return true;
            }

            return false;
        }


        void OnEggCollected(Egg egg, PlayerController collector)
        {
            if (collector == this)
            {
                score += egg.Points;
                OnScoreChanged();
            }
        }

        protected virtual void OnScoreChanged()
        {
            
        }

        public void SetGridPosition(Vector2Int gridPos)
        {
            currentGridPos = gridPos;
            targetWorldPos = MapData.GridToWorld(gridPos.x, gridPos.y);
            transform.position = targetWorldPos;
        }
    }
}