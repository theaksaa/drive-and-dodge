using UnityEngine;

public class SideRoadSpawner : MonoBehaviour, ISpawnExecutor<SideRoadSpawnRequest>, ISpawnTimer
{
    private static readonly SideRoadType[] DefaultSideRoadTypes =
    {
        SideRoadType.Default,
        SideRoadType.GasStation,
        SideRoadType.RepairService,
        SideRoadType.Highway,
        SideRoadType.Forest
    };

    [Header("References")]
    [SerializeField] private LaneSystem laneSystem;
    [SerializeField] private SideRoad sideRoadPrefab;

    [Header("Variants")]
    [SerializeField] private SideRoadSpawnEntry[] sideRoadVariants;

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

        if (laneSystem == null || request.Prefab == null)
            return false;

        if (preventMultipleVisibleSideRoads)
        {
            bool sameSideAlreadyVisible = request.SideDirection == SideRoadDirection.Left
                ? SideRoad.VisibleLeftSideRoadCount > 0
                : SideRoad.VisibleRightSideRoadCount > 0;

            if (sameSideAlreadyVisible)
                return false;
        }

        return true;
    }

    public bool TryCreateSpawnRequest(
        SideRoadDirection direction,
        float spawnTime,
        float timeToPlayerArea,
        float moveSpeed,
        out SideRoadSpawnRequest request)
    {
        request = null;

        if (laneSystem == null)
            return false;

        if (!TryGetRandomVariant(out SideRoadType sideRoadType, out SideRoad prefab))
            return false;

        return TryCreateSpawnRequest(
            sideRoadType,
            prefab,
            direction,
            spawnTime,
            timeToPlayerArea,
            moveSpeed,
            out request);
    }

    public bool TryCreateSpawnRequest(
        SideRoadType sideRoadType,
        SideRoad prefab,
        SideRoadDirection direction,
        float spawnTime,
        float timeToPlayerArea,
        float moveSpeed,
        out SideRoadSpawnRequest request)
    {
        request = null;

        if (laneSystem == null || prefab == null)
            return false;

        float width = laneSystem.GetSideRoadWidth(direction);

        request = new SideRoadSpawnRequest(
            prefab,
            sideRoadType,
            direction,
            spawnTime,
            timeToPlayerArea,
            width,
            sideRoadHeight,
            moveSpeed);

        return true;
    }

    public bool TrySelectRandomVariant(out SideRoadType sideRoadType, out SideRoad prefab)
    {
        return TryGetRandomVariant(out sideRoadType, out prefab);
    }

    public bool ExecuteSpawn(SideRoadSpawnRequest request)
    {
        if (!CanExecuteSpawn(request))
            return false;

        SideRoadDirection direction = request.SideDirection;
        float centerX = laneSystem.GetSideRoadCenterX(direction);
        float width = request.Width;

        if (width <= 0f)
        {
            Debug.LogWarning("SideRoadSpawner: Side road width is 0 or less. Check LaneSystem margins.");
            return false;
        }

        float spawnY = laneSystem.GetTopY() + request.Height / 2f + spawnYExtraOffset;
        float despawnY = laneSystem.GetBottomY() - request.Height / 2f - despawnYExtraOffset;

        Vector3 spawnPosition = new Vector3(centerX, spawnY, 0f);

        SideRoad sideRoad = Instantiate(request.Prefab, spawnPosition, Quaternion.identity);

        sideRoad.Setup(
            request.SideRoadType,
            direction,
            despawnY,
            width,
            request.Height,
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

    private bool TryGetRandomVariant(out SideRoadType sideRoadType, out SideRoad prefab)
    {
        sideRoadType = SideRoadType.Default;
        prefab = null;

        if (TryGetConfiguredVariant(out SideRoadSpawnEntry selectedEntry))
        {
            sideRoadType = selectedEntry.SideRoadType;
            prefab = selectedEntry.ResolvePrefab(sideRoadPrefab);
            return true;
        }

        if (sideRoadPrefab == null || DefaultSideRoadTypes.Length == 0)
            return false;

        sideRoadType = DefaultSideRoadTypes[Random.Range(0, DefaultSideRoadTypes.Length)];
        prefab = sideRoadPrefab;
        return true;
    }

    private bool TryGetConfiguredVariant(out SideRoadSpawnEntry selectedEntry)
    {
        selectedEntry = null;

        if (sideRoadVariants == null || sideRoadVariants.Length == 0)
            return false;

        float totalWeight = 0f;

        foreach (SideRoadSpawnEntry entry in sideRoadVariants)
        {
            if (entry == null || !entry.IsValid(sideRoadPrefab))
                continue;

            totalWeight += entry.SpawnWeight;
        }

        if (totalWeight <= 0f)
            return false;

        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (SideRoadSpawnEntry entry in sideRoadVariants)
        {
            if (entry == null || !entry.IsValid(sideRoadPrefab))
                continue;

            currentWeight += entry.SpawnWeight;

            if (randomValue <= currentWeight)
            {
                selectedEntry = entry;
                return true;
            }
        }

        return false;
    }
}
