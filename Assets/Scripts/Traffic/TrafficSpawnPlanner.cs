using System.Collections.Generic;
using UnityEngine;

public class TrafficSpawnPlanner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LaneSystem laneSystem;
    [SerializeField] private PlayerLaneTracker playerLaneTracker;

    [Header("Prediction")]
    [SerializeField] private float predictionTime = 3f;
    [SerializeField] private float timeStep = 0.25f;

    [Header("Danger Zone")]
    [SerializeField] private float dangerZoneAbovePlayer = 2.2f;
    [SerializeField] private float dangerZoneBelowPlayer = 1.2f;

    public bool IsSpawnFair(GameObject enemyPrefab, int laneIndex)
    {
        TrafficVehicle enemyData = enemyPrefab.GetComponent<TrafficVehicle>();

        if (enemyData == null)
            return false;

        List<EnemySnapshot> snapshots = CreateEnemySnapshots();

        EnemySnapshot newEnemySnapshot = new EnemySnapshot
        {
            LaneIndex = laneIndex,
            StartY = laneSystem.GetSpawnY(enemyData),
            Speed = enemyData.MoveSpeed,
            HalfLength = enemyData.GetHalfLength()
        };

        if (WillCollideWithEnemyInSameLane(snapshots, newEnemySnapshot))
            return false;

        snapshots.Add(newEnemySnapshot);

        bool[,] blockedMap = BuildBlockedMap(snapshots);

        return HasPlayerPath(blockedMap);
    }

    private bool WillCollideWithEnemyInSameLane(List<EnemySnapshot> existingEnemies, EnemySnapshot newEnemy)
    {
        foreach (EnemySnapshot existingEnemy in existingEnemies)
        {
            if (existingEnemy.LaneIndex != newEnemy.LaneIndex)
                continue;

            // Existing enemy mora biti ispred novog enemyja.
            // Veći Y znači da je više/gore na ekranu.
            if (existingEnemy.StartY >= newEnemy.StartY)
                continue;

            // Ako novi enemy nije brži, neće ga stići.
            float relativeSpeed = newEnemy.Speed - existingEnemy.Speed;

            if (relativeSpeed <= 0f)
                continue;

            float newEnemyFrontY = newEnemy.StartY - newEnemy.HalfLength;
            float existingEnemyBackY = existingEnemy.StartY + existingEnemy.HalfLength;

            float gap = newEnemyFrontY - existingEnemyBackY;

            if (gap <= 0f)
                return true;

            float timeUntilCatch = gap / relativeSpeed;
            float timeUntilExistingEnemyLeaves = GetTimeUntilEnemyLeavesScreen(existingEnemy);

            // Ako ga stiže pre nego što postojeći enemy izađe sa ekrana,
            // ovaj spawn nije bezbedan.
            if (timeUntilCatch <= timeUntilExistingEnemyLeaves)
                return true;
        }

        return false;
    }

    private float GetTimeUntilEnemyLeavesScreen(EnemySnapshot enemy)
    {
        float bottomY = laneSystem.GetBottomY();

        float distanceUntilGone = enemy.StartY - bottomY + enemy.HalfLength;

        return distanceUntilGone / enemy.Speed;
    }

    private List<EnemySnapshot> CreateEnemySnapshots()
    {
        List<EnemySnapshot> snapshots = new List<EnemySnapshot>();

        TrafficVehicle[] activeEnemies = FindObjectsByType<TrafficVehicle>(FindObjectsSortMode.None);

        foreach (TrafficVehicle enemy in activeEnemies)
        {
            if (enemy.LaneIndex < 0)
                continue;

            snapshots.Add(new EnemySnapshot
            {
                LaneIndex = enemy.LaneIndex,
                StartY = enemy.transform.position.y,
                Speed = enemy.MoveSpeed,
                HalfLength = enemy.GetHalfLength()
            });
        }

        return snapshots;
    }

    private bool[,] BuildBlockedMap(List<EnemySnapshot> enemies)
    {
        int stepCount = Mathf.CeilToInt(predictionTime / timeStep) + 1;
        int laneCount = laneSystem.LaneCount;

        bool[,] blocked = new bool[stepCount, laneCount];

        float playerY = playerLaneTracker.transform.position.y;
        float playerHalfLength = playerLaneTracker.GetHalfLength();

        float dangerBottomY = playerY - playerHalfLength - dangerZoneBelowPlayer;
        float dangerTopY = playerY + playerHalfLength + dangerZoneAbovePlayer;

        for (int step = 0; step < stepCount; step++)
        {
            float futureTime = step * timeStep;

            foreach (EnemySnapshot enemy in enemies)
            {
                float futureY = enemy.StartY - enemy.Speed * futureTime;

                float enemyBottomY = futureY - enemy.HalfLength;
                float enemyTopY = futureY + enemy.HalfLength;

                bool overlapsDangerZone =
                    enemyTopY >= dangerBottomY &&
                    enemyBottomY <= dangerTopY;

                if (overlapsDangerZone)
                {
                    blocked[step, enemy.LaneIndex] = true;
                }
            }
        }

        return blocked;
    }

    private bool HasPlayerPath(bool[,] blocked)
    {
        int stepCount = blocked.GetLength(0);
        int laneCount = blocked.GetLength(1);

        bool[] reachable = new bool[laneCount];

        int startLane = playerLaneTracker.CurrentLaneIndex;

        if (blocked[0, startLane])
            return false;

        reachable[startLane] = true;

        for (int step = 1; step < stepCount; step++)
        {
            bool[] nextReachable = new bool[laneCount];

            for (int lane = 0; lane < laneCount; lane++)
            {
                if (!reachable[lane])
                    continue;

                for (int move = -1; move <= 1; move++)
                {
                    int nextLane = lane + move;

                    if (nextLane < 0 || nextLane >= laneCount)
                        continue;

                    if (blocked[step, nextLane])
                        continue;

                    nextReachable[nextLane] = true;
                }
            }

            reachable = nextReachable;

            bool hasAnyReachableLane = false;

            for (int lane = 0; lane < laneCount; lane++)
            {
                if (reachable[lane])
                {
                    hasAnyReachableLane = true;
                    break;
                }
            }

            if (!hasAnyReachableLane)
                return false;
        }

        return true;
    }

    private struct EnemySnapshot
    {
        public int LaneIndex;
        public float StartY;
        public float Speed;
        public float HalfLength;
    }
}