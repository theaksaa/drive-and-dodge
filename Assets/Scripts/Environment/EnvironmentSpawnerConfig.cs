using System;
using UnityEngine;

[CreateAssetMenu(fileName = "SpawnerConfig", menuName = "Drive And Dodge/Environment Spawner Config")]
public class EnvironmentSpawnerConfig : ScriptableObject
{
    [Serializable]
    public class TrafficEntry
    {
        public GameObject prefab;
        [Min(0f)] public float spawnWeight = 1f;
    }

    [Header("Traffic Spawner - Complete Config")]
    [SerializeField] private TrafficEntry[] trafficPrefabs;
    [Min(0.01f)] [SerializeField] private float trafficSpawnInterval = 1.1f;

    [Header("Side Road Spawner - Complete Config")]
    [SerializeField] private SideRoad sideRoadPrefab;
    [SerializeField] private SideRoadSpawnEntry[] sideRoadVariants;
    [Min(0f)] [SerializeField] private float sideRoadMinSpawnInterval = 5f;
    [Min(0f)] [SerializeField] private float sideRoadMaxSpawnInterval = 9f;
    [Min(0f)] [SerializeField] private float sideRoadSpawnYExtraOffset = 1.5f;
    [Min(0f)] [SerializeField] private float sideRoadDespawnYExtraOffset = 1.5f;
    [SerializeField] private bool preventMultipleVisibleSideRoads = true;
    [Min(0.01f)] [SerializeField] private float sideRoadHeight = 2.5f;
    [Min(0f)] [SerializeField] private float sideRoadTriggerOverlap = 0.5f;

    [Header("Road Sign Spawner - Complete Config")]
    [Min(0f)] [SerializeField] private float roadSignSpawnYExtraOffset = 1f;
    [Min(0f)] [SerializeField] private float roadSignDespawnYExtraOffset = 1f;
    [SerializeField] private float roadSignHorizontalOffsetFromSideCenter;

    [Header("Spawn Director - Complete Config")]
    [Min(0f)] [SerializeField] private float minEventLeadDistance = 50f;
    [Min(0f)] [SerializeField] private float maxEventLeadDistance = 150f;

    [Header("Side Road Event Source - Complete Config")]
    [Min(0f)] [SerializeField] private float sideRoadEventSpawnWeight = 1f;
    [Tooltip("Each side-road type owns its complete warning-sign sequence in this environment.")]
    [SerializeField] private SideRoadEventSource.SideRoadEventProfile[] sideRoadEventProfiles;

    public TrafficEntry[] TrafficPrefabs => trafficPrefabs;
    public float TrafficSpawnInterval => trafficSpawnInterval;
    public SideRoad SideRoadPrefab => sideRoadPrefab;
    public SideRoadSpawnEntry[] SideRoadVariants => sideRoadVariants;
    public float SideRoadMinSpawnInterval => sideRoadMinSpawnInterval;
    public float SideRoadMaxSpawnInterval => sideRoadMaxSpawnInterval;
    public float SideRoadSpawnYExtraOffset => sideRoadSpawnYExtraOffset;
    public float SideRoadDespawnYExtraOffset => sideRoadDespawnYExtraOffset;
    public bool PreventMultipleVisibleSideRoads => preventMultipleVisibleSideRoads;
    public float SideRoadHeight => sideRoadHeight;
    public float SideRoadTriggerOverlap => sideRoadTriggerOverlap;
    public float RoadSignSpawnYExtraOffset => roadSignSpawnYExtraOffset;
    public float RoadSignDespawnYExtraOffset => roadSignDespawnYExtraOffset;
    public float RoadSignHorizontalOffsetFromSideCenter => roadSignHorizontalOffsetFromSideCenter;
    public float MinEventLeadDistance => minEventLeadDistance;
    public float MaxEventLeadDistance => maxEventLeadDistance;
    public float SideRoadEventSpawnWeight => sideRoadEventSpawnWeight;
    public SideRoadEventSource.SideRoadEventProfile[] SideRoadEventProfiles => sideRoadEventProfiles;

    private void OnValidate()
    {
        sideRoadMaxSpawnInterval = Mathf.Max(sideRoadMinSpawnInterval, sideRoadMaxSpawnInterval);
        maxEventLeadDistance = Mathf.Max(minEventLeadDistance, maxEventLeadDistance);
    }
}
