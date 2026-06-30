using UnityEngine;

public enum SpawnRequestType
{
    Traffic,
    SideRoad
}

public enum SpawnSafetyMode
{
    Required,
    SkipValidation
}

public abstract class SpawnRequestBase : ISpawnRequest
{
    protected SpawnRequestBase(
        SpawnRequestType type,
        float spawnTime,
        bool blocksMovement,
        SpawnSafetyMode safetyMode)
    {
        Type = type;
        SpawnTime = spawnTime;
        BlocksMovement = blocksMovement;
        SafetyMode = safetyMode;
    }

    public SpawnRequestType Type { get; }
    public float SpawnTime { get; }
    public bool BlocksMovement { get; }
    public SpawnSafetyMode SafetyMode { get; }
}

public sealed class TrafficSpawnRequest : SpawnRequestBase
{
    public TrafficSpawnRequest(
        GameObject prefab,
        int laneIndex,
        float spawnTime,
        SpawnSafetyMode safetyMode = SpawnSafetyMode.Required)
        : base(SpawnRequestType.Traffic, spawnTime, true, safetyMode)
    {
        Prefab = prefab;
        LaneIndex = laneIndex;

        TrafficVehicle trafficVehicle = prefab != null ? prefab.GetComponent<TrafficVehicle>() : null;
        Height = trafficVehicle != null ? trafficVehicle.GetHalfLength() * 2f : 0f;
        Speed = trafficVehicle != null ? trafficVehicle.GetFinalMoveSpeed() : 0f;
    }

    public GameObject Prefab { get; }
    public int LaneIndex { get; }
    public float Height { get; }
    public float Speed { get; }
}

public sealed class SideRoadSpawnRequest : SpawnRequestBase
{
    public SideRoadSpawnRequest(
        SideRoadDirection sideDirection,
        float spawnTime,
        float timeToPlayerArea,
        float width,
        float height,
        float speed,
        SpawnSafetyMode safetyMode = SpawnSafetyMode.Required)
        : base(SpawnRequestType.SideRoad, spawnTime, false, safetyMode)
    {
        SideDirection = sideDirection;
        TimeToPlayerArea = timeToPlayerArea;
        Width = width;
        Height = height;
        Speed = speed;
    }

    public SideRoadDirection SideDirection { get; }
    public float TimeToPlayerArea { get; }
    public float Width { get; }
    public float Height { get; }
    public float Speed { get; }
}
