using System.Collections.Generic;
using UnityEngine;

public class SpawnSafetyPlanner : MonoBehaviour
{
    public struct SideRoadSpawnPlan
    {
        public SideRoadDirection Direction;
        public int StartLane;
        public int TargetLane;
        public float ReservationStartTime;
        public float ReservationEndTime;
    }

    [Header("References")]
    [SerializeField] private LaneSystem laneSystem;
    [SerializeField] private PlayerLaneTracker playerLaneTracker;
    [SerializeField] private SpawnReservationMap reservationMap;

    [Header("Prediction")]
    [SerializeField] private float predictionTime = 3f;
    [SerializeField] private float timeStep = 0.15f;
    [SerializeField] private float laneChangeTime = 0.25f;

    [Header("Danger Zone")]
    [SerializeField] private float dangerZoneAbovePlayer = 2.2f;
    [SerializeField] private float dangerZoneBelowPlayer = 1.2f;

    [Header("Side Road")]
    [SerializeField] private float sideRoadArrivalBuffer = 0.35f;

    private void Awake()
    {
        laneSystem ??= FindAnyObjectByType<LaneSystem>();
        playerLaneTracker ??= FindAnyObjectByType<PlayerLaneTracker>();
        reservationMap ??= GetComponent<SpawnReservationMap>();
    }

    public void Tick(float currentTime)
    {
        reservationMap?.CleanupExpiredReservations(currentTime);
    }

    public bool CanSpawnTraffic(TrafficSpawnRequest request)
    {
        if (request.SafetyMode == SpawnSafetyMode.SkipValidation)
            return true;

        // Traffic-only scenes, such as the main menu background, intentionally
        // have no player. Keep traffic-to-traffic spacing handled by the normal
        // spawner while skipping the player escape-path requirement.
        if (playerLaneTracker == null)
            return true;

        if (!TryBuildTrafficSnapshot(request, out TrafficSnapshot candidate))
            return false;

        List<TrafficSnapshot> snapshots = CreateTrafficSnapshots();

        if (WillCollideWithTraffic(snapshots, candidate))
            return false;

        if (ViolatesLaneReservations(candidate, request.SpawnTime))
            return false;

        snapshots.Add(candidate);

        bool[,] blockedMap = BuildBlockedMap(snapshots);
        return HasPlayerHorizontalPath(blockedMap);
    }

    public void RegisterTrafficSpawn(TrafficSpawnRequest request)
    {
        if (request.SafetyMode == SpawnSafetyMode.SkipValidation || playerLaneTracker == null)
            return;

        if (!TryBuildTrafficSnapshot(request, out TrafficSnapshot snapshot))
            return;

        for (int lane = 0; lane < laneSystem.LaneCount; lane++)
        {
            if (!TryGetDangerWindowForLane(snapshot, request.SpawnTime, lane, out float blockedStartTime, out float blockedEndTime))
                continue;

            reservationMap?.TryReserveLane(
                lane,
                blockedStartTime,
                blockedEndTime,
                SpawnReservationMap.ReservationKind.Blocked,
                "Traffic");
        }
    }

    public bool TryCreateSideRoadPlan(SideRoadSpawnRequest request, out SideRoadSpawnPlan plan)
    {
        plan = default;

        if (request.SafetyMode == SpawnSafetyMode.SkipValidation)
        {
            return true;
        }

        if (laneSystem == null || playerLaneTracker == null || reservationMap == null)
            return false;

        int currentLane = playerLaneTracker.CurrentLaneIndex;
        int targetLane = request.SideDirection == SideRoadDirection.Left
            ? 0
            : laneSystem.LaneCount - 1;

        if (currentLane < 0 || currentLane >= laneSystem.LaneCount)
            return false;

        float reservationStartTime = request.SpawnTime;
        float reservationEndTime = request.SpawnTime + request.TimeToPlayerArea + sideRoadArrivalBuffer;

        if (!CanKeepCorridorClear(currentLane, targetLane, reservationStartTime, reservationEndTime))
            return false;

        plan = new SideRoadSpawnPlan
        {
            Direction = request.SideDirection,
            StartLane = currentLane,
            TargetLane = targetLane,
            ReservationStartTime = reservationStartTime,
            ReservationEndTime = reservationEndTime
        };

        return true;
    }

    public bool RegisterSideRoadPlan(SideRoadSpawnPlan plan)
    {
        if (reservationMap == null)
            return false;

        int minLane = Mathf.Min(plan.StartLane, plan.TargetLane);
        int maxLane = Mathf.Max(plan.StartLane, plan.TargetLane);

        for (int lane = minLane; lane <= maxLane; lane++)
        {
            if (!reservationMap.TryReserveLane(
                    lane,
                    plan.ReservationStartTime,
                    plan.ReservationEndTime,
                    SpawnReservationMap.ReservationKind.KeepClear,
                    "SideRoad corridor"))
            {
                return false;
            }
        }

        reservationMap.RegisterSideRoadWindow(
            plan.Direction,
            plan.ReservationStartTime,
            plan.ReservationEndTime);

        return true;
    }

    public bool ShouldValidate(ISpawnRequest request)
    {
        return request != null && request.SafetyMode == SpawnSafetyMode.Required;
    }

    private bool CanKeepCorridorClear(int startLane, int targetLane, float startTime, float endTime)
    {
        int minLane = Mathf.Min(startLane, targetLane);
        int maxLane = Mathf.Max(startLane, targetLane);
        List<TrafficSnapshot> snapshots = CreateTrafficSnapshots();

        for (float time = startTime; time <= endTime; time += timeStep)
        {
            float futureTime = time - startTime;

            for (int lane = minLane; lane <= maxLane; lane++)
            {
                if (reservationMap.HasConflict(
                        lane,
                        time,
                        time,
                        SpawnReservationMap.ReservationKind.KeepClear))
                {
                    return false;
                }

                if (IsLaneBlockedByTrafficAtTime(snapshots, lane, futureTime))
                    return false;
            }
        }

        return true;
    }

    private bool IsLaneBlockedByTrafficAtTime(List<TrafficSnapshot> trafficVehicles, int laneIndex, float futureTime)
    {
        float playerY = playerLaneTracker.transform.position.y;
        float playerHalfLength = playerLaneTracker.GetHalfLength();

        float dangerBottomY = playerY - playerHalfLength - dangerZoneBelowPlayer;
        float dangerTopY = playerY + playerHalfLength + dangerZoneAbovePlayer;

        foreach (TrafficSnapshot trafficVehicle in trafficVehicles)
        {
            if (!OccupiesLaneAtTime(trafficVehicle, laneIndex, futureTime))
                continue;

            float futureY = GetFutureTrafficY(trafficVehicle, futureTime);
            float trafficBottomY = futureY - trafficVehicle.HalfLength;
            float trafficTopY = futureY + trafficVehicle.HalfLength;

            bool overlapsDangerZone =
                trafficTopY >= dangerBottomY &&
                trafficBottomY <= dangerTopY;

            if (overlapsDangerZone)
                return true;
        }

        return false;
    }

    private bool ViolatesLaneReservations(TrafficSnapshot traffic, float spawnTime)
    {
        if (reservationMap == null)
            return false;

        int stepCount = Mathf.CeilToInt(predictionTime / timeStep) + 1;
        float playerY = playerLaneTracker.transform.position.y;
        float playerHalfLength = playerLaneTracker.GetHalfLength();

        float dangerBottomY = playerY - playerHalfLength - dangerZoneBelowPlayer;
        float dangerTopY = playerY + playerHalfLength + dangerZoneAbovePlayer;

        for (int step = 0; step < stepCount; step++)
        {
            float futureTime = step * timeStep;
            float futureY = GetFutureTrafficY(traffic, futureTime);

            float trafficBottomY = futureY - traffic.HalfLength;
            float trafficTopY = futureY + traffic.HalfLength;

            bool overlapsDangerZone =
                trafficTopY >= dangerBottomY &&
                trafficBottomY <= dangerTopY;

            if (!overlapsDangerZone)
                continue;

            GetOccupiedLaneRange(traffic, futureTime, out int minLane, out int maxLane);

            for (int lane = minLane; lane <= maxLane; lane++)
            {
                if (!reservationMap.IsLaneReserved(
                        lane,
                        spawnTime + futureTime,
                        SpawnReservationMap.ReservationKind.KeepClear))
                {
                    continue;
                }

                return true;
            }
        }

        return false;
    }

    private bool TryBuildTrafficSnapshot(TrafficSpawnRequest request, out TrafficSnapshot snapshot)
    {
        snapshot = default;

        if (laneSystem == null || playerLaneTracker == null || request.Prefab == null)
            return false;

        TrafficVehicle trafficData = request.Prefab.GetComponent<TrafficVehicle>();

        if (trafficData == null)
            return false;

        if (request.LaneIndex < 0 || request.LaneIndex >= laneSystem.LaneCount)
            return false;

        snapshot = new TrafficSnapshot
        {
            LaneIndex = request.LaneIndex,
            HasLaneChangePlan = request.HasLaneChangePlan,
            TargetLaneIndex = request.TargetLaneIndex,
            LaneChangeStartDelay = request.LaneChangeStartDelay,
            LaneChangeDuration = request.LaneChangeDuration,
            StartY = laneSystem.GetSpawnY(trafficData),
            SpeedOffset = trafficData.SpeedOffset,
            HalfLength = trafficData.GetHalfLength()
        };

        return true;
    }

    private List<TrafficSnapshot> CreateTrafficSnapshots()
    {
        List<TrafficSnapshot> snapshots = new List<TrafficSnapshot>();
        TrafficVehicle[] activeTraffic = FindObjectsByType<TrafficVehicle>();

        foreach (TrafficVehicle trafficVehicle in activeTraffic)
        {
            if (trafficVehicle.LaneIndex < 0)
                continue;

            bool hasLaneChangePlan = trafficVehicle.TryGetLaneChangePrediction(
                out int targetLaneIndex,
                out float laneChangeStartDelay,
                out float laneChangeDuration);

            snapshots.Add(new TrafficSnapshot
            {
                LaneIndex = trafficVehicle.LaneIndex,
                HasLaneChangePlan = hasLaneChangePlan,
                TargetLaneIndex = hasLaneChangePlan ? targetLaneIndex : trafficVehicle.LaneIndex,
                LaneChangeStartDelay = hasLaneChangePlan ? laneChangeStartDelay : 0f,
                LaneChangeDuration = hasLaneChangePlan ? laneChangeDuration : 0f,
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

                if (!overlapsDangerZone)
                    continue;

                GetOccupiedLaneRange(trafficVehicle, futureTime, out int minLane, out int maxLane);

                for (int lane = minLane; lane <= maxLane; lane++)
                    blocked[step, lane] = true;
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
            nextReachable[targetLane] = true;
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

    private bool WillCollideWithTraffic(List<TrafficSnapshot> existingTraffic, TrafficSnapshot newTraffic)
    {
        int stepCount = Mathf.CeilToInt(predictionTime / timeStep) + 1;

        foreach (TrafficSnapshot existingVehicle in existingTraffic)
        {
            for (int step = 0; step < stepCount; step++)
            {
                float futureTime = step * timeStep;

                if (!DoTrafficSnapshotsOverlapInAnyLane(existingVehicle, newTraffic, futureTime))
                    continue;

                return true;
            }
        }

        return false;
    }

    private bool TryGetDangerWindowForLane(
        TrafficSnapshot traffic,
        float spawnTime,
        int laneIndex,
        out float startTime,
        out float endTime)
    {
        startTime = 0f;
        endTime = 0f;

        bool foundStart = false;
        int stepCount = Mathf.CeilToInt(predictionTime / timeStep) + 1;
        float playerY = playerLaneTracker.transform.position.y;
        float playerHalfLength = playerLaneTracker.GetHalfLength();

        float dangerBottomY = playerY - playerHalfLength - dangerZoneBelowPlayer;
        float dangerTopY = playerY + playerHalfLength + dangerZoneAbovePlayer;

        for (int step = 0; step < stepCount; step++)
        {
            float futureTime = step * timeStep;
            float futureY = GetFutureTrafficY(traffic, futureTime);
            float trafficBottomY = futureY - traffic.HalfLength;
            float trafficTopY = futureY + traffic.HalfLength;

            bool overlapsDangerZone =
                trafficTopY >= dangerBottomY &&
                trafficBottomY <= dangerTopY;

            if (!overlapsDangerZone || !OccupiesLaneAtTime(traffic, laneIndex, futureTime))
            {
                if (foundStart)
                    break;

                continue;
            }

            if (!foundStart)
            {
                startTime = spawnTime + futureTime;
                foundStart = true;
            }

            endTime = spawnTime + futureTime;
        }

        return foundStart;
    }

    private float GetFutureTrafficY(TrafficSnapshot traffic, float futureTime)
    {
        return traffic.StartY - GetTrafficTravelDistance(traffic, futureTime);
    }

    private float GetTrafficTravelDistance(TrafficSnapshot traffic, float futureTime)
    {
        float currentGlobalSpeed = GetCurrentGlobalSpeed();
        float maxGlobalSpeed = GetMaxGlobalSpeed();
        float acceleration = GetGlobalAcceleration();

        float currentVehicleSpeed = Mathf.Max(0f, currentGlobalSpeed + traffic.SpeedOffset);
        float maxVehicleSpeed = Mathf.Max(0f, maxGlobalSpeed + traffic.SpeedOffset);

        if (acceleration <= 0f || currentVehicleSpeed >= maxVehicleSpeed)
            return currentVehicleSpeed * futureTime;

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

    private float GetCurrentGlobalSpeed()
    {
        return GameManager.Instance != null ? GameManager.Instance.CurrentGameSpeed : 4f;
    }

    private float GetMaxGlobalSpeed()
    {
        return GameManager.Instance != null ? GameManager.Instance.MaxGameSpeed : 12f;
    }

    private float GetGlobalAcceleration()
    {
        return GameManager.Instance != null ? GameManager.Instance.SpeedIncreasePerSecond : 0f;
    }

    private struct TrafficSnapshot
    {
        public int LaneIndex;
        public bool HasLaneChangePlan;
        public int TargetLaneIndex;
        public float LaneChangeStartDelay;
        public float LaneChangeDuration;
        public float StartY;
        public float SpeedOffset;
        public float HalfLength;
    }

    private void GetOccupiedLaneRange(TrafficSnapshot traffic, float futureTime, out int minLane, out int maxLane)
    {
        minLane = traffic.LaneIndex;
        maxLane = traffic.LaneIndex;

        if (!traffic.HasLaneChangePlan || traffic.TargetLaneIndex == traffic.LaneIndex)
            return;

        if (futureTime < traffic.LaneChangeStartDelay)
            return;

        if (futureTime < traffic.LaneChangeStartDelay + traffic.LaneChangeDuration)
        {
            minLane = Mathf.Min(traffic.LaneIndex, traffic.TargetLaneIndex);
            maxLane = Mathf.Max(traffic.LaneIndex, traffic.TargetLaneIndex);
            return;
        }

        minLane = traffic.TargetLaneIndex;
        maxLane = traffic.TargetLaneIndex;
    }

    private bool OccupiesLaneAtTime(TrafficSnapshot traffic, int laneIndex, float futureTime)
    {
        GetOccupiedLaneRange(traffic, futureTime, out int minLane, out int maxLane);
        return laneIndex >= minLane && laneIndex <= maxLane;
    }

    private bool DoTrafficSnapshotsOverlapInAnyLane(
        TrafficSnapshot firstTraffic,
        TrafficSnapshot secondTraffic,
        float futureTime)
    {
        GetOccupiedLaneRange(firstTraffic, futureTime, out int firstMinLane, out int firstMaxLane);
        GetOccupiedLaneRange(secondTraffic, futureTime, out int secondMinLane, out int secondMaxLane);

        bool sharesLane =
            firstMinLane <= secondMaxLane &&
            firstMaxLane >= secondMinLane;

        if (!sharesLane)
            return false;

        float firstFutureY = GetFutureTrafficY(firstTraffic, futureTime);
        float secondFutureY = GetFutureTrafficY(secondTraffic, futureTime);

        float firstBottomY = firstFutureY - firstTraffic.HalfLength;
        float firstTopY = firstFutureY + firstTraffic.HalfLength;
        float secondBottomY = secondFutureY - secondTraffic.HalfLength;
        float secondTopY = secondFutureY + secondTraffic.HalfLength;

        return firstTopY >= secondBottomY && firstBottomY <= secondTopY;
    }
}
