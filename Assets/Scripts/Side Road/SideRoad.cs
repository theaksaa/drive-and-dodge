using System;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class SideRoad : MonoBehaviour
{
    public static int ActiveLeftZones { get; private set; }
    public static int ActiveRightZones { get; private set; }
    public static int VisibleSideRoadCount { get; private set; }
    public static int VisibleLeftSideRoadCount { get; private set; }
    public static int VisibleRightSideRoadCount { get; private set; }

    public static bool CanUseLeftSideRoad => ActiveLeftZones > 0;
    public static bool CanUseRightSideRoad => ActiveRightZones > 0;

    public static event Action<SideRoad> PlayerEnteredSideRoad;
    public static event Action<SideRoad> PlayerExitedSideRoad;

    [Header("Movement")]
    [SerializeField] private float fallbackMoveSpeed = 5f;
    [SerializeField] private float despawnY = -8f;

    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private BoxCollider2D triggerCollider;

    private SideRoadVisual sideRoadVisual;
    private LaneSystem laneSystem;

    [Header("Environment Transition")]
    [Tooltip("Entering this side road switches to this environment. Leave empty for a normal side road.")]
    [SerializeField] private EnvironmentDefinition destinationEnvironment;

    [Header("Visual Shape")]
    [Tooltip("Vertical diagonal rise as a multiplier of the configured side-road height.")]
    [Min(0.01f)] [SerializeField] private float visualRiseRatio = 0.75f;
    [Tooltip("Road thickness as a multiplier of the configured side-road height.")]
    [Min(0.01f)] [SerializeField] private float visualThicknessRatio = 0.55f;
    [Tooltip("Maximum width of each edge band as a fraction of the road thickness.")]
    [Range(0.01f, 0.49f)] [SerializeField] private float visualEdgeThicknessRatio = 0.20f;
    [Tooltip("How many road-thickness units the branch extends beyond the outside screen edge.")]
    [Min(0f)] [SerializeField] private float visualOuterExtensionMultiplier = 1f;
    [Tooltip("Extra world-space distance the complete road continues after clearing the outside screen edge.")]
    [Min(0f)] [SerializeField] private float visualOuterScreenPadding = 0.5f;
    [Tooltip("How strongly the branch eases into vertical tangents at the main road and outside edge.")]
    [Range(0f, 1f)] [SerializeField] private float visualCurveStrength = 1f;
    [Tooltip("Number of sections used to draw the curved road. Higher values look smoother.")]
    [Range(4, 64)] [SerializeField] private int visualCurveSegments = 20;

    private SideRoadDirection direction;
    private SideRoadType sideRoadType;
    private bool playerInside;
    private bool playerHasCrossedRoadEdge;
    private bool wasCountedAsVisible;
    private bool directionWasConfigured;
    private PolygonCollider2D curvedTriggerCollider;

    public SideRoadDirection Direction => direction;
    public SideRoadType RoadType => sideRoadType;
    public EnvironmentDefinition DestinationEnvironment => destinationEnvironment;

    private void Awake()
    {
        if (triggerCollider == null)
            triggerCollider = GetComponent<BoxCollider2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        sideRoadVisual = GetComponent<SideRoadVisual>();
        laneSystem = FindAnyObjectByType<LaneSystem>();

        if (sideRoadVisual == null)
            sideRoadVisual = gameObject.AddComponent<SideRoadVisual>();

        curvedTriggerCollider = GetComponent<PolygonCollider2D>();

        if (curvedTriggerCollider == null)
            curvedTriggerCollider = gameObject.AddComponent<PolygonCollider2D>();

        // The prefab SpriteRenderer was only a placeholder. The actual road is
        // assembled from the active environment sprites during Setup.
        if (spriteRenderer != null)
            spriteRenderer.enabled = false;

        triggerCollider.isTrigger = true;
        triggerCollider.enabled = false;
        curvedTriggerCollider.isTrigger = true;
    }

    private void OnEnable()
    {
        if (!wasCountedAsVisible)
        {
            VisibleSideRoadCount++;
            wasCountedAsVisible = true;
        }
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameplayStopped)
            return;

        float speed = fallbackMoveSpeed;

        if (GameManager.Instance != null)
            speed = GameManager.Instance.CurrentGameSpeed;

        transform.position += Vector3.down * speed * Time.deltaTime;

        if (transform.position.y <= despawnY)
            Destroy(gameObject);
    }

    public void Setup(
        SideRoadType type,
        SideRoadDirection sideRoadDirection,
        float despawnPositionY,
        float visualWidth,
        float visualHeight,
        float triggerOverlapIntoMainRoad
    )
    {
        sideRoadType = type;
        direction = sideRoadDirection;
        directionWasConfigured = true;
        despawnY = despawnPositionY;

        if (direction == SideRoadDirection.Left)
            VisibleLeftSideRoadCount++;
        else
            VisibleRightSideRoadCount++;

        transform.localScale = Vector3.one;

        if (triggerCollider == null)
            triggerCollider = GetComponent<BoxCollider2D>();

        triggerCollider.enabled = false;

        if (curvedTriggerCollider == null)
            curvedTriggerCollider = GetComponent<PolygonCollider2D>();

        if (curvedTriggerCollider == null)
            curvedTriggerCollider = gameObject.AddComponent<PolygonCollider2D>();

        curvedTriggerCollider.isTrigger = true;

        float safeVisualWidth = Mathf.Max(visualWidth, 0.01f);
        float safeVisualHeight = Mathf.Max(visualHeight, 0.01f);
        Vector2[] triggerPath = sideRoadVisual.Build(
            direction,
            safeVisualWidth,
            safeVisualHeight,
            triggerOverlapIntoMainRoad,
            visualRiseRatio,
            visualThicknessRatio,
            visualEdgeThicknessRatio,
            visualOuterExtensionMultiplier,
            visualOuterScreenPadding,
            visualCurveStrength,
            visualCurveSegments);

        curvedTriggerCollider.pathCount = 1;
        curvedTriggerCollider.SetPath(0, triggerPath);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (playerInside)
            return;

        playerInside = true;
        AddActiveZone();
        TryEnterSideRoad(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!playerInside || playerHasCrossedRoadEdge || !other.CompareTag("Player"))
            return;

        TryEnterSideRoad(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (!playerInside)
            return;

        playerInside = false;
        RemoveActiveZone();

        if (playerHasCrossedRoadEdge)
        {
            playerHasCrossedRoadEdge = false;
            PlayerExitedSideRoad?.Invoke(this);
        }
    }

    private void TryEnterSideRoad(Collider2D playerCollider)
    {
        if (laneSystem == null)
            laneSystem = FindAnyObjectByType<LaneSystem>();

        if (laneSystem == null)
            return;

        // The trigger deliberately overlaps the main road so it can temporarily
        // unlock movement toward the branch. Do not count that overlap as an
        // entry: the player's center must actually cross the main-road edge.
        float playerCenterX = playerCollider.bounds.center.x;
        bool crossedRoadEdge = direction == SideRoadDirection.Left
            ? playerCenterX < laneSystem.RoadLeftX
            : playerCenterX > laneSystem.RoadRightX;

        if (!crossedRoadEdge)
            return;

        playerHasCrossedRoadEdge = true;
        PlayerEnteredSideRoad?.Invoke(this);
    }

    private void AddActiveZone()
    {
        if (direction == SideRoadDirection.Left)
            ActiveLeftZones++;
        else
            ActiveRightZones++;
    }

    private void RemoveActiveZone()
    {
        if (direction == SideRoadDirection.Left)
            ActiveLeftZones = Mathf.Max(0, ActiveLeftZones - 1);
        else
            ActiveRightZones = Mathf.Max(0, ActiveRightZones - 1);
    }

    private void OnDestroy()
    {
        if (playerInside)
        {
            playerInside = false;
            RemoveActiveZone();
        }

        playerHasCrossedRoadEdge = false;

        if (wasCountedAsVisible)
        {
            VisibleSideRoadCount = Mathf.Max(0, VisibleSideRoadCount - 1);
            wasCountedAsVisible = false;
        }

        if (directionWasConfigured)
        {
            if (direction == SideRoadDirection.Left)
                VisibleLeftSideRoadCount = Mathf.Max(0, VisibleLeftSideRoadCount - 1);
            else
                VisibleRightSideRoadCount = Mathf.Max(0, VisibleRightSideRoadCount - 1);

            directionWasConfigured = false;
        }
    }
}
