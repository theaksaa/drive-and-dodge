using System.Collections.Generic;
using UnityEngine;

public class TrafficSpawnPlanner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LaneSystem laneSystem;
    [SerializeField] private PlayerLaneTracker playerLaneTracker;

    [Header("Prediction")]
    [SerializeField] private float predictionTime = 3f;

    [Tooltip("How often the planner checks the future. Smaller = more precise, but more expensive.")]
    [SerializeField] private float timeStep = 0.15f;

    [Tooltip("How long it takes the player to safely change one lane.")]
    [SerializeField] private float laneChangeTime = 0.25f;

    [Header("Danger Zone")]
    [SerializeField] private float dangerZoneAbovePlayer = 2.2f;
    [SerializeField] private float dangerZoneBelowPlayer = 1.2f;

    public bool IsSpawnFair(GameObject trafficPrefab, int laneIndex)
    {
        if (laneSystem == null || playerLaneTracker == null)
            return false;

        TrafficVehicle trafficData = trafficPrefab.GetComponent<TrafficVehicle>();

        if (trafficData == null)
            return false;

        if (laneIndex < 0 || laneIndex >= laneSystem.LaneCount)
            return false;

        List<TrafficSnapshot> snapshots = CreateTrafficSnapshots();

        TrafficSnapshot newTrafficSnapshot = new TrafficSnapshot
        {
            LaneIndex = laneIndex,
            StartY = laneSystem.GetSpawnY(trafficData),
            SpeedOffset = trafficData.SpeedOffset,
            HalfLength = trafficData.GetHalfLength()
        };

        if (WillCollideWithTrafficInSameLane(snapshots, newTrafficSnapshot))
            return false;

        snapshots.Add(newTrafficSnapshot);

        bool[,] blockedMap = BuildBlockedMap(snapshots);

        return HasPlayerHorizontalPath(blockedMap);
    }

    private List<TrafficSnapshot> CreateTrafficSnapshots()
    {
        List<TrafficSnapshot> snapshots = new List<TrafficSnapshot>();

        TrafficVehicle[] activeTraffic = FindObjectsByType<TrafficVehicle>(FindObjectsSortMode.None);

        foreach (TrafficVehicle trafficVehicle in activeTraffic)
        {
            if (trafficVehicle.LaneIndex < 0)
                continue;

            snapshots.Add(new TrafficSnapshot
            {
                LaneIndex = trafficVehicle.LaneIndex,
                StartY = trafficVehicle.transform.position.y,
                SpeedOffset = trafficVehicle.SpeedOffset,
                HalfLength = trafficVehicle.GetHalfLength()
            });
        }

        return snapshots;
    }

    private bool[,] BuildBlockedMap(List<TrafficSnapshot> trafficVehicles)
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

            foreach (TrafficSnapshot trafficVehicle in trafficVehicles)
            {
                float futureY = GetFutureTrafficY(trafficVehicle, futureTime);

                float trafficBottomY = futureY - trafficVehicle.HalfLength;
                float trafficTopY = futureY + trafficVehicle.HalfLength;

                bool overlapsDangerZone =
                    trafficTopY >= dangerBottomY &&
                    trafficBottomY <= dangerTopY;

                if (overlapsDangerZone)
                {
                    blocked[step, trafficVehicle.LaneIndex] = true;
                }
            }
        }

        return blocked;
    }

    private bool HasPlayerHorizontalPath(bool[,] blocked)
    {
        int stepCount = blocked.GetLength(0);
        int laneCount = blocked.GetLength(1);

        int startLane = playerLaneTracker.CurrentLaneIndex;

        if (startLane < 0 || startLane >= laneCount)
            return false;

        if (blocked[0, startLane])
            return false;

        bool[] reachable = new bool[laneCount];
        reachable[startLane] = true;

        for (int step = 1; step < stepCount; step++)
        {
            float futureTime = step * timeStep;

            bool[] nextReachable = new bool[laneCount];

            for (int lane = 0; lane < laneCount; lane++)
            {
                if (!reachable[lane])
                    continue;

                TryMarkLaneReachable(blocked, nextReachable, step, futureTime, lane, lane);
                TryMarkLaneReachable(blocked, nextReachable, step, futureTime, lane, lane - 1);
                TryMarkLaneReachable(blocked, nextReachable, step, futureTime, lane, lane + 1);
            }

            reachable = nextReachable;

            if (!HasAnyReachableLane(reachable))
                return false;
        }

        return true;
    }

    private void TryMarkLaneReachable(
        bool[,] blocked,
        bool[] nextReachable,
        int step,
        float futureTime,
        int currentLane,
        int targetLane)
    {
        int laneCount = blocked.GetLength(1);

        if (targetLane < 0 || targetLane >= laneCount)
            return;

        if (blocked[step, targetLane])
            return;

        int laneDistance = Mathf.Abs(targetLane - currentLane);

        if (laneDistance == 0)
        {
            nextReachable[targetLane] = true;
            return;
        }

        float requiredTimeToChangeLane = laneChangeTime * laneDistance;

        if (futureTime >= requiredTimeToChangeLane)
        {
            nextReachable[targetLane] = true;
        }
    }

    private bool HasAnyReachableLane(bool[] reachable)
    {
        for (int i = 0; i < reachable.Length; i++)
        {
            if (reachable[i])
                return true;
        }

        return false;
    }

    private bool WillCollideWithTrafficInSameLane(List<TrafficSnapshot> existingTraffic, TrafficSnapshot newTraffic)
    {
        foreach (TrafficSnapshot existingVehicle in existingTraffic)
        {
            if (existingVehicle.LaneIndex != newTraffic.LaneIndex)
                continue;

            // Existing vehicle is below the newly spawned vehicle.
            // New vehicle spawns above and moves downward.
            if (existingVehicle.StartY >= newTraffic.StartY)
                continue;

            float newTrafficSpeed = GetCurrentTrafficSpeed(newTraffic);
            float existingTrafficSpeed = GetCurrentTrafficSpeed(existingVehicle);

            float relativeSpeed = newTrafficSpeed - existingTrafficSpeed;

            if (relativeSpeed <= 0f)
                continue;

            float newTrafficFrontY = newTraffic.StartY - newTraffic.HalfLength;
            float existingTrafficBackY = existingVehicle.StartY + existingVehicle.HalfLength;

            float gap = newTrafficFrontY - existingTrafficBackY;

            if (gap <= 0f)
                return true;

            float timeUntilCatch = gap / relativeSpeed;
            float timeUntilExistingTrafficLeaves = GetTimeUntilTrafficLeavesScreen(existingVehicle);

            if (timeUntilCatch <= timeUntilExistingTrafficLeaves)
                return true;
        }

        return false;
    }

    private float GetTimeUntilTrafficLeavesScreen(TrafficSnapshot traffic)
    {
        float bottomY = laneSystem.GetBottomY();
        float targetY = bottomY - traffic.HalfLength - 1f;

        float minTime = 0f;
        float maxTime = 30f;

        for (int i = 0; i < 30; i++)
        {
            float midTime = (minTime + maxTime) * 0.5f;
            float futureY = GetFutureTrafficY(traffic, midTime);

            if (futureY <= targetY)
                maxTime = midTime;
            else
                minTime = midTime;
        }

        return maxTime;
    }

    private float GetFutureTrafficY(TrafficSnapshot traffic, float futureTime)
    {
        float distance = GetTrafficTravelDistance(traffic, futureTime);

        return traffic.StartY - distance;
    }

    private float GetTrafficTravelDistance(TrafficSnapshot traffic, float futureTime)
    {
        float currentGlobalSpeed = GetCurrentGlobalSpeed();
        float maxGlobalSpeed = GetMaxGlobalSpeed();
        float acceleration = GetGlobalAcceleration();

        float currentVehicleSpeed = Mathf.Max(0f, currentGlobalSpeed + traffic.SpeedOffset);
        float maxVehicleSpeed = Mathf.Max(0f, maxGlobalSpeed + traffic.SpeedOffset);

        if (acceleration <= 0f || currentVehicleSpeed >= maxVehicleSpeed)
        {
            return currentVehicleSpeed * futureTime;
        }

        float timeUntilMaxSpeed = (maxVehicleSpeed - currentVehicleSpeed) / acceleration;

        if (futureTime <= timeUntilMaxSpeed)
        {
            return currentVehicleSpeed * futureTime + 0.5f * acceleration * futureTime * futureTime;
        }

        float distanceUntilMaxSpeed =
            currentVehicleSpeed * timeUntilMaxSpeed +
            0.5f * acceleration * timeUntilMaxSpeed * timeUntilMaxSpeed;

        float remainingTime = futureTime - timeUntilMaxSpeed;

        return distanceUntilMaxSpeed + maxVehicleSpeed * remainingTime;
    }

    private float GetCurrentTrafficSpeed(TrafficSnapshot traffic)
    {
        return Mathf.Max(0f, GetCurrentGlobalSpeed() + traffic.SpeedOffset);
    }

    private float GetCurrentGlobalSpeed()
    {
        if (GameManager.Instance == null)
            return 4f;

        return GameManager.Instance.CurrentGameSpeed;
    }

    private float GetMaxGlobalSpeed()
    {
        if (GameManager.Instance == null)
            return 12f;

        return GameManager.Instance.MaxGameSpeed;
    }

    private float GetGlobalAcceleration()
    {
        if (GameManager.Instance == null)
            return 0f;

        return GameManager.Instance.SpeedIncreasePerSecond;
    }

    private struct TrafficSnapshot
    {
        public int LaneIndex;
        public float StartY;
        public float SpeedOffset;
        public float HalfLength;
    }
}