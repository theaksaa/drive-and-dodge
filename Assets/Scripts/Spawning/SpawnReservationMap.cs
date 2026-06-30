using System.Collections.Generic;
using UnityEngine;

public class SpawnReservationMap : MonoBehaviour
{
    public enum ReservationKind
    {
        Blocked,
        KeepClear
    }

    [SerializeField] private float cleanupPadding = 0.5f;

    private readonly List<LaneReservation> laneReservations = new List<LaneReservation>();
    private readonly List<SideRoadReservation> sideRoadReservations = new List<SideRoadReservation>();

    public void CleanupExpiredReservations(float currentTime)
    {
        float cutoffTime = currentTime - cleanupPadding;

        laneReservations.RemoveAll(reservation => reservation.EndTime < cutoffTime);
        sideRoadReservations.RemoveAll(reservation => reservation.EndTime < cutoffTime);
    }

    public bool HasConflict(int laneIndex, float startTime, float endTime, ReservationKind newKind)
    {
        CleanupExpiredReservations(startTime);

        foreach (LaneReservation reservation in laneReservations)
        {
            if (reservation.LaneIndex != laneIndex)
                continue;

            if (!IntervalsOverlap(startTime, endTime, reservation.StartTime, reservation.EndTime))
                continue;

            if (newKind == ReservationKind.KeepClear && reservation.Kind == ReservationKind.Blocked)
                return true;

            if (newKind == ReservationKind.Blocked && reservation.Kind == ReservationKind.KeepClear)
                return true;
        }

        return false;
    }

    public bool TryReserveLane(
        int laneIndex,
        float startTime,
        float endTime,
        ReservationKind kind,
        string debugLabel)
    {
        if (HasConflict(laneIndex, startTime, endTime, kind))
            return false;

        laneReservations.Add(new LaneReservation
        {
            LaneIndex = laneIndex,
            StartTime = startTime,
            EndTime = endTime,
            Kind = kind,
            DebugLabel = debugLabel
        });

        return true;
    }

    public bool IsLaneReserved(int laneIndex, float time, ReservationKind kind)
    {
        CleanupExpiredReservations(time);

        foreach (LaneReservation reservation in laneReservations)
        {
            if (reservation.LaneIndex != laneIndex)
                continue;

            if (reservation.Kind != kind)
                continue;

            if (time >= reservation.StartTime && time <= reservation.EndTime)
                return true;
        }

        return false;
    }

    public void RegisterSideRoadWindow(SideRoadDirection direction, float startTime, float endTime)
    {
        sideRoadReservations.Add(new SideRoadReservation
        {
            Direction = direction,
            StartTime = startTime,
            EndTime = endTime
        });
    }

    private static bool IntervalsOverlap(float startA, float endA, float startB, float endB)
    {
        return startA <= endB && endA >= startB;
    }

    private struct LaneReservation
    {
        public int LaneIndex;
        public float StartTime;
        public float EndTime;
        public ReservationKind Kind;
        public string DebugLabel;
    }

    private struct SideRoadReservation
    {
        public SideRoadDirection Direction;
        public float StartTime;
        public float EndTime;
    }
}
