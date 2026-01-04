using System;
using System.Collections.Generic;
using UnityEngine;
using Network.Messages;
using Network.Transport;
using GridSystem.Core;
using Network.GameState;

namespace Network.Client
{
    public class ClientSimulator : IClient
    {
        public event Action OnGameStarted;
        public event Action OnGameEnded;
        public event Action<ClientPlayerData> OnPlayerUpdated;
        public event Action<ClientEggData> OnEggSpawned;
        public event Action<int, int> OnEggCollected;
        public event Action<float> OnTimeUpdated;
        public event Action<int, int> OnScoreUpdated;

        private readonly MapGridData mapData;
        private readonly Dictionary<int, ClientPlayerData> players = new Dictionary<int, ClientPlayerData>();
        private readonly Dictionary<int, ClientEggData> eggs = new Dictionary<int, ClientEggData>();

        private int localPlayerId = -1;
        private float gameTime;
        private float remainingTime;
        private bool isGameRunning;
        private float interpolationDelay = 0.1f;
        private float playerMoveSpeed = 4f;

        private uint inputSequenceNumber;
        private float lastServerTimestamp;

        public bool EnablePrediction { get; set; } = true;
        public bool EnableReconciliation { get; set; } = true;
        public bool EnableInterpolation { get; set; } = true;
        public bool DebugMode { get; set; } = false;
        public float InterpolationDelay
        {
            get
            {
                return interpolationDelay;
            }
            set
            {
                interpolationDelay = Mathf.Max(0.05f, value);
            }
        }

        public ClientSimulator(MapGridData map)
        {
            mapData = map;
        }

        public void ProcessMessage(NetworkMessage message)
        {
            switch (message.Type)
            {
                case MessageType.GameStart:
                    HandleGameStart((GameStartMessage)message);
                    break;
                case MessageType.WorldState:
                    HandleWorldState((WorldStateMessage)message);
                    break;
                case MessageType.EggSpawn:
                    HandleEggSpawn((EggSpawnMessage)message);
                    break;
                case MessageType.EggCollected:
                    HandleEggCollected((EggCollectedMessage)message);
                    break;
                case MessageType.GameEnd:
                    HandleGameEnd((GameEndMessage)message);
                    break;
            }
        }

        private void HandleGameStart(GameStartMessage msg)
        {
            players.Clear();
            eggs.Clear();
            localPlayerId = msg.LocalPlayerId;
            remainingTime = msg.GameDuration;
            isGameRunning = true;
            inputSequenceNumber = 0;

            foreach (var playerState in msg.InitialPlayers)
            {
                var player = new ClientPlayerData
                {
                    PlayerId = playerState.PlayerId,
                    Position = playerState.Position,
                    InterpolatedPosition = playerState.Position,
                    PredictedPosition = playerState.Position,
                    GridPosition = playerState.GridPosition,
                    PredictedGridPosition = playerState.GridPosition,
                    EggCount = playerState.EggCount,
                    PlayerColor = playerState.PlayerColor,
                    IsLocal = playerState.PlayerId == localPlayerId
                };
                players[playerState.PlayerId] = player;
            }

            if (DebugMode)
            {
                Debug.Log($"[Client] Game started. Local player: {localPlayerId}");
            }

            OnGameStarted?.Invoke();
        }

        private void HandleWorldState(WorldStateMessage msg)
        {
            lastServerTimestamp = msg.Timestamp;
            remainingTime = msg.RemainingTime;
            OnTimeUpdated?.Invoke(remainingTime);

            foreach (var playerState in msg.Players)
            {
                if (!players.TryGetValue(playerState.PlayerId, out var player))
                {
                    player = new ClientPlayerData
                    {
                        PlayerId = playerState.PlayerId,
                        IsLocal = playerState.PlayerId == localPlayerId
                    };
                    players[playerState.PlayerId] = player;
                }

                if (player.IsLocal)
                {
                    HandleLocalPlayerUpdate(player, playerState);
                }
                else
                {
                    HandleRemotePlayerUpdate(player, playerState);
                }

                if (player.EggCount != playerState.EggCount)
                {
                    player.EggCount = playerState.EggCount;
                    OnScoreUpdated?.Invoke(player.PlayerId, player.EggCount);
                }
            }

            var activeEggIds = new HashSet<int>();
            foreach (var eggState in msg.Eggs)
            {
                activeEggIds.Add(eggState.EggId);
                if (!eggs.ContainsKey(eggState.EggId))
                {
                    var egg = new ClientEggData
                    {
                        EggId = eggState.EggId,
                        Position = eggState.Position,
                        GridPosition = eggState.GridPosition,
                        EggColor = eggState.EggColor,
                        IsActive = true,
                        SpawnTime = Time.time
                    };
                    eggs[eggState.EggId] = egg;
                    OnEggSpawned?.Invoke(egg);
                }
            }

            var toRemove = new List<int>();
            foreach (var kvp in eggs)
            {
                if (!activeEggIds.Contains(kvp.Key))
                {
                    toRemove.Add(kvp.Key);
                }
            }
            foreach (var id in toRemove)
            {
                eggs.Remove(id);
            }
        }

        private void HandleLocalPlayerUpdate(ClientPlayerData player, PlayerState serverState)
        {
            if (!EnableReconciliation)
            {
                player.Position = serverState.Position;
                player.GridPosition = serverState.GridPosition;
                return;
            }

            player.PendingInputs.RemoveAll(input => input.SequenceNumber <= serverState.LastProcessedInput);

            player.Position = serverState.Position;
            player.GridPosition = serverState.GridPosition;
            player.PredictedPosition = serverState.Position;
            player.PredictedGridPosition = serverState.GridPosition;

            foreach (var input in player.PendingInputs)
            {
                ApplyInputPrediction(player, input);
            }

            if (DebugMode && player.PendingInputs.Count > 0)
            {
                Debug.Log($"[Client] Reconciled {player.PendingInputs.Count} pending inputs");
            }
        }

        private void HandleRemotePlayerUpdate(ClientPlayerData player, PlayerState serverState)
        {
            if (EnableInterpolation)
            {
                player.InterpolationBuffer.Enqueue(new InterpolationState
                {
                    Position = serverState.Position,
                    Timestamp = Time.time
                });

                while (player.InterpolationBuffer.Count > 20)
                {
                    player.InterpolationBuffer.Dequeue();
                }
            }
            else
            {
                player.Position = serverState.Position;
                player.InterpolatedPosition = serverState.Position;
            }

            player.GridPosition = serverState.GridPosition;
            player.IsMoving = serverState.IsMoving;
            player.MoveDirection = serverState.MoveDirection;
        }

        private void HandleEggSpawn(EggSpawnMessage msg)
        {
            if (eggs.ContainsKey(msg.EggId)) return;

            var egg = new ClientEggData
            {
                EggId = msg.EggId,
                Position = msg.Position,
                GridPosition = msg.GridPosition,
                EggColor = msg.EggColor,
                IsActive = true,
                SpawnTime = Time.time
            };
            eggs[msg.EggId] = egg;
            OnEggSpawned?.Invoke(egg);

            if (DebugMode)
            {
                Debug.Log($"[Client] Egg spawned: {msg.EggId} at {msg.GridPosition}");
            }
        }

        private void HandleEggCollected(EggCollectedMessage msg)
        {
            if (eggs.TryGetValue(msg.EggId, out var egg))
            {
                egg.IsActive = false;
            }

            if (players.TryGetValue(msg.PlayerId, out var player))
            {
                player.EggCount = msg.NewEggCount;
                OnScoreUpdated?.Invoke(player.PlayerId, player.EggCount);
            }

            OnEggCollected?.Invoke(msg.EggId, msg.PlayerId);

            if (DebugMode)
            {
                Debug.Log($"[Client] Player {msg.PlayerId} collected egg {msg.EggId}");
            }
        }

        private void HandleGameEnd(GameEndMessage msg)
        {
            isGameRunning = false;

            foreach (var score in msg.FinalScores)
            {
                if (players.TryGetValue(score.PlayerId, out var player))
                {
                    player.EggCount = score.EggCount;
                }
            }

            if (DebugMode)
            {
                Debug.Log($"[Client] Game ended. Winner: Player {msg.WinnerId}");
            }

            OnGameEnded?.Invoke();
        }

        public InputMessage CreateInput(Vector2Int direction)
        {
            var input = new InputMessage
            {
                PlayerId = localPlayerId,
                MoveDirection = direction,
                ClientTimestamp = Time.time,
                SequenceNumber = ++inputSequenceNumber
            };

            if (EnablePrediction && players.TryGetValue(localPlayerId, out var localPlayer))
            {
                localPlayer.PendingInputs.Add(input);
                ApplyInputPrediction(localPlayer, input);
            }

            return input;
        }

        private void ApplyInputPrediction(ClientPlayerData player, InputMessage input)
        {
            if (input.MoveDirection == Vector2Int.zero) return;

            var targetGrid = player.PredictedGridPosition + input.MoveDirection;

            if (mapData.IsWalkable(targetGrid.x, targetGrid.y))
            {
                player.PredictedGridPosition = targetGrid;
                player.PredictedPosition = mapData.GridToWorld(targetGrid.x, targetGrid.y);
            }
        }

        public void Update(float deltaTime)
        {
            if (!isGameRunning) return;

            gameTime += deltaTime;

            foreach (var player in players.Values)
            {
                if (player.IsLocal)
                {
                    UpdateLocalPlayer(player, deltaTime);
                }
                else
                {
                    UpdateRemotePlayer(player, deltaTime);
                }

                OnPlayerUpdated?.Invoke(player);
            }
        }

        private void UpdateLocalPlayer(ClientPlayerData player, float deltaTime)
        {
            if (EnablePrediction)
            {
                player.Position = Vector3.Lerp(player.Position, player.PredictedPosition, deltaTime * 15f);
            }
        }

        private void UpdateRemotePlayer(ClientPlayerData player, float deltaTime)
        {
            if (!EnableInterpolation || player.InterpolationBuffer.Count < 2)
            {
                return;
            }

            float renderTime = Time.time - interpolationDelay;

            var buffer = player.InterpolationBuffer.ToArray();
            
            InterpolationState from = null;
            InterpolationState to = null;

            for (int i = 0; i < buffer.Length - 1; i++)
            {
                if (buffer[i].Timestamp <= renderTime && buffer[i + 1].Timestamp >= renderTime)
                {
                    from = buffer[i];
                    to = buffer[i + 1];
                    break;
                }
            }

            if (from != null && to != null)
            {
                float t = (renderTime - from.Timestamp) / (to.Timestamp - from.Timestamp);
                player.InterpolatedPosition = Vector3.Lerp(from.Position, to.Position, t);
                player.Position = player.InterpolatedPosition;
            }
            else if (buffer.Length > 0)
            {
                var latest = buffer[^1];
                player.Position = Vector3.Lerp(player.Position, latest.Position, deltaTime * 10f);
                player.InterpolatedPosition = player.Position;
            }
        }

        public ClientPlayerData GetLocalPlayer()
        {
            return players.GetValueOrDefault(localPlayerId);
        }

        public ClientPlayerData GetPlayer(int playerId)
        {
            return players.GetValueOrDefault(playerId);
        }

        public IEnumerable<ClientPlayerData> GetAllPlayers()
        {
            return players.Values;
        }

        public IEnumerable<ClientEggData> GetAllEggs()
        {
            return eggs.Values;
        }

        public bool IsRunning => isGameRunning;
        public float RemainingTime => remainingTime;
        public int LocalPlayerId => localPlayerId;
    }
}
