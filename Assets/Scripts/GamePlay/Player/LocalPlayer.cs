using GamePlay;
using UnityEngine;

namespace Player
{
    public class LocalPlayer : PlayerController
    {
        [Header("Input")] public KeyCode upKey = KeyCode.UpArrow;
        public KeyCode downKey = KeyCode.DownArrow;
        public KeyCode leftKey = KeyCode.LeftArrow;
        public KeyCode rightKey = KeyCode.RightArrow;

        protected override void Update()
        {
            base.Update();
            HandleInput();
        }

        void HandleInput()
        {
            if (GameManager.Instance?.CurrentState != GameState.Playing)
                return;

            if (isMoving) return;

            Vector2Int direction = Vector2Int.zero;

            if (Input.GetKeyDown(upKey))
                direction = Vector2Int.up;
            else if (Input.GetKeyDown(downKey))
                direction = Vector2Int.down;
            else if (Input.GetKeyDown(leftKey))
                direction = Vector2Int.left;
            else if (Input.GetKeyDown(rightKey))
                direction = Vector2Int.right;

            if (direction != Vector2Int.zero)
            {
                TryMove(direction);
            }
        }
    }
}