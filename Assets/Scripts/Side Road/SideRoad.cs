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

    [Header("Environment Transition")]
    [Tooltip("Entering this side road switches to this environment. Leave empty for a normal side road.")]
    [SerializeField] private EnvironmentDefinition destinationEnvironment;

    private SideRoadDirection direction;
    private SideRoadType sideRoadType;
    private bool playerInside;
    private bool wasCountedAsVisible;
    private bool directionWasConfigured;

    public SideRoadDirection Direction => direction;
    public SideRoadType RoadType => sideRoadType;
    public EnvironmentDefinition DestinationEnvironment => destinationEnvironment;

    private void Awake()
    {
        if (triggerCollider == null)
            triggerCollider = GetComponent<BoxCollider2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        triggerCollider.isTrigger = true;
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
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
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

        transform.localScale = new Vector3(visualWidth, visualHeight, 1f);

        if (triggerCollider == null)
            triggerCollider = GetComponent<BoxCollider2D>();

        triggerCollider.isTrigger = true;

        float safeVisualWidth = Mathf.Max(visualWidth, 0.01f);

        float colliderWidthMultiplier = 1f + (triggerOverlapIntoMainRoad / safeVisualWidth);
        triggerCollider.size = new Vector2(colliderWidthMultiplier, 1f);

        float offsetX = triggerOverlapIntoMainRoad / (2f * safeVisualWidth);

        triggerCollider.offset = direction == SideRoadDirection.Left
            ? new Vector2(offsetX, 0f)
            : new Vector2(-offsetX, 0f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (playerInside)
            return;

        playerInside = true;
        AddActiveZone();
        PlayerEnteredSideRoad?.Invoke(this);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (!playerInside)
            return;

        playerInside = false;
        RemoveActiveZone();
        PlayerExitedSideRoad?.Invoke(this);
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
