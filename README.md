# Egg Collector Game - Multiplayer Server Simulation

A Unity multiplayer egg collection game with server simulation architecture, supporting prediction, reconciliation, and interpolation.

## Architecture Overview

This game follows a client-server architecture pattern with a simulated server running locally:

```
┌──────────────────────────────────────────────────────────────┐
│                         CLIENT                               │
│  ┌─────────────┐  ┌───────────────┐  ┌───────────────────┐   │
│  │ InputHandler│→ │ClientSimulator│→ │   PlayerView/     │   │
│  │  (Commands) │  │  Prediction   │  │   EggView/UI      │   │
│  └─────────────┘  │ Reconciliation│  └───────────────────┘   │
│                   │ Interpolation │                          │
│                   └───────────────┘                          │
└──────────────────────────────────────────────────────────────┘
                           ↕ Messages (via NetworkTransport)
┌──────────────────────────────────────────────────────────────┐
│                        SERVER                                │
│  ┌───────────────────────────────────────────────────────┐   │
│  │                 ServerSimulator                       │   │
│  │  • Authoritative game state                           │   │
│  │  • Random update intervals (0.1-0.5s)                 │   │
│  │  • Input processing & validation                      │   │
│  │  • Egg spawning & collision detection                 │   │
│  │  • Winner determination                               │   │
│  └───────────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────────────┘
```

## Key Features

### Network Simulation
- **Simulated Latency**: Configurable min/max latency (50-500ms)
- **Packet Loss**: Configurable packet loss simulation
- **Random Update Intervals**: Server sends updates at random 0.1-0.5s intervals
- **Message-Based Communication**: Easy to switch to real network later

### Client-Side Techniques
- **Prediction**: Local player inputs applied immediately
- **Reconciliation**: Server state synced with pending input replay
- **Interpolation**: Remote players rendered with smooth interpolation

### AI System
- **State Machine**: Idle → Wander → Chase states
- **Custom Pathfinding**: A* algorithm (no external libraries)
- **Smart Targeting**: Bots avoid competing for same eggs

### Input System
- **Command Pattern**: All inputs wrapped as commands
- **Input Buffer**: History for undo capability
- **Configurable Keys**: WASD and Arrow keys

## Controls

| Key | Action |
|-----|--------|
| W / ↑ | Move Up |
| S / ↓ | Move Down |
| A / ← | Move Left |
| D / → | Move Right |
| F1 | Toggle Debug Panel |


## Configuration Constants

| Setting | Default | Location |
|---------|---------|----------|
| Player Count | 4 | GameManager |
| Game Duration | 60s | GameManager |
| Min Latency | 50ms | NetworkManager |
| Max Latency | 150ms | NetworkManager |
| Update Interval | 0.1-0.5s | ServerSimulator |
| Max Eggs | 5 | ServerSimulator |
| Egg Spawn Interval | 3s | ServerSimulator |
| Player Speed | 4 units/s | ServerSimulator |
| Bot Move Cooldown | 0.25s | AIController |
| Interpolation Delay | 100ms | ClientSimulator |

## Requirements

- Support Unity Version: 6.3LTS and 2022.3LTS with their respective brands.
- No external packages required
- All pathfinding is custom-built