using System;
using System.Collections.Generic;
using UnityEngine;
namespace GamePlay.UserInput
{
	public class InputHandler
    {
        public event Action<Vector2Int> OnMoveInput;
        
        private readonly CommandBuffer commandBuffer;
        private readonly Dictionary<KeyCode, Vector2Int> keyBindings;
        private float inputCooldown = 0.15f;
        private float lastInputTime;
        private bool isEnabled = true;

        public InputHandler()
        {
            commandBuffer = new CommandBuffer();
            keyBindings = new Dictionary<KeyCode, Vector2Int>
            {
                { KeyCode.W, Vector2Int.up },
                { KeyCode.S, Vector2Int.down },
                { KeyCode.A, Vector2Int.left },
                { KeyCode.D, Vector2Int.right },
                { KeyCode.UpArrow, Vector2Int.up },
                { KeyCode.DownArrow, Vector2Int.down },
                { KeyCode.LeftArrow, Vector2Int.left },
                { KeyCode.RightArrow, Vector2Int.right }
            };
        }

        public void SetEnabled(bool enabled)
        {
            isEnabled = enabled;
        }

        public void SetInputCooldown(float cooldown)
        {
            inputCooldown = Mathf.Max(0, cooldown);
        }

        public void Update()
        {
            if (!isEnabled) return;

            if (Time.time - lastInputTime < inputCooldown) return;

            foreach (var binding in keyBindings)
            {
                if (UnityEngine.Input.GetKey(binding.Key))
                {
                    var command = new MoveCommand(binding.Value, ExecuteMove);
                    commandBuffer.AddCommand(command);
                    lastInputTime = Time.time;
                    break;
                }
            }

            commandBuffer.ExecuteAll();
        }

        private void ExecuteMove(Vector2Int direction)
        {
            OnMoveInput?.Invoke(direction);
        }

        public void RebindKey(KeyCode key, Vector2Int direction)
        {
            keyBindings[key] = direction;
        }

        public void ClearBindings()
        {
            keyBindings.Clear();
        }
    }
}