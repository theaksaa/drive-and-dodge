using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy")]
    [SerializeField] private GameObject enemyPrefab;

    [Header("Spawning")]
    [SerializeField] private float spawnInterval = 1.2f;
    [SerializeField] private float spawnYExtraOffset = 1.5f;

    [Header("Lanes")]
    [SerializeField] private int laneCount = 3;
    [SerializeField] private float roadWidthPercent = 0.75f;

    private Camera mainCamera;
    private float timer;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            SpawnEnemy();
            timer = 0f;
        }
    }

    private void SpawnEnemy()
    {
        int randomLaneIndex = Random.Range(0, laneCount);

        Vector3 spawnPosition = GetLaneSpawnPosition(randomLaneIndex);

        Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
    }

    private Vector3 GetLaneSpawnPosition(int laneIndex)
    {
        float distanceFromCamera = Mathf.Abs(mainCamera.transform.position.z);

        Vector3 bottomLeft = mainCamera.ViewportToWorldPoint(
            new Vector3(0f, 0f, distanceFromCamera)
        );

        Vector3 topRight = mainCamera.ViewportToWorldPoint(
            new Vector3(1f, 1f, distanceFromCamera)
        );

        float screenWidth = topRight.x - bottomLeft.x;
        float roadWidth = screenWidth * roadWidthPercent;

        float roadLeftX = -roadWidth / 2f;
        float laneWidth = roadWidth / laneCount;

        float laneX = roadLeftX + laneWidth * laneIndex + laneWidth / 2f;

        float spawnY = topRight.y + spawnYExtraOffset;

        return new Vector3(laneX, spawnY, 0f);
    }
}