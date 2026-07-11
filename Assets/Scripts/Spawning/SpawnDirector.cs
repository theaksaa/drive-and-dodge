using System.Collections.Generic;
using UnityEngine;

public class SpawnDirector : MonoBehaviour
{
    private sealed class EventSlot
    {
        public EventSlot(RoadEventSide side)
        {
            Side = side;
        }

        public RoadEventSide Side { get; }
        public IRoadEventPlan Plan;
        public float EventDistance;
        public int NextWarningIndex;
    }

    private TrafficSpawner trafficSpawner;
    private RoadSignSpawner roadSignSpawner;
    private SpawnSafetyPlanner safetyPlanner;

    [Header("Road Event Scheduling")]
    [Tooltip("Extra distance after the farthest warning before a newly planned event.")]
    private float minEventLeadDistance = 50f;
    private float maxEventLeadDistance = 150f;

    private readonly EventSlot leftSlot = new EventSlot(RoadEventSide.Left);
    private readonly EventSlot rightSlot = new EventSlot(RoadEventSide.Right);
    private readonly EventSlot fullRoadSlot = new EventSlot(RoadEventSide.FullRoad);
    private readonly List<IRoadEventSource> eventSources = new List<IRoadEventSource>();

    private float trafficTimer;
    private float distanceTravelled;
    private bool hasFullRoadSources;
    private ISpawnRequestSource<TrafficSpawnRequest> trafficRequestSource;
    private ISpawnExecutor<TrafficSpawnRequest> trafficExecutor;

    private void Awake()
    {
        trafficSpawner ??= FindAnyObjectByType<TrafficSpawner>();
        roadSignSpawner ??= FindAnyObjectByType<RoadSignSpawner>();
        safetyPlanner ??= FindAnyObjectByType<SpawnSafetyPlanner>();

        trafficRequestSource = trafficSpawner;
        trafficExecutor = trafficSpawner;
        BuildEventSourceList();
    }

    private void Start()
    {
        TryPlanNextEvent(leftSlot);
        TryPlanNextEvent(rightSlot);

        if (hasFullRoadSources)
            TryPlanNextEvent(fullRoadSlot);
    }

    public void ApplyConfig(EnvironmentSpawnerConfig config)
    {
        if (config == null)
            return;

        minEventLeadDistance = Mathf.Max(0f, config.MinEventLeadDistance);
        maxEventLeadDistance = Mathf.Max(minEventLeadDistance, config.MaxEventLeadDistance);

        ReplanEvents();
    }

    private void ReplanEvents()
    {
        leftSlot.Plan = null;
        rightSlot.Plan = null;
        fullRoadSlot.Plan = null;

        TryPlanNextEvent(leftSlot);
        TryPlanNextEvent(rightSlot);

        if (hasFullRoadSources)
            TryPlanNextEvent(fullRoadSlot);
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;

        if (!HasRequiredReferences())
            return;

        safetyPlanner.Tick(Time.time);
        distanceTravelled += GetCurrentScrollSpeed() * Time.deltaTime;

        UpdateTrafficSpawning();
        UpdateEventSlot(leftSlot);
        UpdateEventSlot(rightSlot);

        if (hasFullRoadSources)
            UpdateEventSlot(fullRoadSlot);
    }

    private void BuildEventSourceList()
    {
        eventSources.Clear();
        hasFullRoadSources = false;

        MonoBehaviour[] sourceBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

        foreach (MonoBehaviour sourceBehaviour in sourceBehaviours)
        {
            if (sourceBehaviour is IRoadEventSource source)
            {
                eventSources.Add(source);
                hasFullRoadSources |= source.SupportsSide(RoadEventSide.FullRoad);
            }
        }
    }

    private void UpdateEventSlot(EventSlot slot)
    {
        if (slot.Plan == null)
        {
            TryPlanNextEvent(slot);
            return;
        }

        RoadEventWarning[] warnings = slot.Plan.WarningProfile?.warnings;

        while (warnings != null && slot.NextWarningIndex < warnings.Length)
        {
            RoadEventWarning warning = warnings[slot.NextWarningIndex];
            float warningMilestone = slot.EventDistance - warning.distanceBeforeEvent;

            if (distanceTravelled < warningMilestone)
                break;

            if (!TrySpawnWarning(warning, slot.Plan.Side))
            {
                slot.Plan = null;
                TryPlanNextEvent(slot);
                return;
            }

            slot.NextWarningIndex++;
        }

        if (distanceTravelled < slot.EventDistance)
            return;

        if (!slot.Plan.TryExecute())
            return;

        slot.Plan = null;
        TryPlanNextEvent(slot);
    }

    private bool TrySpawnWarning(RoadEventWarning warning, RoadEventSide eventSide)
    {
        if (warning == null || warning.prefab == null)
            return false;

        switch (warning.placement)
        {
            case RoadSignPlacement.Left:
                return roadSignSpawner.TrySpawn(warning.prefab, SideRoadDirection.Left);

            case RoadSignPlacement.Right:
                return roadSignSpawner.TrySpawn(warning.prefab, SideRoadDirection.Right);

            case RoadSignPlacement.Both:
                bool leftSpawned = roadSignSpawner.TrySpawn(warning.prefab, SideRoadDirection.Left);
                bool rightSpawned = roadSignSpawner.TrySpawn(warning.prefab, SideRoadDirection.Right);
                return leftSpawned && rightSpawned;

            case RoadSignPlacement.EventSide:
                if (eventSide == RoadEventSide.FullRoad)
                    return false;

                SideRoadDirection direction = eventSide == RoadEventSide.Left
                    ? SideRoadDirection.Left
                    : SideRoadDirection.Right;
                return roadSignSpawner.TrySpawn(warning.prefab, direction);

            default:
                return false;
        }
    }

    private void TryPlanNextEvent(EventSlot slot)
    {
        if (slot.Plan != null)
            return;

        List<IRoadEventSource> compatibleSources = new List<IRoadEventSource>();
        float totalWeight = 0f;

        foreach (IRoadEventSource source in eventSources)
        {
            if (source == null || source.SpawnWeight <= 0f || !source.SupportsSide(slot.Side))
                continue;

            compatibleSources.Add(source);
            totalWeight += source.SpawnWeight;
        }

        while (compatibleSources.Count > 0 && totalWeight > 0f)
        {
            IRoadEventSource selectedSource = SelectWeightedSource(compatibleSources, totalWeight);

            if (selectedSource.TryCreatePlan(slot.Side, out IRoadEventPlan plan) &&
                plan != null && plan.WarningProfile != null &&
                plan.WarningProfile.IsValidFor(plan.Side))
            {
                float minLead = Mathf.Min(minEventLeadDistance, maxEventLeadDistance);
                float maxLead = Mathf.Max(minEventLeadDistance, maxEventLeadDistance);

                plan.WarningProfile.SortFarthestFirst();
                slot.Plan = plan;
                slot.EventDistance = distanceTravelled +
                                     plan.WarningProfile.GetFarthestWarningDistance() +
                                     Random.Range(minLead, maxLead);
                slot.NextWarningIndex = 0;
                return;
            }

            totalWeight -= selectedSource.SpawnWeight;
            compatibleSources.Remove(selectedSource);
        }
    }

    private static IRoadEventSource SelectWeightedSource(
        List<IRoadEventSource> sources,
        float totalWeight)
    {
        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (IRoadEventSource source in sources)
        {
            currentWeight += source.SpawnWeight;

            if (randomValue <= currentWeight)
                return source;
        }

        return sources[sources.Count - 1];
    }

    private void UpdateTrafficSpawning()
    {
        trafficTimer += Time.deltaTime;

        if (trafficTimer < trafficSpawner.SpawnInterval)
            return;

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

            break;
        }

        trafficTimer = 0f;
    }

    private float GetCurrentScrollSpeed()
    {
        return GameManager.Instance != null ? GameManager.Instance.CurrentGameSpeed : 4f;
    }

    private bool HasRequiredReferences()
    {
        return trafficSpawner != null &&
               roadSignSpawner != null &&
               safetyPlanner != null &&
               trafficRequestSource != null &&
               trafficExecutor != null;
    }

    private static void Shuffle<T>(List<T> values)
    {
        for (int i = 0; i < values.Count; i++)
        {
            int randomIndex = Random.Range(i, values.Count);
            (values[i], values[randomIndex]) = (values[randomIndex], values[i]);
        }
    }
}
