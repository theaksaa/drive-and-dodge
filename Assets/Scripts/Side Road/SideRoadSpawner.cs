using UnityEngine;

public class SideRoadSpawner : MonoBehaviour
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

    private float timer;
    private float nextSpawnTime;

    private void Awake()
    {
        if (laneSystem == null)
            laneSystem = FindAnyObjectByType<LaneSystem>();
    }

    private void Start()
    {
        SetNextSpawnTime();
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;

        if (laneSystem == null || sideRoadPrefab == null)
            return;

        timer += Time.deltaTime;

        if (timer < nextSpawnTime)
            return;

        TrySpawnSideRoad();

        timer = 0f;
        SetNextSpawnTime();
    }

    private void TrySpawnSideRoad()
    {
        if (preventMultipleVisibleSideRoads && SideRoad.VisibleSideRoadCount > 0)
            return;

        SideRoadDirection direction = Random.value < 0.5f
            ? SideRoadDirection.Left
            : SideRoadDirection.Right;

        SpawnSideRoad(direction);
    }

    private void SpawnSideRoad(SideRoadDirection direction)
    {
        float centerX = laneSystem.GetSideRoadCenterX(direction);
        float width = laneSystem.GetSideRoadWidth(direction);

        if (width <= 0f)
        {
            Debug.LogWarning("SideRoadSpawner: Side road width is 0 or less. Check LaneSystem margins.");
            return;
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
    }

    private void SetNextSpawnTime()
    {
        nextSpawnTime = Random.Range(minSpawnInterval, maxSpawnInterval);
    }
}