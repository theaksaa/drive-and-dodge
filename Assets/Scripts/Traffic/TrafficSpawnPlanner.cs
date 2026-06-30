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

    public bool IsSpawnFair(GameObject trafficPrefab, int laneIndex)
    {
        TrafficVehicle trafficData = trafficPrefab.GetComponent<TrafficVehicle>();

        if (trafficData == null)
            return false;

        List<TrafficSnapshot> snapshots = CreateTrafficSnapshots();

        TrafficSnapshot newTrafficSnapshot = new TrafficSnapshot
        {
            LaneIndex = laneIndex,
            StartY = laneSystem.GetSpawnY(trafficData),
            Speed = trafficData.MoveSpeed,
            HalfLength = trafficData.GetHalfLength()
        };

        if (WillCollideWithTrafficInSameLane(snapshots, newTrafficSnapshot))
            return false;

        snapshots.Add(newTrafficSnapshot);

        bool[,] blockedMap = BuildBlockedMap(snapshots);

        return HasPlayerPath(blockedMap);
    }

    private bool WillCollideWithTrafficInSameLane(List<TrafficSnapshot> existingTraffic, TrafficSnapshot newTraffic)
    {
        foreach (TrafficSnapshot existingVehicle in existingTraffic)
        {
            if (existingVehicle.LaneIndex != newTraffic.LaneIndex)
                continue;

            // Existing traffic mora biti ispred novog traffic vozila.
            // Veci Y znaci da je vise/gore na ekranu.
            if (existingVehicle.StartY >= newTraffic.StartY)
                continue;

            // Ako novo traffic vozilo nije brze, nece ga stici.
            float relativeSpeed = newTraffic.Speed - existingVehicle.Speed;

            if (relativeSpeed <= 0f)
                continue;

            float newTrafficFrontY = newTraffic.StartY - newTraffic.HalfLength;
            float existingTrafficBackY = existingVehicle.StartY + existingVehicle.HalfLength;

            float gap = newTrafficFrontY - existingTrafficBackY;

            if (gap <= 0f)
                return true;

            float timeUntilCatch = gap / relativeSpeed;
            float timeUntilExistingTrafficLeaves = GetTimeUntilTrafficLeavesScreen(existingVehicle);

            // Ako ga stize pre nego sto postojece traffic vozilo izadje sa ekrana,
            // ovaj spawn nije bezbedan.
            if (timeUntilCatch <= timeUntilExistingTrafficLeaves)
                return true;
        }

        return false;
    }

    private float GetTimeUntilTrafficLeavesScreen(TrafficSnapshot traffic)
    {
        float bottomY = laneSystem.GetBottomY();

        float distanceUntilGone = traffic.StartY - bottomY + traffic.HalfLength;

        return distanceUntilGone / traffic.Speed;
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
                Speed = trafficVehicle.MoveSpeed,
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
                float futureY = trafficVehicle.StartY - trafficVehicle.Speed * futureTime;

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

    private struct TrafficSnapshot
    {
        public int LaneIndex;
        public float StartY;
        public float Speed;
        public float HalfLength;
    }
}
