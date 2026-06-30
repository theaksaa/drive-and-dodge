using System.Collections.Generic;
using UnityEngine;

public class TrafficSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LaneSystem laneSystem;
    [SerializeField] private TrafficSpawnPlanner spawnPlanner;

    [Header("Enemies")]
    [SerializeField] private GameObject[] enemyPrefabs;

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
            TrySpawnEnemy();
            timer = 0f;
        }
    }

    private void TrySpawnEnemy()
    {
        List<SpawnOption> options = GenerateSpawnOptions();
        Shuffle(options);

        foreach (SpawnOption option in options)
        {
            if (spawnPlanner.IsSpawnFair(option.EnemyPrefab, option.LaneIndex))
            {
                SpawnEnemy(option.EnemyPrefab, option.LaneIndex);
                return;
            }
        }

        // Ako nijedna opcija nije fer, ne spawnujemo ništa.
    }

    private List<SpawnOption> GenerateSpawnOptions()
    {
        List<SpawnOption> options = new List<SpawnOption>();

        for (int enemyIndex = 0; enemyIndex < enemyPrefabs.Length; enemyIndex++)
        {
            for (int laneIndex = 0; laneIndex < laneSystem.LaneCount; laneIndex++)
            {
                options.Add(new SpawnOption
                {
                    EnemyPrefab = enemyPrefabs[enemyIndex],
                    LaneIndex = laneIndex
                });
            }
        }

        return options;
    }

    private void SpawnEnemy(GameObject enemyPrefab, int laneIndex)
    {
        TrafficVehicle enemyData = enemyPrefab.GetComponent<TrafficVehicle>();

        if (enemyData == null)
        {
            Debug.LogWarning("Enemy prefab nema TrafficVehicle script.");
            return;
        }

        float laneX = laneSystem.GetLaneX(laneIndex);
        float spawnY = laneSystem.GetSpawnY(enemyData);

        Vector3 spawnPosition = new Vector3(laneX, spawnY, 0f);

        GameObject spawnedEnemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);

        TrafficVehicle TrafficVehicle = spawnedEnemy.GetComponent<TrafficVehicle>();
        TrafficVehicle.Initialize(laneIndex, laneSystem);
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
        public GameObject EnemyPrefab;
        public int LaneIndex;
    }
}