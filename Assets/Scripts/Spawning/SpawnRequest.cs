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
        bool hasLaneChangePlan,
        int targetLaneIndex,
        float laneChangeStartDelay,
        float laneChangeDuration,
        SpawnSafetyMode safetyMode = SpawnSafetyMode.Required)
        : base(SpawnRequestType.Traffic, spawnTime, true, safetyMode)
    {
        Prefab = prefab;
        LaneIndex = laneIndex;
        HasLaneChangePlan = hasLaneChangePlan;
        TargetLaneIndex = targetLaneIndex;
        LaneChangeStartDelay = laneChangeStartDelay;
        LaneChangeDuration = laneChangeDuration;

        TrafficVehicle trafficVehicle = prefab != null ? prefab.GetComponent<TrafficVehicle>() : null;
        Height = trafficVehicle != null ? trafficVehicle.GetHalfLength() * 2f : 0f;
        Speed = trafficVehicle != null ? trafficVehicle.GetFinalMoveSpeed() : 0f;
    }

    public GameObject Prefab { get; }
    public int LaneIndex { get; }
    public bool HasLaneChangePlan { get; }
    public int TargetLaneIndex { get; }
    public float LaneChangeStartDelay { get; }
    public float LaneChangeDuration { get; }
    public float Height { get; }
    public float Speed { get; }
}

public sealed class SideRoadSpawnRequest : SpawnRequestBase
{
    public SideRoadSpawnRequest(
        SideRoad prefab,
        SideRoadType sideRoadType,
        SideRoadDirection sideDirection,
        float spawnTime,
        float timeToPlayerArea,
        float width,
        float height,
        float speed,
        SpawnSafetyMode safetyMode = SpawnSafetyMode.Required)
        : base(SpawnRequestType.SideRoad, spawnTime, false, safetyMode)
    {
        Prefab = prefab;
        SideRoadType = sideRoadType;
        SideDirection = sideDirection;
        TimeToPlayerArea = timeToPlayerArea;
        Width = width;
        Height = height;
        Speed = speed;
    }

    public SideRoad Prefab { get; }
    public SideRoadType SideRoadType { get; }
    public SideRoadDirection SideDirection { get; }
    public float TimeToPlayerArea { get; }
    public float Width { get; }
    public float Height { get; }
    public float Speed { get; }
}
