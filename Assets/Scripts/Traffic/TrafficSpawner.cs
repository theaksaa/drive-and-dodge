using System.Collections.Generic;
using UnityEngine;

public class TrafficSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LaneSystem laneSystem;
    [SerializeField] private TrafficSpawnPlanner spawnPlanner;

    [Header("Traffic")]
    [SerializeField] private GameObject[] trafficPrefabs;

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

        for (int trafficIndex = 0; trafficIndex < trafficPrefabs.Length; trafficIndex++)
        {
            for (int laneIndex = 0; laneIndex < laneSystem.LaneCount; laneIndex++)
            {
                options.Add(new SpawnOption
                {
                    TrafficPrefab = trafficPrefabs[trafficIndex],
                    LaneIndex = laneIndex
                });
            }
        }

        return options;
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
