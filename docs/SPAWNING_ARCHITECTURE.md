# Spawning Architecture

## Goal
All spawn decisions go through one central system so we can avoid unfair situations and keep future spawn types easy to add.

Current supported spawn types:
- Traffic
- SideRoad

Main idea:
- `SpawnDirector` decides when to try spawns
- request objects describe what wants to spawn
- `SpawnSafetyPlanner` decides if validation is needed and if the spawn is safe
- `SpawnReservationMap` remembers future blocked/reserved lane windows
- concrete spawners only execute the spawn

## Main Files
- [Assets/Scripts/Spawning/SpawnDirector.cs](../Assets/Scripts/Spawning/SpawnDirector.cs)
- [Assets/Scripts/Spawning/SpawnSafetyPlanner.cs](../Assets/Scripts/Spawning/SpawnSafetyPlanner.cs)
- [Assets/Scripts/Spawning/SpawnReservationMap.cs](../Assets/Scripts/Spawning/SpawnReservationMap.cs)
- [Assets/Scripts/Spawning/ISpawnContracts.cs](../Assets/Scripts/Spawning/ISpawnContracts.cs)
- [Assets/Scripts/Spawning/SpawnRequest.cs](../Assets/Scripts/Spawning/SpawnRequest.cs)
- [Assets/Scripts/Traffic/TrafficSpawner.cs](../Assets/Scripts/Traffic/TrafficSpawner.cs)
- [Assets/Scripts/Side Road/SideRoadSpawner.cs](../Assets/Scripts/Side%20Road/SideRoadSpawner.cs)

## SpawnDirector
`SpawnDirector` is the brain.

Responsibilities:
- keeps spawn timers
- asks sources for spawn requests
- decides whether safety validation is required
- asks `SpawnSafetyPlanner` for approval when needed
- calls the correct spawner executor
- registers reservations after successful spawns

Current flow:

### Traffic flow
1. Traffic timer reaches interval.
2. `TrafficSpawner` builds `TrafficSpawnRequest` objects.
3. `SpawnDirector` shuffles requests.
4. If request safety mode is `Required`, `SpawnSafetyPlanner` checks it.
5. If accepted, `TrafficSpawner` executes spawn.
6. If the spawn blocks movement, planner registers its blocked window.

### SideRoad flow
1. Side road timer reaches interval.
2. `SpawnDirector` tries left/right in random order.
3. It builds a `SideRoadSpawnRequest`.
4. If request safety mode is `Required`, planner checks if a corridor to that side can stay valid.
5. If accepted, `SideRoadSpawner` executes spawn.
6. Planner reserves the corridor lanes for that side road window.

## Safety System
`SpawnSafetyPlanner` is the fairness layer.

It currently does 3 important things:
- checks if traffic would create impossible movement
- checks if traffic conflicts with reserved lanes
- checks if a side road can stay reachable from the player’s current lane

Important rule:
- the spawner does not decide fairness
- the planner does

## Reservation System
`SpawnReservationMap` stores future lane promises.

Current reservation kinds:
- `Blocked`: traffic is expected to occupy a lane near the player
- `KeepClear`: a lane must stay free, usually for a side road corridor

This is how different spawn types can work together without custom hardcoded exceptions.

## Contracts
Defined in `ISpawnContracts.cs`.

### `ISpawnRequest`
Shared request data:
- type
- spawn time
- whether it blocks movement
- safety mode

### `ISpawnRequestSource<TRequest>`
Used by systems that generate candidate requests.

Current example:
- `TrafficSpawner` builds traffic requests

### `ISpawnExecutor<TRequest>`
Used by systems that actually instantiate the spawn.

Current examples:
- `TrafficSpawner`
- `SideRoadSpawner`

### `ISpawnTimer`
Used for spawners that own random or configurable spawn delay rules.

Current example:
- `SideRoadSpawner`

## Request Types
Defined in `SpawnRequest.cs`.

Current request classes:
- `TrafficSpawnRequest`
- `SideRoadSpawnRequest`

Each request is specific to one spawn type. This is better than one giant universal request struct because future spawn types can carry only the data they actually need.

## Safety Modes
Also defined in `SpawnRequest.cs`.

Current modes:
- `Required`
- `SkipValidation`

Use them like this:
- `Required`: traffic, roadblocks, obstacles, things that can affect fairness
- `SkipValidation`: decorative signs, overhead props, non-collidable visuals

Important:
- `SkipValidation` means the request can bypass fairness/path validation
- it does not automatically mean the request blocks something
- it does not automatically create reservations

## Current Spawners

### TrafficSpawner
What it does:
- picks weighted traffic prefabs
- builds lane-based traffic requests
- executes a chosen traffic spawn

What it does not do anymore:
- fairness logic
- timing logic

### SideRoadSpawner
What it does:
- checks if a side road can be executed
- spawns a side road left or right
- provides the next random delay
- provides travel time to player area

What it does not do anymore:
- fairness logic
- central timing logic

## How to Create a New Spawner
Use this pattern:

1. Create a new request class in `SpawnRequest.cs`
   Example: `SignSpawnRequest`

2. Decide its safety mode
   Example:
   - collidable obstacle -> `Required`
   - decorative sign -> `SkipValidation`

3. Create a new spawner component
   Usually implement:
   - `ISpawnExecutor<TRequest>`
   - optionally `ISpawnRequestSource<TRequest>`
   - optionally `ISpawnTimer`

4. Add safety behavior only if needed
   If the spawn affects movement, add planner logic and reservation logic.

5. Connect it in `SpawnDirector`
   Director should:
   - build the request
   - ask whether validation is needed
   - validate if needed
   - execute spawn
   - register reservations if needed

## Example Mental Model
Think in this order:
- "What wants to spawn?" -> request
- "Does it need fairness validation?" -> safety mode
- "Can it happen safely?" -> planner
- "What future space does it occupy or reserve?" -> reservation map
- "Actually create it" -> executor

## What Changed In This Refactor
- moved spawn decisions into `SpawnDirector`
- removed fairness logic from individual spawners
- removed old `TrafficSpawnPlanner`
- added typed spawn requests
- added interface-based contracts for future spawners
- added safety policy with `Required` and `SkipValidation`
- added reservation-based coordination between traffic and side roads

## Rule To Keep
When adding new spawn types, do not put fairness rules inside the spawner.

Keep this split:
- director = orchestration
- planner = safety/fairness
- reservation map = future space ownership
- spawner = execution only
