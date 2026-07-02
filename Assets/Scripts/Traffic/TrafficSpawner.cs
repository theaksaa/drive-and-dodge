using System.Collections.Generic;
using UnityEngine;

public class TrafficSpawner : MonoBehaviour, ISpawnRequestSource<TrafficSpawnRequest>, ISpawnExecutor<TrafficSpawnRequest>
{
    [System.Serializable]
    private class TrafficSpawnEntry
    {
        public GameObject prefab;

        [Min(0f)]
        public float spawnWeight = 1f;
    }

    [Header("References")]
    [SerializeField] private LaneSystem laneSystem;

    [Header("Traffic")]
    [SerializeField] private TrafficSpawnEntry[] trafficPrefabs;

    [Header("Spawning")]
    [SerializeField] private float spawnInterval = 1.1f;

    public float SpawnInterval => spawnInterval;

    private void Awake()
    {
        laneSystem ??= FindAnyObjectByType<LaneSystem>();
    }

    public List<TrafficSpawnRequest> BuildSpawnRequests(float spawnTime)
    {
        List<TrafficSpawnRequest> requests = new List<TrafficSpawnRequest>();

        if (laneSystem == null)
            return requests;

        for (int laneIndex = 0; laneIndex < laneSystem.LaneCount; laneIndex++)
        {
            GameObject selectedPrefab = GetRandomTrafficPrefabByWeight();

            if (selectedPrefab == null)
                continue;

            TrafficVehicle trafficData = selectedPrefab.GetComponent<TrafficVehicle>();
            int targetLaneIndex = laneIndex;
            float laneChangeStartDelay = 0f;
            float laneChangeDuration = 0f;

            bool hasLaneChangePlan = trafficData != null &&
                                     trafficData.TryCreateLaneChangePlan(
                                         laneIndex,
                                         laneSystem.LaneCount,
                                         out targetLaneIndex,
                                         out laneChangeStartDelay,
                                         out laneChangeDuration);

            requests.Add(new TrafficSpawnRequest(
                selectedPrefab,
                laneIndex,
                spawnTime,
                hasLaneChangePlan,
                hasLaneChangePlan ? targetLaneIndex : laneIndex,
                hasLaneChangePlan ? laneChangeStartDelay : 0f,
                hasLaneChangePlan ? laneChangeDuration : 0f));
        }

        return requests;
    }

    public GameObject GetRandomTrafficPrefabByWeight()
    {
        float totalWeight = 0f;

        foreach (TrafficSpawnEntry entry in trafficPrefabs)
        {
            if (entry == null || entry.prefab == null)
                continue;

            if (entry.spawnWeight <= 0f)
                continue;

            totalWeight += entry.spawnWeight;
        }

        if (totalWeight <= 0f)
            return null;

        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (TrafficSpawnEntry entry in trafficPrefabs)
        {
            if (entry == null || entry.prefab == null)
                continue;

            if (entry.spawnWeight <= 0f)
                continue;

            currentWeight += entry.spawnWeight;

            if (randomValue <= currentWeight)
                return entry.prefab;
        }

        return null;
    }

    public bool CanExecuteSpawn(TrafficSpawnRequest request)
    {
        return request != null &&
               laneSystem != null &&
               request.Prefab != null &&
               request.LaneIndex >= 0 &&
               request.LaneIndex < laneSystem.LaneCount;
    }

    public bool ExecuteSpawn(TrafficSpawnRequest request)
    {
        if (!CanExecuteSpawn(request))
        {
            Debug.LogWarning("TrafficSpawner: LaneSystem is not assigned.");
            return false;
        }

        TrafficVehicle trafficData = request.Prefab.GetComponent<TrafficVehicle>();

        if (trafficData == null)
        {
            Debug.LogWarning("Traffic prefab nema TrafficVehicle script.");
            return false;
        }

        float laneX = laneSystem.GetLaneX(request.LaneIndex);
        float spawnY = laneSystem.GetSpawnY(trafficData);

        Vector3 spawnPosition = new Vector3(laneX, spawnY, 0f);

        GameObject spawnedTraffic = Instantiate(request.Prefab, spawnPosition, Quaternion.identity);

        TrafficVehicle trafficVehicle = spawnedTraffic.GetComponent<TrafficVehicle>();
        trafficVehicle.Initialize(
            request.LaneIndex,
            laneSystem,
            request.HasLaneChangePlan,
            request.TargetLaneIndex,
            request.LaneChangeStartDelay,
            request.LaneChangeDuration);

        return true;
    }
}
