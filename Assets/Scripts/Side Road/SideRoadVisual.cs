using System.Collections.Generic;
using UnityEngine;

public sealed class SideRoadVisual : MonoBehaviour
{
    private const int OverlayOrderOffset = 10;
    private const float JunctionSeamPadding = 0.05f;

    private static Sprite fallbackSprite;

    private Transform visualRoot;
    private Material clipMaterial;
    private readonly List<Material> stripMaterials = new List<Material>();
    private readonly List<Mesh> stripMeshes = new List<Mesh>();

    public Vector2[] Build(
        SideRoadDirection direction,
        float availableWidth,
        float availableHeight,
        float connectionOverlap,
        float centerLineRiseRatio,
        float roadThicknessRatio,
        float edgeThicknessRatio,
        float outerExtensionMultiplier,
        float outerScreenPadding,
        float curveStrength,
        int curveSegments)
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
        float minimumHiddenDepth = roadThickness * 0.5f + JunctionSeamPadding;
        float minimumOuterHiddenDepth = minimumHiddenDepth + Mathf.Max(0f, outerScreenPadding);
        float outerExtension = Mathf.Max(
            Mathf.Max(
                roadThickness * Mathf.Max(0f, outerExtensionMultiplier),
                minimumOuterHiddenDepth),
            overlap);
        float hiddenJunctionDepth = Mathf.Max(
            overlap,
            minimumHiddenDepth);

        CreateClipMaterial(direction, safeWidth);

        Vector2 outerPoint;
        Vector2 junctionPoint;

        if (direction == SideRoadDirection.Left)
        {
            outerPoint = new Vector2(-safeWidth * 0.5f - outerExtension, rise * 0.5f);
            junctionPoint = new Vector2(safeWidth * 0.5f + hiddenJunctionDepth, -rise * 0.5f);
        }
        else
        {
            junctionPoint = new Vector2(-safeWidth * 0.5f - hiddenJunctionDepth, -rise * 0.5f);
            outerPoint = new Vector2(safeWidth * 0.5f + outerExtension, rise * 0.5f);
        }

        BuildCurve(
            junctionPoint,
            outerPoint,
            Mathf.Clamp01(curveStrength),
            Mathf.Clamp(curveSegments, 4, 64),
            out Vector2[] curvePoints,
            out Vector2[] curveNormals);

        int baseOrder = settings.SortingOrder + OverlayOrderOffset;

        CreateCurvedStrip(
            "Surface",
            settings.RoadSurfaceSprite,
            settings.RoadSurfaceColor,
            curvePoints,
            curveNormals,
            0f,
            roadThickness,
            baseOrder,
            1f);

        float leftEdgeWidth = Mathf.Min(settings.LeftEdgeWidth, roadThickness * safeEdgeThicknessRatio);
        float rightEdgeWidth = Mathf.Min(settings.RightEdgeWidth, roadThickness * safeEdgeThicknessRatio);

        if (leftEdgeWidth > 0f)
        {
            CreateCurvedStrip(
                "UpperEdge",
                settings.GetRandomLeftEdgeSprite(),
                settings.EdgeColor,
                curvePoints,
                curveNormals,
                roadThickness * 0.5f - leftEdgeWidth * 0.5f,
                leftEdgeWidth,
                baseOrder + 1,
                settings.LeftEdgeScale.y);
        }

        if (rightEdgeWidth > 0f)
        {
            CreateCurvedStrip(
                "LowerEdge",
                settings.GetRandomRightEdgeSprite(),
                settings.EdgeColor,
                curvePoints,
                curveNormals,
                -(roadThickness * 0.5f - rightEdgeWidth * 0.5f),
                rightEdgeWidth,
                baseOrder + 1,
                settings.RightEdgeScale.y);
        }

        return BuildRoadPolygon(curvePoints, curveNormals, roadThickness);
    }

    public void Clear()
    {
        DestroyGeneratedResources();

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
        DestroyGeneratedResources();

        if (clipMaterial != null)
            Destroy(clipMaterial);
    }

    private static void BuildCurve(
        Vector2 junctionPoint,
        Vector2 outerPoint,
        float curveStrength,
        int curveSegments,
        out Vector2[] points,
        out Vector2[] normals)
    {
        Vector2 chord = outerPoint - junctionPoint;
        float chordLength = Mathf.Max(0.01f, chord.magnitude);
        float verticalDirection = Mathf.Sign(chord.y);
        float handleLength = chordLength / 3f;

        Vector2 linearControlA = junctionPoint + chord / 3f;
        Vector2 linearControlB = junctionPoint + chord * (2f / 3f);
        Vector2 curvedControlA = junctionPoint + Vector2.up * (verticalDirection * handleLength);
        Vector2 curvedControlB = outerPoint - Vector2.up * (verticalDirection * handleLength);
        Vector2 controlA = Vector2.Lerp(linearControlA, curvedControlA, curveStrength);
        Vector2 controlB = Vector2.Lerp(linearControlB, curvedControlB, curveStrength);

        points = new Vector2[curveSegments + 1];
        normals = new Vector2[curveSegments + 1];

        for (int index = 0; index <= curveSegments; index++)
        {
            float t = index / (float)curveSegments;
            points[index] = EvaluateCubicBezier(
                junctionPoint,
                controlA,
                controlB,
                outerPoint,
                t);

            Vector2 tangent = EvaluateCubicBezierTangent(
                junctionPoint,
                controlA,
                controlB,
                outerPoint,
                t);

            if (tangent.sqrMagnitude <= Mathf.Epsilon)
                tangent = chord;

            tangent.Normalize();
            normals[index] = new Vector2(-tangent.y, tangent.x);
        }
    }

    private static Vector2 EvaluateCubicBezier(
        Vector2 start,
        Vector2 controlA,
        Vector2 controlB,
        Vector2 end,
        float t)
    {
        float inverseT = 1f - t;
        return inverseT * inverseT * inverseT * start
            + 3f * inverseT * inverseT * t * controlA
            + 3f * inverseT * t * t * controlB
            + t * t * t * end;
    }

    private static Vector2 EvaluateCubicBezierTangent(
        Vector2 start,
        Vector2 controlA,
        Vector2 controlB,
        Vector2 end,
        float t)
    {
        float inverseT = 1f - t;
        return 3f * inverseT * inverseT * (controlA - start)
            + 6f * inverseT * t * (controlB - controlA)
            + 3f * t * t * (end - controlB);
    }

    private void CreateCurvedStrip(
        string objectName,
        Sprite sprite,
        Color color,
        IReadOnlyList<Vector2> curvePoints,
        IReadOnlyList<Vector2> curveNormals,
        float centerOffset,
        float width,
        int sortingOrder,
        float preferredTileScaleY)
    {
        Sprite resolvedSprite = sprite != null ? sprite : GetFallbackSprite();
        GetSpriteUvBounds(resolvedSprite, out Vector2 uvMin, out Vector2 uvMax);

        float totalLength = 0f;
        float[] curveDistances = new float[curvePoints.Count];

        for (int index = 1; index < curvePoints.Count; index++)
        {
            totalLength += Vector2.Distance(curvePoints[index - 1], curvePoints[index]);
            curveDistances[index] = totalLength;
        }

        totalLength = Mathf.Max(0.01f, totalLength);
        float nativeTileLength = Mathf.Max(0.01f, resolvedSprite.bounds.size.y);
        float safeTileScaleY = Mathf.Max(0.01f, Mathf.Abs(preferredTileScaleY));
        int tileCount = Mathf.Max(
            1,
            Mathf.CeilToInt(totalLength / (nativeTileLength * safeTileScaleY)));
        float tileLength = totalLength / tileCount;

        BuildStripStations(
            curvePoints,
            curveNormals,
            curveDistances,
            totalLength,
            tileLength,
            out List<Vector2> stripPoints,
            out List<Vector2> stripNormals,
            out List<float> stripDistances);

        int spanCount = stripPoints.Count - 1;
        Vector3[] vertices = new Vector3[spanCount * 4];
        Vector2[] uvs = new Vector2[spanCount * 4];
        Color[] colors = new Color[spanCount * 4];
        int[] triangles = new int[spanCount * 6];

        for (int spanIndex = 0; spanIndex < spanCount; spanIndex++)
        {
            Vector2 startNormal = stripNormals[spanIndex];
            Vector2 endNormal = stripNormals[spanIndex + 1];
            Vector2 startCenter = stripPoints[spanIndex] + startNormal * centerOffset;
            Vector2 endCenter = stripPoints[spanIndex + 1] + endNormal * centerOffset;
            Vector2 startHalfWidth = startNormal * (width * 0.5f);
            Vector2 endHalfWidth = endNormal * (width * 0.5f);
            float startDistance = stripDistances[spanIndex];
            float endDistance = stripDistances[spanIndex + 1];
            float middleDistance = (startDistance + endDistance) * 0.5f;
            int tileIndex = Mathf.Clamp(
                Mathf.FloorToInt(middleDistance / tileLength),
                0,
                tileCount - 1);
            float tileStartDistance = tileIndex * tileLength;
            float startVRatio = Mathf.Clamp01((startDistance - tileStartDistance) / tileLength);
            float endVRatio = Mathf.Clamp01((endDistance - tileStartDistance) / tileLength);
            float startV = Mathf.Lerp(uvMin.y, uvMax.y, startVRatio);
            float endV = Mathf.Lerp(uvMin.y, uvMax.y, endVRatio);
            int vertexIndex = spanIndex * 4;

            vertices[vertexIndex] = startCenter + startHalfWidth;
            vertices[vertexIndex + 1] = startCenter - startHalfWidth;
            vertices[vertexIndex + 2] = endCenter + endHalfWidth;
            vertices[vertexIndex + 3] = endCenter - endHalfWidth;
            uvs[vertexIndex] = new Vector2(uvMin.x, startV);
            uvs[vertexIndex + 1] = new Vector2(uvMax.x, startV);
            uvs[vertexIndex + 2] = new Vector2(uvMin.x, endV);
            uvs[vertexIndex + 3] = new Vector2(uvMax.x, endV);
            colors[vertexIndex] = color;
            colors[vertexIndex + 1] = color;
            colors[vertexIndex + 2] = color;
            colors[vertexIndex + 3] = color;

            int triangleIndex = spanIndex * 6;
            triangles[triangleIndex] = vertexIndex;
            triangles[triangleIndex + 1] = vertexIndex + 2;
            triangles[triangleIndex + 2] = vertexIndex + 1;
            triangles[triangleIndex + 3] = vertexIndex + 1;
            triangles[triangleIndex + 4] = vertexIndex + 2;
            triangles[triangleIndex + 5] = vertexIndex + 3;
        }

        Mesh mesh = new Mesh
        {
            name = $"SideRoad_{objectName}_Mesh",
            vertices = vertices,
            uv = uvs,
            colors = colors,
            triangles = triangles
        };
        mesh.RecalculateBounds();
        stripMeshes.Add(mesh);

        GameObject strip = new GameObject(objectName);
        strip.transform.SetParent(visualRoot, false);
        strip.transform.localScale = Vector3.one;

        MeshFilter meshFilter = strip.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = mesh;

        MeshRenderer meshRenderer = strip.AddComponent<MeshRenderer>();
        meshRenderer.sortingOrder = sortingOrder;

        Shader shader = clipMaterial != null
            ? clipMaterial.shader
            : Shader.Find("Sprites/Default");

        if (shader == null)
        {
            Debug.LogError("SideRoadVisual: Could not find a shader for the curved road strip.");
            return;
        }

        Material stripMaterial = clipMaterial != null
            ? new Material(clipMaterial)
            : new Material(shader);
        stripMaterial.name = $"SideRoadMaterial_{objectName}";
        stripMaterial.SetTexture("_MainTex", resolvedSprite.texture);
        meshRenderer.sharedMaterial = stripMaterial;
        stripMaterials.Add(stripMaterial);
    }

    private static void BuildStripStations(
        IReadOnlyList<Vector2> curvePoints,
        IReadOnlyList<Vector2> curveNormals,
        IReadOnlyList<float> curveDistances,
        float totalLength,
        float tileLength,
        out List<Vector2> stripPoints,
        out List<Vector2> stripNormals,
        out List<float> stripDistances)
    {
        stripPoints = new List<Vector2> { curvePoints[0] };
        stripNormals = new List<Vector2> { curveNormals[0] };
        stripDistances = new List<float> { 0f };

        for (int curveIndex = 1; curveIndex < curvePoints.Count; curveIndex++)
        {
            float startDistance = curveDistances[curveIndex - 1];
            float endDistance = curveDistances[curveIndex];
            float segmentLength = Mathf.Max(0.0001f, endDistance - startDistance);
            int nextTileIndex = Mathf.FloorToInt(startDistance / tileLength) + 1;
            float boundaryDistance = nextTileIndex * tileLength;

            while (boundaryDistance < endDistance - 0.0001f
                && boundaryDistance < totalLength - 0.0001f)
            {
                float interpolation = (boundaryDistance - startDistance) / segmentLength;
                Vector2 normal = Vector2.Lerp(
                    curveNormals[curveIndex - 1],
                    curveNormals[curveIndex],
                    interpolation).normalized;

                stripPoints.Add(Vector2.Lerp(
                    curvePoints[curveIndex - 1],
                    curvePoints[curveIndex],
                    interpolation));
                stripNormals.Add(normal);
                stripDistances.Add(boundaryDistance);

                nextTileIndex++;
                boundaryDistance = nextTileIndex * tileLength;
            }

            stripPoints.Add(curvePoints[curveIndex]);
            stripNormals.Add(curveNormals[curveIndex]);
            stripDistances.Add(endDistance);
        }
    }

    private static Vector2[] BuildRoadPolygon(
        IReadOnlyList<Vector2> curvePoints,
        IReadOnlyList<Vector2> curveNormals,
        float roadThickness)
    {
        int stationCount = curvePoints.Count;
        Vector2[] polygon = new Vector2[stationCount * 2];
        float halfWidth = roadThickness * 0.5f;

        for (int index = 0; index < stationCount; index++)
        {
            polygon[index] = curvePoints[index] + curveNormals[index] * halfWidth;
            int oppositeIndex = polygon.Length - 1 - index;
            polygon[oppositeIndex] = curvePoints[index] - curveNormals[index] * halfWidth;
        }

        return polygon;
    }

    private void DestroyGeneratedResources()
    {
        foreach (Material material in stripMaterials)
        {
            if (material != null)
                Destroy(material);
        }

        stripMaterials.Clear();

        foreach (Mesh mesh in stripMeshes)
        {
            if (mesh != null)
                Destroy(mesh);
        }

        stripMeshes.Clear();
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

    private static void GetSpriteUvBounds(Sprite sprite, out Vector2 min, out Vector2 max)
    {
        Vector2[] spriteUvs = sprite.uv;
        min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

        foreach (Vector2 uv in spriteUvs)
        {
            min = Vector2.Min(min, uv);
            max = Vector2.Max(max, uv);
        }
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
