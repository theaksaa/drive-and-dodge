using System.Collections.Generic;
using UnityEngine;

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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstanceExists()
    {
        if (FindAnyObjectByType<RoadVisualController>() == null)
            new GameObject(nameof(RoadVisualController)).AddComponent<RoadVisualController>();
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
            settings.SortingOrder);

        float leftAvailableWidth = Mathf.Max(0f, laneSystem.RoadLeftX - visibleLeftX);
        float leftEdgeWidth = Mathf.Min(settings.LeftEdgeWidth, leftAvailableWidth);
        float leftOutsideWidth = Mathf.Max(0f, leftAvailableWidth - leftEdgeWidth);

        if (leftEdgeWidth > 0f)
        {
            float leftEdgeCenterX = laneSystem.RoadLeftX - leftEdgeWidth * 0.5f;
            GameObject leftEdge = CreateSingleSpriteStrip(
                "LeftEdge",
                segmentRoot.transform,
                settings.LeftEdgeSprite,
                settings.EdgeColor,
                leftEdgeCenterX,
                leftEdgeWidth,
                settings.SegmentHeight,
                settings.SortingOrder - 1);
            leftEdge.transform.localScale = settings.LeftEdgeScale;
        }

        if (leftOutsideWidth > 0f)
        {
            float leftOutsideCenterX = visibleLeftX + leftOutsideWidth * 0.5f;
            GameObject leftOutside = CreateStrip(
                "LeftOutside",
                segmentRoot.transform,
                settings.LeftOutsideSprite,
                settings.OutsideColor,
                leftOutsideCenterX,
                leftOutsideWidth,
                settings.SegmentHeight,
                settings.SortingOrder - 2);
            leftOutside.transform.localScale = settings.LeftOutsideScale;
        }

        float rightAvailableWidth = Mathf.Max(0f, visibleRightX - laneSystem.RoadRightX);
        float rightEdgeWidth = Mathf.Min(settings.RightEdgeWidth, rightAvailableWidth);
        float rightOutsideWidth = Mathf.Max(0f, rightAvailableWidth - rightEdgeWidth);

        if (rightEdgeWidth > 0f)
        {
            float rightEdgeCenterX = laneSystem.RoadRightX + rightEdgeWidth * 0.5f;
            GameObject rightEdge = CreateSingleSpriteStrip(
                "RightEdge",
                segmentRoot.transform,
                settings.RightEdgeSprite,
                settings.EdgeColor,
                rightEdgeCenterX,
                rightEdgeWidth,
                settings.SegmentHeight,
                settings.SortingOrder - 1);
            rightEdge.transform.localScale = settings.RightEdgeScale;
        }

        if (rightOutsideWidth > 0f)
        {
            float rightOutsideCenterX = laneSystem.RoadRightX + rightEdgeWidth + rightOutsideWidth * 0.5f;
            GameObject rightOutside = CreateStrip(
                "RightOutside",
                segmentRoot.transform,
                settings.RightOutsideSprite,
                settings.OutsideColor,
                rightOutsideCenterX,
                rightOutsideWidth,
                settings.SegmentHeight,
                settings.SortingOrder - 2);
            rightOutside.transform.localScale = settings.RightOutsideScale;
        }

        for (int laneIndex = 1; laneIndex < laneSystem.LaneCount; laneIndex++)
        {
            float laneBoundaryX = laneSystem.GetLaneLeftX(laneIndex);
            GameObject laneDivider = CreateStrip(
                $"LaneDivider_{laneIndex}",
                segmentRoot.transform,
                settings.LaneDividerSprite,
                settings.LaneDividerColor,
                laneBoundaryX,
                settings.LaneDividerWidth,
                settings.SegmentHeight,
                settings.SortingOrder + 1);
            laneDivider.transform.localScale = settings.LaneDividerScale;
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
        int sortingOrder)
    {
        GameObject strip = new GameObject(objectName);
        strip.transform.SetParent(parent, false);
        strip.transform.localPosition = new Vector3(centerX, 0f, 0f);

        SpriteRenderer renderer = strip.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite != null ? sprite : GetFallbackSprite();
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        renderer.drawMode = SpriteDrawMode.Tiled;
        renderer.size = new Vector2(Mathf.Max(0.01f, width), Mathf.Max(0.01f, height));

        return strip;
    }

    private static GameObject CreateSingleSpriteStrip(
        string objectName,
        Transform parent,
        Sprite sprite,
        Color color,
        float centerX,
        float width,
        float height,
        int sortingOrder)
    {
        GameObject strip = new GameObject(objectName);
        strip.transform.SetParent(parent, false);
        strip.transform.localPosition = new Vector3(centerX, 0f, 0f);

        SpriteRenderer renderer = strip.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite != null ? sprite : GetFallbackSprite();
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        renderer.drawMode = SpriteDrawMode.Tiled;

        Vector2 spriteSize = renderer.sprite.bounds.size;
        float safeSpriteWidth = Mathf.Max(0.01f, spriteSize.x);
        renderer.size = new Vector2(safeSpriteWidth, Mathf.Max(0.01f, height));

        strip.transform.localScale = new Vector3(
            Mathf.Max(0.01f, width / safeSpriteWidth),
            1f,
            1f);

        return strip;
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
