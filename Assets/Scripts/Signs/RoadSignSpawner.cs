using UnityEngine;

public class RoadSignSpawner : MonoBehaviour
{
    private LaneSystem laneSystem;

    [Header("Placement")]
    private float spawnYExtraOffset = 1f;
    private float despawnYExtraOffset = 1f;
    private float horizontalOffsetFromSideCenter;

    private void Awake()
    {
        laneSystem ??= FindAnyObjectByType<LaneSystem>();
    }

    public void ApplyConfig(EnvironmentSpawnerConfig config)
    {
        if (config == null)
            return;

        spawnYExtraOffset = Mathf.Max(0f, config.RoadSignSpawnYExtraOffset);
        despawnYExtraOffset = Mathf.Max(0f, config.RoadSignDespawnYExtraOffset);
        horizontalOffsetFromSideCenter = config.RoadSignHorizontalOffsetFromSideCenter;
    }

    public bool TrySpawn(RoadSign prefab, SideRoadDirection direction)
    {
        if (laneSystem == null || prefab == null)
            return false;

        float directionSign = direction == SideRoadDirection.Left ? -1f : 1f;
        float spawnX = laneSystem.GetSideRoadCenterX(direction) +
                       horizontalOffsetFromSideCenter * directionSign;
        float spawnY = laneSystem.GetTopY() + spawnYExtraOffset;
        float despawnY = laneSystem.GetBottomY() - despawnYExtraOffset;

        RoadSign sign = Instantiate(prefab, new Vector3(spawnX, spawnY, 0f), Quaternion.identity);
        sign.Setup(despawnY);
        return true;
    }
}
