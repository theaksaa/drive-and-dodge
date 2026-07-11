# Road Event System

## Overview

`SpawnDirector` schedules gameplay events by distance travelled. An event supplies its own warning signs and knows how to execute itself when the player reaches the event distance.

Side roads are currently the only event implementation. They use the same generic system that future events such as potholes, roadworks, or lane closures will use.

## How an event runs

1. `SpawnDirector` chooses a compatible event source using its spawn weight.
2. The source creates an `IRoadEventPlan` containing the event data and warning profile.
3. The director reserves an event distance after the farthest warning.
4. Warning signs spawn when their distance milestones are reached.
5. At the event distance, the director calls `IRoadEventPlan.TryExecute()`.
6. After successful execution, the director plans the next event for that channel.

Distances use accumulated road travel, so warning spacing remains correct when game speed changes.

## Event channels

Events declare which channel they support:

- `Left` - an event on the left side.
- `Right` - an event on the right side.
- `FullRoad` - an event affecting the whole road.

Left and right are independent, so both sides can have separate events and warning sequences. The full-road channel remains inactive until a source supporting it is registered.

## Warning profiles

Every `RoadEventWarning` contains:

- `Distance Before Event`
- `Prefab`
- `Placement`

Placement options:

- `EventSide` - use the event's left or right side.
- `Left`
- `Right`
- `Both`

Full-road events cannot use `EventSide`; choose left, right, or both explicitly.

When `Require Signs` is enabled, the event is rejected if its warning list is empty or any sign prefab is missing.

## Current side-road setup

The `SideRoadEventSource` component is attached to `SideRoadSpawner`. It contains one profile for every `SideRoadType`, currently configured with signs at 500, 300, and 100 metres.

The source selects the side-road variant before the first warning appears. This keeps the selected side-road prefab, side, and warning sequence connected until the event executes.

To change side-road signs:

1. Select `SideRoadSpawner` in `GameScene`.
2. Open `Side Road Event Profiles`.
3. Find the required `Side Road Type`.
4. Add, remove, or edit its warning distances and prefabs.

## Adding a new event type

For an event such as a pothole zone:

1. Create a MonoBehaviour such as `PotholeEventSource` that implements `IRoadEventSource`.
2. Set its `SpawnWeight` and implement `SupportsSide()`.
3. In `TryCreatePlan()`, select/configure the event and return an `IRoadEventPlan` with its `RoadEventWarningProfile`.
4. In the plan's `TryExecute()`, start or spawn the actual event. The event itself can manage duration, such as potholes remaining active for 500 metres.
5. Add the source component to a scene GameObject.
6. Add that component to `SpawnDirector > Road Event Sources`.

The director then handles selection, distance scheduling, warning signs, side placement, and execution timing automatically.

## Main files

- `Assets/Scripts/Spawning/SpawnDirector.cs` - generic scheduler.
- `Assets/Scripts/Spawning/RoadEvents/RoadEventContracts.cs` - event interfaces, channels, and warning data.
- `Assets/Scripts/Side Road/SideRoadEventSource.cs` - side-road implementation and example for future sources.
- `Assets/Scripts/Signs/RoadSignSpawner.cs` - places and spawns roadside sign prefabs.
