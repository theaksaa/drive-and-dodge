using UnityEngine;

public class RoadSignSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LaneSystem laneSystem;

    [Header("Placement")]
    [SerializeField] private float spawnYExtraOffset = 1f;
    [SerializeField] private float despawnYExtraOffset = 1f;
    [SerializeField] private float horizontalOffsetFromSideCenter;

    private void Awake()
    {
        laneSystem ??= FindAnyObjectByType<LaneSystem>();
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
