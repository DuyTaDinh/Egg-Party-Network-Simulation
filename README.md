# Egg Party - Quick Overview

Lightweight grid-based egg-collecting game simulation.

Quick summary
- `GameManager`: game lifecycle (spawn players, game loop, time, state, events).
- `StageManager` + `MapGridData`: map data (ScriptableObject) — grid size, cell types, Grid↔World conversions, walkability checks.
- `EggManager`: spawn and manage eggs, fire events on spawn/collect, configurable spawn rules.
- `PlayerController`: base for players and bots; handles movement interpolation, score, and event hooks.
- `LocalPlayer` / `BotPlayer`: input and AI implementations on top of `PlayerController`.

Data model
- `MapGridData`: width, height, cellSize, cell array, `IsWalkable`, `GridToWorld` / `WorldToGrid`.
- `EggData`: prefab, points, spawn weight.

Controls (local player)
- Arrow keys or WASD move the local player on the grid. Default WASD mapping:
  - W / UpArrow: move up
  - S / DownArrow: move down
  - A / LeftArrow: move left
  - D / RightArrow: move right

AI / Pathfinding 
- Bots query `EggManager` for targets (e.g. nearest egg) and request a path on the grid.
- Pathfinding uses the grid data (walkability from `MapGridData`) and the project's pathfinding module (`GridSystem.PathFinding`) to compute a waypoint list (A* or similar). Bots follow waypoints by setting `targetWorldPos` and using the `PlayerController` movement interpolation.

Unfinished: server simulator
- Purpose: authoritative game host that validates moves, resolves conflicts (simultaneous pickups/collisions), spawns eggs and broadcasts the authoritative state to clients.
- Responsibilities: accept player inputs, validate against `MapGridData`, advance game tick, run egg spawn logic (same rules as `EggManager`), apply scoring, and emit state snapshots.
- Follow the concepts in Gabriel Gambetta's [Fast-Paced Multiplayer](http://www.gabrielgambetta.com/client-server-game-architecture.html) series
- Notes: This is not implemented in the repo yet. Implement server simulator as a single-scene authoritative host for local multiplayer testing; later extract to a networked server by replacing local broadcasts with network messages.
