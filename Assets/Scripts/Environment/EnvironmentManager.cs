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
            Destroy(gameObject);
            return;
        }

        Instance = this;
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
        if (environment == null || environment == currentEnvironment)
            return;

        LaneSystem laneSystem = FindAnyObjectByType<LaneSystem>();
        laneSystem?.ApplyEnvironment(environment);

        GameManager.Instance?.ApplyEnvironment(environment);

        FuelSystem fuelSystem = FindAnyObjectByType<FuelSystem>();
        fuelSystem?.SetConsumptionMultiplier(environment.FuelConsumptionMultiplier);

        PlayerController playerController = FindAnyObjectByType<PlayerController>();
        playerController?.SetEnvironmentSpeedMultiplier(environment.HorizontalSpeedMultiplier);

        ApplySpawnerConfig(environment.SpawnerConfig);

        CameraLaneFitter cameraFitter = FindAnyObjectByType<CameraLaneFitter>();
        cameraFitter?.FitCameraToLanes();

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

        SideRoadEventSource[] eventSources = FindObjectsByType<SideRoadEventSource>(FindObjectsSortMode.None);
        foreach (SideRoadEventSource eventSource in eventSources)
            eventSource.ApplyConfig(config);

        FindAnyObjectByType<SpawnDirector>()?.ApplyConfig(config);
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
