using System;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class EnvironmentManager : MonoBehaviour
{
    public static EnvironmentManager Instance { get; private set; }
    public static event Action<EnvironmentDefinition> EnvironmentChanged;

    [Header("Optional Starting Environment")]
    [SerializeField] private EnvironmentDefinition startingEnvironment;

    private EnvironmentDefinition currentEnvironment;
    private GameObject currentEnvironmentVisual;

    public EnvironmentDefinition CurrentEnvironment => currentEnvironment;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstanceExists()
    {
        if (FindAnyObjectByType<EnvironmentManager>() == null)
            new GameObject(nameof(EnvironmentManager)).AddComponent<EnvironmentManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // A replacement scene may awaken before the previous scene is fully
            // unloaded. Transfer ownership across scenes instead of deleting the
            // new manager and leaving Instance null after the old one is destroyed.
            if (Instance.gameObject.scene.Equals(gameObject.scene))
            {
                Destroy(gameObject);
                return;
            }
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void OnEnable()
    {
        SideRoad.PlayerEnteredSideRoad += HandleSideRoadEntered;
    }

    private void Start()
    {
        if (startingEnvironment != null)
            SwitchEnvironment(startingEnvironment);
    }

    private void OnDisable()
    {
        SideRoad.PlayerEnteredSideRoad -= HandleSideRoadEntered;
    }

    private void HandleSideRoadEntered(SideRoad sideRoad)
    {
        if (sideRoad != null && sideRoad.DestinationEnvironment != null)
            SwitchEnvironment(sideRoad.DestinationEnvironment);
    }

    public void SwitchEnvironment(EnvironmentDefinition environment)
    {
        if (environment == null)
            return;

        ResetDynamicWorldState();

        LaneSystem laneSystem = FindAnyObjectByType<LaneSystem>();
        laneSystem?.ApplyEnvironment(environment);

        GameManager.Instance?.ApplyEnvironment(environment);

        FuelSystem fuelSystem = FindAnyObjectByType<FuelSystem>();
        fuelSystem?.SetConsumptionMultiplier(environment.FuelConsumptionMultiplier);

        PlayerController playerController = FindAnyObjectByType<PlayerController>();
        playerController?.SetEnvironmentSpeedMultiplier(environment.HorizontalSpeedMultiplier);

        ApplySpawnerConfig(environment.SpawnerConfig);

        playerController?.ResetToEnvironmentStart();

        CameraLaneFitter cameraFitter = FindAnyObjectByType<CameraLaneFitter>();
        cameraFitter?.FitCameraToLanes();

        FindAnyObjectByType<SpawnReservationMap>()?.ClearAllReservations();
        FindAnyObjectByType<SpawnDirector>()?.ResetRuntimeState();

        ReplaceEnvironmentVisual(environment.EnvironmentPrefab);
        currentEnvironment = environment;
        EnvironmentChanged?.Invoke(environment);

        Debug.Log($"Environment changed to {environment.DisplayName}. Player progress was preserved.");
    }

    private static void ApplySpawnerConfig(EnvironmentSpawnerConfig config)
    {
        if (config == null)
        {
            Debug.LogWarning("Environment has no spawner config assigned.");
            return;
        }

        FindAnyObjectByType<TrafficSpawner>()?.ApplyConfig(config);
        FindAnyObjectByType<SideRoadSpawner>()?.ApplyConfig(config);
        FindAnyObjectByType<RoadSignSpawner>()?.ApplyConfig(config);

        SideRoadEventSource[] eventSources = FindObjectsByType<SideRoadEventSource>();
        foreach (SideRoadEventSource eventSource in eventSources)
            eventSource.ApplyConfig(config);

        FindAnyObjectByType<SpawnDirector>()?.ApplyConfig(config);
    }

    private static void ResetDynamicWorldState()
    {
        DestroyAllActive<TrafficVehicle>();
        DestroyAllActive<RoadSign>();
        DestroyAllActive<SideRoad>();
    }

    private static void DestroyAllActive<T>() where T : MonoBehaviour
    {
        T[] activeObjects = FindObjectsByType<T>();

        foreach (T activeObject in activeObjects)
        {
            if (activeObject != null)
                Destroy(activeObject.gameObject);
        }
    }

    private void ReplaceEnvironmentVisual(GameObject visualPrefab)
    {
        if (currentEnvironmentVisual != null)
            Destroy(currentEnvironmentVisual);

        currentEnvironmentVisual = visualPrefab != null
            ? Instantiate(visualPrefab, Vector3.zero, Quaternion.identity)
            : null;
    }
}
