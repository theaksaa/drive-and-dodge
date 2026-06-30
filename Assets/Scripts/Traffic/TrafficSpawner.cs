using System.Collections.Generic;
using UnityEngine;

public class TrafficSpawner : MonoBehaviour
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
    [SerializeField] private TrafficSpawnPlanner spawnPlanner;

    [Header("Traffic")]
    [SerializeField] private TrafficSpawnEntry[] trafficPrefabs;

    [Header("Spawning")]
    [SerializeField] private float spawnInterval = 1.1f;

    private float timer;

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;

        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            TrySpawnTraffic();
            timer = 0f;
        }
    }

    private void TrySpawnTraffic()
    {
        List<SpawnOption> options = GenerateSpawnOptions();
        Shuffle(options);

        foreach (SpawnOption option in options)
        {
            if (spawnPlanner.IsSpawnFair(option.TrafficPrefab, option.LaneIndex))
            {
                SpawnTraffic(option.TrafficPrefab, option.LaneIndex);
                return;
            }
        }

        // Ako nijedna opcija nije fer, ne spawnujemo nista.
    }

    private List<SpawnOption> GenerateSpawnOptions()
    {
        List<SpawnOption> options = new List<SpawnOption>();

        for (int laneIndex = 0; laneIndex < laneSystem.LaneCount; laneIndex++)
        {
            GameObject selectedPrefab = GetRandomTrafficPrefabByWeight();

            if (selectedPrefab == null)
                continue;

            options.Add(new SpawnOption
            {
                TrafficPrefab = selectedPrefab,
                LaneIndex = laneIndex
            });
        }

        return options;
    }

    private GameObject GetRandomTrafficPrefabByWeight()
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

    private void SpawnTraffic(GameObject trafficPrefab, int laneIndex)
    {
        TrafficVehicle trafficData = trafficPrefab.GetComponent<TrafficVehicle>();

        if (trafficData == null)
        {
            Debug.LogWarning("Traffic prefab nema TrafficVehicle script.");
            return;
        }

        float laneX = laneSystem.GetLaneX(laneIndex);
        float spawnY = laneSystem.GetSpawnY(trafficData);

        Vector3 spawnPosition = new Vector3(laneX, spawnY, 0f);

        GameObject spawnedTraffic = Instantiate(trafficPrefab, spawnPosition, Quaternion.identity);

        TrafficVehicle trafficVehicle = spawnedTraffic.GetComponent<TrafficVehicle>();
        trafficVehicle.Initialize(laneIndex, laneSystem);
    }

    private void Shuffle(List<SpawnOption> options)
    {
        for (int i = 0; i < options.Count; i++)
        {
            int randomIndex = Random.Range(i, options.Count);

            SpawnOption temp = options[i];
            options[i] = options[randomIndex];
            options[randomIndex] = temp;
        }
    }

    private struct SpawnOption
    {
        public GameObject TrafficPrefab;
        public int LaneIndex;
    }
}