using System.Collections.Generic;
using UnityEngine;

public class SpawnDirector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TrafficSpawner trafficSpawner;
    [SerializeField] private SideRoadSpawner sideRoadSpawner;
    [SerializeField] private SpawnSafetyPlanner safetyPlanner;
    [SerializeField] private PlayerLaneTracker playerLaneTracker;
    [SerializeField] private LaneSystem laneSystem;

    private float trafficTimer;
    private float sideRoadTimer;
    private float nextSideRoadSpawnTime;

    private ISpawnRequestSource<TrafficSpawnRequest> trafficRequestSource;
    private ISpawnExecutor<TrafficSpawnRequest> trafficExecutor;
    private ISpawnExecutor<SideRoadSpawnRequest> sideRoadExecutor;
    private ISpawnTimer sideRoadTimerSource;

    private void Awake()
    {
        trafficSpawner ??= FindAnyObjectByType<TrafficSpawner>();
        sideRoadSpawner ??= FindAnyObjectByType<SideRoadSpawner>();
        safetyPlanner ??= FindAnyObjectByType<SpawnSafetyPlanner>();
        playerLaneTracker ??= FindAnyObjectByType<PlayerLaneTracker>();
        laneSystem ??= FindAnyObjectByType<LaneSystem>();

        trafficRequestSource = trafficSpawner;
        trafficExecutor = trafficSpawner;
        sideRoadExecutor = sideRoadSpawner;
        sideRoadTimerSource = sideRoadSpawner;
    }

    private void Start()
    {
        nextSideRoadSpawnTime = sideRoadSpawner != null
            ? sideRoadTimerSource.GetNextSpawnDelay()
            : 0f;
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;

        if (!HasRequiredReferences())
            return;

        safetyPlanner.Tick(Time.time);

        UpdateTrafficSpawning();
        UpdateSideRoadSpawning();
    }

    private void UpdateTrafficSpawning()
    {
        trafficTimer += Time.deltaTime;

        if (trafficTimer < trafficSpawner.SpawnInterval)
            return;

        TrySpawnTraffic();
        trafficTimer = 0f;
    }

    private void UpdateSideRoadSpawning()
    {
        sideRoadTimer += Time.deltaTime;

        if (sideRoadTimer < nextSideRoadSpawnTime)
            return;

        TrySpawnSideRoad();
        sideRoadTimer = 0f;
        nextSideRoadSpawnTime = sideRoadTimerSource.GetNextSpawnDelay();
    }

    private void TrySpawnTraffic()
    {
        float spawnTime = Time.time;
        List<TrafficSpawnRequest> requests = trafficRequestSource.BuildSpawnRequests(spawnTime);
        Shuffle(requests);

        foreach (TrafficSpawnRequest request in requests)
        {
            if (safetyPlanner.ShouldValidate(request) &&
                !safetyPlanner.CanSpawnTraffic(request))
                continue;

            if (!trafficExecutor.ExecuteSpawn(request))
                continue;

            if (request.BlocksMovement)
                safetyPlanner.RegisterTrafficSpawn(request);

            return;
        }
    }

    private void TrySpawnSideRoad()
    {
        List<SideRoadDirection> directions = new List<SideRoadDirection>
        {
            SideRoadDirection.Left,
            SideRoadDirection.Right
        };

        Shuffle(directions);

        float spawnTime = Time.time;
        float scrollSpeed = GameManager.Instance != null ? GameManager.Instance.CurrentGameSpeed : 4f;

        foreach (SideRoadDirection direction in directions)
        {
            float timeToPlayerArea = sideRoadSpawner.GetTimeUntilReachingY(
                playerLaneTracker.transform.position.y,
                scrollSpeed);

            if (!sideRoadSpawner.TryCreateSpawnRequest(
                    direction,
                    spawnTime,
                    timeToPlayerArea,
                    scrollSpeed,
                    out SideRoadSpawnRequest request))
            {
                continue;
            }

            if (!sideRoadExecutor.CanExecuteSpawn(request))
                continue;

            bool requiresValidation = safetyPlanner.ShouldValidate(request);
            SpawnSafetyPlanner.SideRoadSpawnPlan plan = default;

            if (requiresValidation &&
                !safetyPlanner.TryCreateSideRoadPlan(request, out plan))
                continue;

            if (!sideRoadExecutor.ExecuteSpawn(request))
                continue;

            if (requiresValidation)
                safetyPlanner.RegisterSideRoadPlan(plan);

            return;
        }
    }

    private bool HasRequiredReferences()
    {
        return trafficSpawner != null &&
               sideRoadSpawner != null &&
               safetyPlanner != null &&
               playerLaneTracker != null &&
               laneSystem != null &&
               trafficRequestSource != null &&
               trafficExecutor != null &&
               sideRoadExecutor != null &&
               sideRoadTimerSource != null;
    }

    private void Shuffle<T>(List<T> values)
    {
        for (int i = 0; i < values.Count; i++)
        {
            int randomIndex = Random.Range(i, values.Count);
            T temp = values[i];
            values[i] = values[randomIndex];
            values[randomIndex] = temp;
        }
    }
}
