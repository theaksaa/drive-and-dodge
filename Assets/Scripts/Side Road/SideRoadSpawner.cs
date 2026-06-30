using UnityEngine;

public class SideRoadSpawner : MonoBehaviour, ISpawnExecutor<SideRoadSpawnRequest>, ISpawnTimer
{
    [Header("References")]
    [SerializeField] private LaneSystem laneSystem;
    [SerializeField] private SideRoad sideRoadPrefab;

    [Header("Spawning")]
    [SerializeField] private float minSpawnInterval = 5f;
    [SerializeField] private float maxSpawnInterval = 9f;
    [SerializeField] private float spawnYExtraOffset = 1.5f;
    [SerializeField] private float despawnYExtraOffset = 1.5f;
    [SerializeField] private bool preventMultipleVisibleSideRoads = true;

    [Header("Side Road")]
    [SerializeField] private float sideRoadHeight = 2.5f;

    [Tooltip("Koliko trigger ulazi preko cyan linije ka glavnom putu.")]
    [SerializeField] private float triggerOverlapIntoMainRoad = 0.5f;

    public float SideRoadHeight => sideRoadHeight;

    private void Awake()
    {
        if (laneSystem == null)
            laneSystem = FindAnyObjectByType<LaneSystem>();
    }

    public bool CanExecuteSpawn(SideRoadSpawnRequest request)
    {
        if (request == null)
            return false;

        if (laneSystem == null || sideRoadPrefab == null)
            return false;

        if (preventMultipleVisibleSideRoads && SideRoad.VisibleSideRoadCount > 0)
            return false;

        return true;
    }

    public bool ExecuteSpawn(SideRoadSpawnRequest request)
    {
        if (!CanExecuteSpawn(request))
            return false;

        SideRoadDirection direction = request.SideDirection;
        float centerX = laneSystem.GetSideRoadCenterX(direction);
        float width = laneSystem.GetSideRoadWidth(direction);

        if (width <= 0f)
        {
            Debug.LogWarning("SideRoadSpawner: Side road width is 0 or less. Check LaneSystem margins.");
            return false;
        }

        float spawnY = laneSystem.GetTopY() + sideRoadHeight / 2f + spawnYExtraOffset;
        float despawnY = laneSystem.GetBottomY() - sideRoadHeight / 2f - despawnYExtraOffset;

        Vector3 spawnPosition = new Vector3(centerX, spawnY, 0f);

        SideRoad sideRoad = Instantiate(sideRoadPrefab, spawnPosition, Quaternion.identity);

        sideRoad.Setup(
            direction,
            despawnY,
            width,
            sideRoadHeight,
            triggerOverlapIntoMainRoad
        );

        return true;
    }

    public float GetNextSpawnDelay()
    {
        return Random.Range(minSpawnInterval, maxSpawnInterval);
    }

    public float GetTimeUntilReachingY(float worldY, float moveSpeed)
    {
        if (laneSystem == null || moveSpeed <= 0f)
            return float.PositiveInfinity;

        float spawnY = laneSystem.GetTopY() + sideRoadHeight / 2f + spawnYExtraOffset;
        return Mathf.Max(0f, (spawnY - worldY) / moveSpeed);
    }
}
