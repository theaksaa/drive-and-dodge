using System.Collections.Generic;
using UnityEngine;

public sealed class SideRoadVisual : MonoBehaviour
{
    private const int OverlayOrderOffset = 10;

    private static Sprite fallbackSprite;

    private Transform visualRoot;
    private Material clipMaterial;
    private readonly List<Material> stripMaterials = new List<Material>();

    public void Build(
        SideRoadDirection direction,
        float availableWidth,
        float availableHeight,
        float connectionOverlap,
        float centerLineRiseRatio,
        float roadThicknessRatio,
        float edgeThicknessRatio,
        float outerExtensionMultiplier)
    {
        Clear();

        EnvironmentDefinition.RoadVisualSettings settings =
            EnvironmentManager.Instance != null && EnvironmentManager.Instance.CurrentEnvironment != null
                ? EnvironmentManager.Instance.CurrentEnvironment.RoadVisuals
                : new EnvironmentDefinition.RoadVisualSettings();

        visualRoot = new GameObject("GeneratedVisual").transform;
        visualRoot.SetParent(transform, false);

        float safeWidth = Mathf.Max(0.01f, availableWidth);
        float safeHeight = Mathf.Max(0.01f, availableHeight);
        float overlap = Mathf.Max(0f, connectionOverlap);
        float rise = safeHeight * Mathf.Max(0.01f, centerLineRiseRatio);
        float roadThickness = safeHeight * Mathf.Max(0.01f, roadThicknessRatio);
        float safeEdgeThicknessRatio = Mathf.Clamp(edgeThicknessRatio, 0.01f, 0.49f);
        float outerExtension = Mathf.Max(
            roadThickness * Mathf.Max(0f, outerExtensionMultiplier),
            overlap);

        CreateClipMaterial(direction, safeWidth);

        Vector2 outerPoint;
        Vector2 junctionPoint;

        if (direction == SideRoadDirection.Left)
        {
            outerPoint = new Vector2(-safeWidth * 0.5f - outerExtension, rise * 0.5f);
            junctionPoint = new Vector2(safeWidth * 0.5f + overlap, -rise * 0.5f);
        }
        else
        {
            junctionPoint = new Vector2(-safeWidth * 0.5f - overlap, -rise * 0.5f);
            outerPoint = new Vector2(safeWidth * 0.5f + outerExtension, rise * 0.5f);
        }

        Vector2 center = (outerPoint + junctionPoint) * 0.5f;
        Vector2 roadVector = outerPoint - junctionPoint;
        float roadLength = Mathf.Max(0.01f, roadVector.magnitude);
        Vector2 roadDirection = roadVector / roadLength;
        Vector2 normal = new Vector2(-roadDirection.y, roadDirection.x);
        float roadAngle = Mathf.Atan2(roadDirection.y, roadDirection.x) * Mathf.Rad2Deg;
        int baseOrder = settings.SortingOrder + OverlayOrderOffset;

        CreateLongStrip(
            "Surface",
            settings.RoadSurfaceSprite,
            settings.RoadSurfaceColor,
            center,
            roadAngle,
            roadThickness,
            roadLength,
            baseOrder,
            Vector3.one);

        float leftEdgeWidth = Mathf.Min(settings.LeftEdgeWidth, roadThickness * safeEdgeThicknessRatio);
        float rightEdgeWidth = Mathf.Min(settings.RightEdgeWidth, roadThickness * safeEdgeThicknessRatio);

        if (leftEdgeWidth > 0f)
        {
            CreateLongStrip(
                "UpperEdge",
                settings.GetRandomLeftEdgeSprite(),
                settings.EdgeColor,
                center + normal * (roadThickness * 0.5f - leftEdgeWidth * 0.5f),
                roadAngle,
                leftEdgeWidth,
                roadLength,
                baseOrder + 1,
                settings.LeftEdgeScale);
        }

        if (rightEdgeWidth > 0f)
        {
            CreateLongStrip(
                "LowerEdge",
                settings.GetRandomRightEdgeSprite(),
                settings.EdgeColor,
                center - normal * (roadThickness * 0.5f - rightEdgeWidth * 0.5f),
                roadAngle,
                rightEdgeWidth,
                roadLength,
                baseOrder + 1,
                settings.RightEdgeScale);
        }

    }

    public void Clear()
    {
        DestroyStripMaterials();

        if (clipMaterial != null)
        {
            Destroy(clipMaterial);
            clipMaterial = null;
        }

        if (visualRoot != null)
        {
            Destroy(visualRoot.gameObject);
            visualRoot = null;
        }
    }

    private void OnDestroy()
    {
        DestroyStripMaterials();

        if (clipMaterial != null)
            Destroy(clipMaterial);
    }

    private void CreateLongStrip(
        string objectName,
        Sprite sprite,
        Color color,
        Vector2 center,
        float roadAngle,
        float width,
        float length,
        int sortingOrder,
        Vector3 preferredScale)
    {
        GameObject stripRoot = new GameObject(objectName);
        stripRoot.transform.SetParent(visualRoot, false);
        stripRoot.transform.localPosition = new Vector3(center.x, center.y, 0f);

        // Source road sprites run vertically, so local Y is kept along the branch.
        stripRoot.transform.localRotation = Quaternion.Euler(0f, 0f, roadAngle - 90f);

        Sprite resolvedSprite = sprite != null ? sprite : GetFallbackSprite();
        Vector3 safeScale = GetSafeScale(preferredScale);
        float nativeWidth = Mathf.Max(0.01f, resolvedSprite.bounds.size.x);
        float nativeLength = Mathf.Max(0.01f, resolvedSprite.bounds.size.y);
        float preferredTileLength = nativeLength * safeScale.y;
        int tileCount = Mathf.Max(1, Mathf.CeilToInt(length / preferredTileLength));
        float tileLength = length / tileCount;
        float startY = -length * 0.5f + tileLength * 0.5f;

        for (int tileIndex = 0; tileIndex < tileCount; tileIndex++)
        {
            GameObject tile = new GameObject($"{objectName}_Tile_{tileIndex}");
            tile.transform.SetParent(stripRoot.transform, false);
            tile.transform.localPosition = new Vector3(0f, startY + tileLength * tileIndex, 0f);
            tile.transform.localScale = new Vector3(
                width / nativeWidth,
                tileLength / nativeLength,
                safeScale.z);

            SpriteRenderer renderer = tile.AddComponent<SpriteRenderer>();
            renderer.sprite = resolvedSprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            renderer.drawMode = SpriteDrawMode.Simple;

            if (clipMaterial == null)
                continue;

            Material stripMaterial = new Material(clipMaterial)
            {
                name = $"SideRoadClipMaterial_{objectName}_{tileIndex}"
            };
            stripMaterial.SetTexture("_MainTex", renderer.sprite.texture);
            renderer.sharedMaterial = stripMaterial;
            stripMaterials.Add(stripMaterial);
        }
    }

    private void DestroyStripMaterials()
    {
        foreach (Material material in stripMaterials)
        {
            if (material != null)
                Destroy(material);
        }

        stripMaterials.Clear();
    }

    private void CreateClipMaterial(SideRoadDirection direction, float availableWidth)
    {
        Shader shader = Shader.Find("Drive And Dodge/Side Road Clip");

        if (shader == null)
        {
            Debug.LogError("SideRoadVisual: Could not find the side-road clipping shader.");
            return;
        }

        clipMaterial = new Material(shader)
        {
            name = "SideRoadClipMaterial"
        };

        float localBoundaryX = direction == SideRoadDirection.Left
            ? availableWidth * 0.5f
            : -availableWidth * 0.5f;
        float worldBoundaryX = transform.TransformPoint(new Vector3(localBoundaryX, 0f, 0f)).x;

        clipMaterial.SetFloat("_ClipX", worldBoundaryX);
        clipMaterial.SetFloat(
            "_ClipDirection",
            direction == SideRoadDirection.Left ? -1f : 1f);
    }

    private static Vector3 GetSafeScale(Vector3 scale)
    {
        return new Vector3(
            Mathf.Max(0.01f, Mathf.Abs(scale.x)),
            Mathf.Max(0.01f, Mathf.Abs(scale.y)),
            Mathf.Max(0.01f, Mathf.Abs(scale.z)));
    }

    private static Sprite GetFallbackSprite()
    {
        if (fallbackSprite != null)
            return fallbackSprite;

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.name = "SideRoadFallbackTexture";
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Point;

        fallbackSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f);
        fallbackSprite.name = "SideRoadFallbackSprite";

        return fallbackSprite;
    }
}
