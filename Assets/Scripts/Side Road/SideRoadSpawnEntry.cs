using UnityEngine;

[System.Serializable]
public class SideRoadSpawnEntry
{
    [SerializeField] private SideRoadType sideRoadType = SideRoadType.Default;
    [SerializeField] private SideRoad prefabOverride;

    [Min(0f)]
    [SerializeField] private float spawnWeight = 1f;

    public SideRoadType SideRoadType => sideRoadType;
    public SideRoad PrefabOverride => prefabOverride;
    public float SpawnWeight => spawnWeight;

    public bool IsValid(SideRoad fallbackPrefab)
    {
        return spawnWeight > 0f && ResolvePrefab(fallbackPrefab) != null;
    }

    public SideRoad ResolvePrefab(SideRoad fallbackPrefab)
    {
        return prefabOverride != null ? prefabOverride : fallbackPrefab;
    }
}
