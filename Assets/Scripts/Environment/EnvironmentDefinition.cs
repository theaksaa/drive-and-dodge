using UnityEngine;

[CreateAssetMenu(fileName = "Environment", menuName = "Drive And Dodge/Environment")]
public class EnvironmentDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string displayName = "Environment";

    [Header("Visuals")]
    [Tooltip("Optional prefab spawned at world origin. Use it for the road/background presentation only.")]
    [SerializeField] private GameObject environmentPrefab;

    [Header("Road")]
    [Min(1)] [SerializeField] private int laneCount = 3;
    [Min(0.1f)] [SerializeField] private float laneWidth = 1.6f;
    [Min(0f)] [SerializeField] private float leftMargin = 1f;
    [Min(0f)] [SerializeField] private float rightMargin = 1f;

    [Header("Speed")]
    [Min(0f)] [SerializeField] private float maxGameSpeed = 10f;
    [Min(0f)] [SerializeField] private float speedIncreasePerSecond = 0.02f;
    [Tooltip("When enabled, entering this environment immediately changes the current world speed.")]
    [SerializeField] private bool setSpeedOnEnter;
    [Min(0f)] [SerializeField] private float speedOnEnter = 6f;

    [Header("Player Modifiers")]
    [Min(0f)] [SerializeField] private float fuelConsumptionMultiplier = 1f;
    [Min(0f)] [SerializeField] private float horizontalSpeedMultiplier = 1f;

    [Header("All Spawning")]
    [Tooltip("Complete configuration for traffic, side roads, road signs, and event scheduling.")]
    [SerializeField] private EnvironmentSpawnerConfig spawnerConfig;

    public string DisplayName => displayName;
    public GameObject EnvironmentPrefab => environmentPrefab;
    public int LaneCount => laneCount;
    public float LaneWidth => laneWidth;
    public float LeftMargin => leftMargin;
    public float RightMargin => rightMargin;
    public float MaxGameSpeed => maxGameSpeed;
    public float SpeedIncreasePerSecond => speedIncreasePerSecond;
    public bool SetSpeedOnEnter => setSpeedOnEnter;
    public float SpeedOnEnter => speedOnEnter;
    public float FuelConsumptionMultiplier => fuelConsumptionMultiplier;
    public float HorizontalSpeedMultiplier => horizontalSpeedMultiplier;
    public EnvironmentSpawnerConfig SpawnerConfig => spawnerConfig;

    private void OnValidate()
    {
        speedOnEnter = Mathf.Min(speedOnEnter, maxGameSpeed);

    }
}
