using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Environment", menuName = "Drive And Dodge/Environment")]
public class EnvironmentDefinition : ScriptableObject
{
    [System.Serializable]
    public class RoadVisualSettings
    {
        [System.Serializable]
        public class WeightedSpriteVariant
        {
            [SerializeField] private Sprite sprite;
            [Min(0f)] [SerializeField] private float weight = 1f;

            public Sprite Sprite => sprite;
            public float Weight => weight;
        }

        [Header("Sprites")]
        [Tooltip("Optional road sprite. If empty, a solid color strip is used.")]
        [SerializeField] private Sprite roadSurfaceSprite;
        [Tooltip("Optional left edge sprite near the road border.")]
        [SerializeField] private Sprite leftEdgeSprite;
        [Min(0f)] [SerializeField] private float leftEdgeSpriteWeight = 1f;
        [Tooltip("Optional extra left edge sprites that can appear sometimes.")]
        [SerializeField] private List<WeightedSpriteVariant> leftEdgeSpriteVariants = new List<WeightedSpriteVariant>();
        [Tooltip("Optional right edge sprite near the road border.")]
        [SerializeField] private Sprite rightEdgeSprite;
        [Min(0f)] [SerializeField] private float rightEdgeSpriteWeight = 1f;
        [Tooltip("Optional extra right edge sprites that can appear sometimes.")]
        [SerializeField] private List<WeightedSpriteVariant> rightEdgeSpriteVariants = new List<WeightedSpriteVariant>();
        [Tooltip("Optional left outside sprite filling the area beyond the edge strip.")]
        [SerializeField] private Sprite leftOutsideSprite;
        [Min(0f)] [SerializeField] private float leftOutsideSpriteWeight = 1f;
        [Tooltip("Optional extra left outside sprites that can appear sometimes.")]
        [SerializeField] private List<WeightedSpriteVariant> leftOutsideSpriteVariants = new List<WeightedSpriteVariant>();
        [Tooltip("Optional right outside sprite filling the area beyond the edge strip.")]
        [SerializeField] private Sprite rightOutsideSprite;
        [Min(0f)] [SerializeField] private float rightOutsideSpriteWeight = 1f;
        [Tooltip("Optional extra right outside sprites that can appear sometimes.")]
        [SerializeField] private List<WeightedSpriteVariant> rightOutsideSpriteVariants = new List<WeightedSpriteVariant>();
        [Tooltip("Sprite used for lane dividers between lanes.")]
        [SerializeField] private Sprite laneDividerSprite;
        [Min(0f)] [SerializeField] private float laneDividerSpriteWeight = 1f;
        [Tooltip("Optional extra lane divider sprites that can appear sometimes.")]
        [SerializeField] private List<WeightedSpriteVariant> laneDividerSpriteVariants = new List<WeightedSpriteVariant>();

        [Header("Colors")]
        [SerializeField] private Color roadSurfaceColor = new Color(0.16f, 0.16f, 0.18f, 1f);
        [SerializeField] private Color edgeColor = new Color(0.28f, 0.28f, 0.28f, 1f);
        [SerializeField] private Color outsideColor = new Color(0.22f, 0.42f, 0.2f, 1f);
        [SerializeField] private Color laneDividerColor = Color.white;

        [Header("Layout")]
        [Min(4f)] [SerializeField] private float segmentHeight = 14f;
        [Min(2)] [SerializeField] private int segmentCount = 3;
        [Min(0.05f)] [SerializeField] private float laneDividerWidth = 0.18f;
        [Min(0f)] [SerializeField] private float leftEdgeWidth = 0.35f;
        [Min(0f)] [SerializeField] private float rightEdgeWidth = 0.35f;
        [SerializeField] private int sortingOrder = -20;

        [Header("Scale")]
        [SerializeField] private Vector3 leftEdgeScale = Vector3.one;
        [SerializeField] private Vector3 rightEdgeScale = Vector3.one;
        [SerializeField] private Vector3 leftOutsideScale = Vector3.one;
        [SerializeField] private Vector3 rightOutsideScale = Vector3.one;
        [SerializeField] private Vector3 laneDividerScale = new Vector3(4f, 4f, 4f);

        public Sprite RoadSurfaceSprite => roadSurfaceSprite;
        public Sprite LeftEdgeSprite => leftEdgeSprite;
        public Sprite RightEdgeSprite => rightEdgeSprite;
        public Sprite LeftOutsideSprite => leftOutsideSprite;
        public Sprite RightOutsideSprite => rightOutsideSprite;
        public Sprite LaneDividerSprite => laneDividerSprite;
        public float LeftEdgeSpriteWeight => leftEdgeSpriteWeight;
        public float RightEdgeSpriteWeight => rightEdgeSpriteWeight;
        public float LeftOutsideSpriteWeight => leftOutsideSpriteWeight;
        public float RightOutsideSpriteWeight => rightOutsideSpriteWeight;
        public float LaneDividerSpriteWeight => laneDividerSpriteWeight;
        public IReadOnlyList<WeightedSpriteVariant> LeftEdgeSpriteVariants => leftEdgeSpriteVariants;
        public IReadOnlyList<WeightedSpriteVariant> RightEdgeSpriteVariants => rightEdgeSpriteVariants;
        public IReadOnlyList<WeightedSpriteVariant> LeftOutsideSpriteVariants => leftOutsideSpriteVariants;
        public IReadOnlyList<WeightedSpriteVariant> RightOutsideSpriteVariants => rightOutsideSpriteVariants;
        public IReadOnlyList<WeightedSpriteVariant> LaneDividerSpriteVariants => laneDividerSpriteVariants;
        public Color RoadSurfaceColor => roadSurfaceColor;
        public Color EdgeColor => edgeColor;
        public Color OutsideColor => outsideColor;
        public Color LaneDividerColor => laneDividerColor;
        public float SegmentHeight => segmentHeight;
        public int SegmentCount => segmentCount;
        public float LaneDividerWidth => laneDividerWidth;
        public float LeftEdgeWidth => leftEdgeWidth;
        public float RightEdgeWidth => rightEdgeWidth;
        public int SortingOrder => sortingOrder;
        public Vector3 LeftEdgeScale => leftEdgeScale;
        public Vector3 RightEdgeScale => rightEdgeScale;
        public Vector3 LeftOutsideScale => leftOutsideScale;
        public Vector3 RightOutsideScale => rightOutsideScale;
        public Vector3 LaneDividerScale => laneDividerScale;

        public Sprite GetRandomLeftEdgeSprite()
        {
            return ChooseWeightedSprite(leftEdgeSprite, leftEdgeSpriteWeight, leftEdgeSpriteVariants);
        }

        public Sprite GetRandomRightEdgeSprite()
        {
            return ChooseWeightedSprite(rightEdgeSprite, rightEdgeSpriteWeight, rightEdgeSpriteVariants);
        }

        public Sprite GetRandomLeftOutsideSprite()
        {
            return ChooseWeightedSprite(leftOutsideSprite, leftOutsideSpriteWeight, leftOutsideSpriteVariants);
        }

        public Sprite GetRandomRightOutsideSprite()
        {
            return ChooseWeightedSprite(rightOutsideSprite, rightOutsideSpriteWeight, rightOutsideSpriteVariants);
        }

        public Sprite GetRandomLaneDividerSprite()
        {
            return ChooseWeightedSprite(laneDividerSprite, laneDividerSpriteWeight, laneDividerSpriteVariants);
        }

        private static Sprite ChooseWeightedSprite(
            Sprite defaultSprite,
            float defaultWeight,
            List<WeightedSpriteVariant> variants)
        {
            float totalWeight = 0f;

            if (defaultSprite != null && defaultWeight > 0f)
                totalWeight += defaultWeight;

            if (variants != null)
            {
                foreach (WeightedSpriteVariant variant in variants)
                {
                    if (variant == null || variant.Sprite == null || variant.Weight <= 0f)
                        continue;

                    totalWeight += variant.Weight;
                }
            }

            if (totalWeight <= 0f)
                return defaultSprite;

            float roll = Random.value * totalWeight;

            if (defaultSprite != null && defaultWeight > 0f)
            {
                if (roll < defaultWeight)
                    return defaultSprite;

                roll -= defaultWeight;
            }

            if (variants != null)
            {
                foreach (WeightedSpriteVariant variant in variants)
                {
                    if (variant == null || variant.Sprite == null || variant.Weight <= 0f)
                        continue;

                    if (roll < variant.Weight)
                        return variant.Sprite;

                    roll -= variant.Weight;
                }
            }

            return defaultSprite;
        }
    }

    [Header("Identity")]
    [SerializeField] private string displayName = "Environment";

    [Header("Visuals")]
    [Tooltip("Optional prefab spawned at world origin. Use it for the road/background presentation only.")]
    [SerializeField] private GameObject environmentPrefab;
    [SerializeField] private RoadVisualSettings roadVisuals = new RoadVisualSettings();

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
    public RoadVisualSettings RoadVisuals => roadVisuals;
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
