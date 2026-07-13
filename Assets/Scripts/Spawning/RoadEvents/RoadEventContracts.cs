using UnityEngine;

public enum RoadEventSide
{
    Left,
    Right,
    FullRoad
}

public enum RoadSignPlacement
{
    EventSide,
    Left,
    Right,
    Both
}

[System.Serializable]
public class RoadEventWarning
{
    [Min(0f)]
    public float distanceBeforeEvent = 100f;

    [Tooltip("Prefab used for Left, Right and Both placement. For Event Side, this is used as a fallback when that side's prefab is not assigned.")]
    public RoadSign prefab;

    public RoadSignPlacement placement = RoadSignPlacement.EventSide;

    [Tooltip("Prefab used when placement is Event Side and the event is on the left.")]
    public RoadSign leftEventSidePrefab;

    [Tooltip("Prefab used when placement is Event Side and the event is on the right.")]
    public RoadSign rightEventSidePrefab;

    public RoadSign GetPrefab(RoadEventSide eventSide)
    {
        if (placement != RoadSignPlacement.EventSide)
            return prefab;

        switch (eventSide)
        {
            case RoadEventSide.Left:
                return leftEventSidePrefab != null ? leftEventSidePrefab : prefab;

            case RoadEventSide.Right:
                return rightEventSidePrefab != null ? rightEventSidePrefab : prefab;

            default:
                return null;
        }
    }

    public bool IsValidFor(RoadEventSide eventSide)
    {
        return distanceBeforeEvent >= 0f && GetPrefab(eventSide) != null;
    }
}

[System.Serializable]
public class RoadEventWarningProfile
{
    [Tooltip("When enabled, the event cannot be planned unless every warning entry is valid.")]
    public bool requireSigns = true;

    [Tooltip("Sign prefab, distance and roadside placement for every warning.")]
    public RoadEventWarning[] warnings;

    public bool IsValidFor(RoadEventSide eventSide)
    {
        if (!requireSigns)
            return true;

        if (warnings == null || warnings.Length == 0)
            return false;

        foreach (RoadEventWarning warning in warnings)
        {
            if (warning == null || !warning.IsValidFor(eventSide))
                return false;
        }

        return true;
    }

    public float GetFarthestWarningDistance()
    {
        float farthest = 0f;

        if (warnings == null)
            return farthest;

        foreach (RoadEventWarning warning in warnings)
        {
            if (warning != null)
                farthest = Mathf.Max(farthest, warning.distanceBeforeEvent);
        }

        return farthest;
    }

    public void SortFarthestFirst()
    {
        if (warnings == null)
        {
            warnings = new RoadEventWarning[0];
            return;
        }

        System.Array.Sort(warnings, (first, second) =>
        {
            float firstDistance = first != null ? first.distanceBeforeEvent : -1f;
            float secondDistance = second != null ? second.distanceBeforeEvent : -1f;
            return secondDistance.CompareTo(firstDistance);
        });
    }
}

public interface IRoadEventPlan
{
    string DebugName { get; }
    RoadEventSide Side { get; }
    RoadEventWarningProfile WarningProfile { get; }
    bool TryExecute();
}

public interface IRoadEventSource
{
    float SpawnWeight { get; }
    bool SupportsSide(RoadEventSide side);
    bool TryCreatePlan(RoadEventSide side, out IRoadEventPlan plan);
}
