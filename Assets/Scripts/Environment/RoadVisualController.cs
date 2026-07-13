using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-90)]
public class RoadVisualController : MonoBehaviour
{
    private sealed class Segment
    {
        public Transform Root;
        public float CenterY;
    }

    private static Sprite fallbackSprite;

    private readonly List<Segment> segments = new List<Segment>();

    private Camera mainCamera;
    private LaneSystem laneSystem;
    private EnvironmentDefinition currentEnvironment;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterForSceneLoads()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInitialInstanceExists()
    {
        EnsureInstanceExists(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode loadMode)
    {
        EnsureInstanceExists(scene);
    }

    private static void EnsureInstanceExists(Scene targetScene)
    {
        RoadVisualController[] controllers = FindObjectsByType<RoadVisualController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (RoadVisualController controller in controllers)
        {
            if (controller != null && controller.gameObject.scene.Equals(targetScene))
                return;
        }

        GameObject controllerObject = new GameObject(nameof(RoadVisualController));
        SceneManager.MoveGameObjectToScene(controllerObject, targetScene);
        controllerObject.AddComponent<RoadVisualController>();
    }

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void OnEnable()
    {
        EnvironmentManager.EnvironmentChanged += HandleEnvironmentChanged;
    }

    private void Start()
    {
        RefreshReferences();

        currentEnvironment = EnvironmentManager.Instance != null
            ? EnvironmentManager.Instance.CurrentEnvironment
            : null;

        Rebuild();
    }

    private void OnDisable()
    {
        EnvironmentManager.EnvironmentChanged -= HandleEnvironmentChanged;
    }

    private void Update()
    {
        if (!HasVisualState())
            return;

        float moveSpeed = GameManager.Instance != null ? GameManager.Instance.CurrentGameSpeed : 0f;
        if (moveSpeed <= 0f)
            return;

        float delta = moveSpeed * Time.deltaTime;
        float segmentHeight = GetVisualSettings().SegmentHeight;
        float bottomLimit = laneSystem.GetBottomY() - segmentHeight * 0.5f;

        float highestY = float.MinValue;

        foreach (Segment segment in segments)
        {
            segment.CenterY -= delta;
            segment.Root.position = new Vector3(0f, segment.CenterY, 0f);
            highestY = Mathf.Max(highestY, segment.CenterY);
        }

        foreach (Segment segment in segments)
        {
            if (segment.CenterY + segmentHeight * 0.5f >= bottomLimit)
                continue;

            segment.CenterY = highestY + segmentHeight;
            segment.Root.position = new Vector3(0f, segment.CenterY, 0f);
            highestY = segment.CenterY;
        }
    }

    private void HandleEnvironmentChanged(EnvironmentDefinition environment)
    {
        currentEnvironment = environment;
        Rebuild();
    }

    private void Rebuild()
    {
        ClearSegments();
        RefreshReferences();

        if (laneSystem == null)
            return;

        EnvironmentDefinition.RoadVisualSettings settings = GetVisualSettings();
        float segmentHeight = settings.SegmentHeight;
        int segmentCount = Mathf.Max(2, settings.SegmentCount);
        float startY = laneSystem.GetBottomY() + segmentHeight * 0.5f;

        for (int i = 0; i < segmentCount; i++)
        {
            float centerY = startY + segmentHeight * i;
            Segment segment = BuildSegment(i, centerY, settings);
            segments.Add(segment);
        }
    }

    private Segment BuildSegment(int index, float centerY, EnvironmentDefinition.RoadVisualSettings settings)
    {
        GameObject segmentRoot = new GameObject($"RoadVisualSegment_{index}");
        segmentRoot.transform.SetParent(transform, false);
        segmentRoot.transform.position = new Vector3(0f, centerY, 0f);

        GetVisibleHorizontalBounds(out float visibleLeftX, out float visibleRightX);

        float roadCenterX = (laneSystem.RoadLeftX + laneSystem.RoadRightX) * 0.5f;
        CreateStrip(
            "RoadSurface",
            segmentRoot.transform,
            settings.RoadSurfaceSprite,
            settings.RoadSurfaceColor,
            roadCenterX,
            laneSystem.RoadWidth,
            settings.SegmentHeight,
            settings.SortingOrder,
            Vector3.one);

        float leftAvailableWidth = Mathf.Max(0f, laneSystem.RoadLeftX - visibleLeftX);
        float leftEdgeWidth = Mathf.Min(settings.LeftEdgeWidth, leftAvailableWidth);
        float leftOutsideWidth = Mathf.Max(0f, leftAvailableWidth - leftEdgeWidth);

        if (leftEdgeWidth > 0f)
        {
            CreateSingleSpriteStrip(
                "LeftEdge",
                segmentRoot.transform,
                settings.GetRandomLeftEdgeSprite(),
                settings.EdgeColor,
                laneSystem.RoadLeftX,
                settings.SegmentHeight,
                settings.SortingOrder - 1,
                settings.LeftEdgeScale,
                alignToLeftSide: true);
        }

        if (leftOutsideWidth > 0f)
        {
            float leftOutsideCenterX = visibleLeftX + leftOutsideWidth * 0.5f;
            CreateStrip(
                "LeftOutside",
                segmentRoot.transform,
                settings.GetRandomLeftOutsideSprite(),
                settings.OutsideColor,
                leftOutsideCenterX,
                leftOutsideWidth,
                settings.SegmentHeight,
                settings.SortingOrder - 2,
                settings.LeftOutsideScale);
        }

        float rightAvailableWidth = Mathf.Max(0f, visibleRightX - laneSystem.RoadRightX);
        float rightEdgeWidth = Mathf.Min(settings.RightEdgeWidth, rightAvailableWidth);
        float rightOutsideWidth = Mathf.Max(0f, rightAvailableWidth - rightEdgeWidth);

        if (rightEdgeWidth > 0f)
        {
            CreateSingleSpriteStrip(
                "RightEdge",
                segmentRoot.transform,
                settings.GetRandomRightEdgeSprite(),
                settings.EdgeColor,
                laneSystem.RoadRightX,
                settings.SegmentHeight,
                settings.SortingOrder - 1,
                settings.RightEdgeScale,
                alignToLeftSide: false);
        }

        if (rightOutsideWidth > 0f)
        {
            float rightOutsideCenterX = laneSystem.RoadRightX + rightEdgeWidth + rightOutsideWidth * 0.5f;
            CreateStrip(
                "RightOutside",
                segmentRoot.transform,
                settings.GetRandomRightOutsideSprite(),
                settings.OutsideColor,
                rightOutsideCenterX,
                rightOutsideWidth,
                settings.SegmentHeight,
                settings.SortingOrder - 2,
                settings.RightOutsideScale);
        }

        for (int laneIndex = 1; laneIndex < laneSystem.LaneCount; laneIndex++)
        {
            float laneBoundaryX = laneSystem.GetLaneLeftX(laneIndex);
            CreateStrip(
                $"LaneDivider_{laneIndex}",
                segmentRoot.transform,
                settings.GetRandomLaneDividerSprite(),
                settings.LaneDividerColor,
                laneBoundaryX,
                settings.LaneDividerWidth,
                settings.SegmentHeight,
                settings.SortingOrder + 1,
                settings.LaneDividerScale);
        }

        return new Segment
        {
            Root = segmentRoot.transform,
            CenterY = centerY
        };
    }

    private static GameObject CreateStrip(
        string objectName,
        Transform parent,
        Sprite sprite,
        Color color,
        float centerX,
        float width,
        float height,
        int sortingOrder,
        Vector3 scale)
    {
        GameObject strip = new GameObject(objectName);
        strip.transform.SetParent(parent, false);
        strip.transform.localPosition = new Vector3(centerX, 0f, 0f);

        SpriteRenderer renderer = strip.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite != null ? sprite : GetFallbackSprite();
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        renderer.drawMode = SpriteDrawMode.Tiled;
        Vector3 safeScale = GetSafeScale(scale);
        Vector3 alignedScale = GetVerticallyTiledScale(renderer.sprite, height, safeScale);
        renderer.size = new Vector2(
            Mathf.Max(0.01f, width),
            GetWholeTileLocalHeight(renderer.sprite, height, safeScale.y));
        strip.transform.localScale = alignedScale;

        return strip;
    }

    private static GameObject CreateSingleSpriteStrip(
        string objectName,
        Transform parent,
        Sprite sprite,
        Color color,
        float roadEdgeX,
        float height,
        int sortingOrder,
        Vector3 scale,
        bool alignToLeftSide)
    {
        GameObject strip = new GameObject(objectName);
        strip.transform.SetParent(parent, false);

        SpriteRenderer renderer = strip.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite != null ? sprite : GetFallbackSprite();
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        renderer.drawMode = SpriteDrawMode.Tiled;
        Vector3 safeScale = GetSafeScale(scale);
        Vector3 alignedScale = GetVerticallyTiledScale(renderer.sprite, height, safeScale);
        float spriteWidth = Mathf.Max(0.01f, renderer.sprite.bounds.size.x);
        renderer.size = new Vector2(
            spriteWidth,
            GetWholeTileLocalHeight(renderer.sprite, height, safeScale.y));
        strip.transform.localScale = alignedScale;
        float halfRenderedWidth = spriteWidth * alignedScale.x * 0.5f;
        float centerX = alignToLeftSide
            ? roadEdgeX - halfRenderedWidth
            : roadEdgeX + halfRenderedWidth;
        strip.transform.localPosition = new Vector3(centerX, 0f, 0f);

        return strip;
    }

    private static Vector3 GetSafeScale(Vector3 scale)
    {
        return new Vector3(
            Mathf.Max(0.01f, Mathf.Abs(scale.x)),
            Mathf.Max(0.01f, Mathf.Abs(scale.y)),
            Mathf.Max(0.01f, Mathf.Abs(scale.z)));
    }

    private static float GetWholeTileLocalHeight(Sprite sprite, float desiredWorldHeight, float preferredScaleY)
    {
        float tileLocalHeight = Mathf.Max(0.01f, sprite.bounds.size.y);
        float safeScaleY = Mathf.Max(0.01f, Mathf.Abs(preferredScaleY));
        int tileCount = Mathf.Max(1, Mathf.RoundToInt(desiredWorldHeight / (tileLocalHeight * safeScaleY)));
        return tileLocalHeight * tileCount;
    }

    private static Vector3 GetVerticallyTiledScale(Sprite sprite, float desiredWorldHeight, Vector3 preferredScale)
    {
        float wholeTileLocalHeight = GetWholeTileLocalHeight(sprite, desiredWorldHeight, preferredScale.y);
        float adjustedScaleY = Mathf.Max(0.01f, desiredWorldHeight / wholeTileLocalHeight);
        return new Vector3(preferredScale.x, adjustedScaleY, preferredScale.z);
    }

    private bool HasVisualState()
    {
        return laneSystem != null && segments.Count > 0;
    }

    private EnvironmentDefinition.RoadVisualSettings GetVisualSettings()
    {
        return currentEnvironment != null
            ? currentEnvironment.RoadVisuals
            : new EnvironmentDefinition.RoadVisualSettings();
    }

    private void RefreshReferences()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (laneSystem == null)
            laneSystem = FindAnyObjectByType<LaneSystem>();
    }

    private void GetVisibleHorizontalBounds(out float leftX, out float rightX)
    {
        if (mainCamera == null)
        {
            leftX = laneSystem.CameraAreaLeftX;
            rightX = laneSystem.CameraAreaRightX;
            return;
        }

        float distanceFromCamera = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);
        Vector3 bottomLeft = mainCamera.ViewportToWorldPoint(new Vector3(0f, 0f, distanceFromCamera));
        Vector3 bottomRight = mainCamera.ViewportToWorldPoint(new Vector3(1f, 0f, distanceFromCamera));

        leftX = bottomLeft.x;
        rightX = bottomRight.x;
    }

    private void ClearSegments()
    {
        foreach (Segment segment in segments)
        {
            if (segment?.Root != null)
                Destroy(segment.Root.gameObject);
        }

        segments.Clear();
    }

    private static Sprite GetFallbackSprite()
    {
        if (fallbackSprite != null)
            return fallbackSprite;

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Point;

        fallbackSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        return fallbackSprite;
    }
}
