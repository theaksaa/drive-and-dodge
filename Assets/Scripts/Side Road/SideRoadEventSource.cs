using UnityEngine;

public class SideRoadEventSource : MonoBehaviour, IRoadEventSource
{
    [System.Serializable]
    public class SideRoadEventProfile
    {
        public SideRoadType sideRoadType = SideRoadType.Default;
        public RoadEventWarningProfile warningProfile = new RoadEventWarningProfile();
    }

    private sealed class SideRoadEventPlan : IRoadEventPlan
    {
        private readonly SideRoadEventSource source;
        private readonly SideRoadType sideRoadType;
        private readonly SideRoad prefab;

        public SideRoadEventPlan(
            SideRoadEventSource source,
            RoadEventSide side,
            SideRoadType sideRoadType,
            SideRoad prefab,
            RoadEventWarningProfile warningProfile)
        {
            this.source = source;
            this.sideRoadType = sideRoadType;
            this.prefab = prefab;
            Side = side;
            WarningProfile = warningProfile;
        }

        public string DebugName => $"Side Road: {sideRoadType}";
        public RoadEventSide Side { get; }
        public RoadEventWarningProfile WarningProfile { get; }

        public bool TryExecute()
        {
            return source.TryExecute(sideRoadType, prefab, Side);
        }
    }

    [Header("References")]
    [SerializeField] private SideRoadSpawner sideRoadSpawner;
    [SerializeField] private SpawnSafetyPlanner safetyPlanner;
    [SerializeField] private PlayerLaneTracker playerLaneTracker;

    [Header("Event Selection")]
    [Min(0f)]
    [SerializeField] private float spawnWeight = 1f;

    [Header("Side Road Event Profiles")]
    [Tooltip("Each side-road type owns its warning-sign sequence.")]
    [SerializeField] private SideRoadEventProfile[] eventProfiles;

    public float SpawnWeight => spawnWeight;

    private void Awake()
    {
        sideRoadSpawner ??= GetComponent<SideRoadSpawner>();
        sideRoadSpawner ??= FindAnyObjectByType<SideRoadSpawner>();
        safetyPlanner ??= FindAnyObjectByType<SpawnSafetyPlanner>();
        playerLaneTracker ??= FindAnyObjectByType<PlayerLaneTracker>();

        if (eventProfiles == null)
            return;

        foreach (SideRoadEventProfile profile in eventProfiles)
            profile?.warningProfile?.SortFarthestFirst();
    }

    public bool SupportsSide(RoadEventSide side)
    {
        return side == RoadEventSide.Left || side == RoadEventSide.Right;
    }

    public bool TryCreatePlan(RoadEventSide side, out IRoadEventPlan plan)
    {
        plan = null;

        if (!SupportsSide(side) || sideRoadSpawner == null)
            return false;

        if (!sideRoadSpawner.TrySelectRandomVariant(
                out SideRoadType selectedType,
                out SideRoad selectedPrefab))
            return false;

        SideRoadEventProfile profile = FindProfile(selectedType);

        if (profile == null || profile.warningProfile == null ||
            !profile.warningProfile.IsValidFor(side))
        {
            Debug.LogWarning($"SideRoadEventSource: {selectedType} has no valid event warning profile.");
            return false;
        }

        plan = new SideRoadEventPlan(
            this,
            side,
            selectedType,
            selectedPrefab,
            profile.warningProfile);

        return true;
    }

    private SideRoadEventProfile FindProfile(SideRoadType sideRoadType)
    {
        if (eventProfiles == null)
            return null;

        foreach (SideRoadEventProfile profile in eventProfiles)
        {
            if (profile != null && profile.sideRoadType == sideRoadType)
                return profile;
        }

        return null;
    }

    private bool TryExecute(SideRoadType sideRoadType, SideRoad prefab, RoadEventSide side)
    {
        if (sideRoadSpawner == null || safetyPlanner == null || playerLaneTracker == null)
            return false;

        SideRoadDirection direction = side == RoadEventSide.Left
            ? SideRoadDirection.Left
            : SideRoadDirection.Right;

        float scrollSpeed = GameManager.Instance != null
            ? GameManager.Instance.CurrentGameSpeed
            : 4f;
        float spawnTime = Time.time;
        float timeToPlayerArea = sideRoadSpawner.GetTimeUntilReachingY(
            playerLaneTracker.transform.position.y,
            scrollSpeed);

        if (!sideRoadSpawner.TryCreateSpawnRequest(
                sideRoadType,
                prefab,
                direction,
                spawnTime,
                timeToPlayerArea,
                scrollSpeed,
                out SideRoadSpawnRequest request))
            return false;

        if (!sideRoadSpawner.CanExecuteSpawn(request))
            return false;

        bool requiresValidation = safetyPlanner.ShouldValidate(request);
        SpawnSafetyPlanner.SideRoadSpawnPlan safetyPlan = default;

        if (requiresValidation &&
            !safetyPlanner.TryCreateSideRoadPlan(request, out safetyPlan))
            return false;

        if (!sideRoadSpawner.ExecuteSpawn(request))
            return false;

        if (requiresValidation)
            safetyPlanner.RegisterSideRoadPlan(safetyPlan);

        return true;
    }
}
