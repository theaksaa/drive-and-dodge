using UnityEngine;

public class LaneSystem : MonoBehaviour
{
    [Header("Lanes")]
    [SerializeField] private int laneCount = 3;
    [SerializeField] private float laneWidth = 1.6f;

    [Header("Horizontal Margins")]
    [SerializeField] private float leftMargin = 1f;
    [SerializeField] private float rightMargin = 1f;

    [Header("Spawn")]
    [SerializeField] private float spawnYExtraOffset = 1.5f;

    [Header("Gizmos")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private bool drawLaneBoundaries = true;
    [SerializeField] private bool drawRoadBoundaries = true;
    [SerializeField] private bool drawCameraMargins = true;
    [SerializeField] private float gizmoLineTopY = 10f;
    [SerializeField] private float gizmoLineBottomY = -10f;

    private Camera mainCamera;

    public int LaneCount => laneCount;
    public float LaneWidth => laneWidth;

    public float LeftMargin => leftMargin;
    public float RightMargin => rightMargin;

    public float RoadWidth => laneCount * laneWidth;

    public float RoadLeftX => -RoadWidth / 2f;
    public float RoadRightX => RoadWidth / 2f;

    public float CameraAreaLeftX => RoadLeftX - leftMargin;
    public float CameraAreaRightX => RoadRightX + rightMargin;

    public float RequiredCameraWorldWidth => RoadWidth + leftMargin + rightMargin;

    private void Awake()
    {
        mainCamera = Camera.main;

        if (mainCamera == null)
            Debug.LogError("LaneSystem: Main Camera not found. Camera needs MainCamera tag.");
    }

    public float GetLaneX(int laneIndex)
    {
        laneIndex = Mathf.Clamp(laneIndex, 0, laneCount - 1);

        return RoadLeftX + laneWidth * laneIndex + laneWidth / 2f;
    }

    public float GetLaneLeftX(int laneIndex)
    {
        laneIndex = Mathf.Clamp(laneIndex, 0, laneCount - 1);

        return RoadLeftX + laneWidth * laneIndex;
    }

    public float GetLaneRightX(int laneIndex)
    {
        laneIndex = Mathf.Clamp(laneIndex, 0, laneCount - 1);

        return RoadLeftX + laneWidth * (laneIndex + 1);
    }

    public int GetClosestLaneIndex(float worldX)
    {
        int closestLane = 0;
        float closestDistance = Mathf.Infinity;

        for (int i = 0; i < laneCount; i++)
        {
            float laneX = GetLaneX(i);
            float distance = Mathf.Abs(worldX - laneX);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestLane = i;
            }
        }

        return closestLane;
    }

    public float GetClampedXInsideRoad(float worldX, float objectHalfWidth = 0f)
    {
        float minX = RoadLeftX + objectHalfWidth;
        float maxX = RoadRightX - objectHalfWidth;

        return Mathf.Clamp(worldX, minX, maxX);
    }

    public float GetSpawnY(TrafficVehicle trafficVehicle)
    {
        Vector3 topRight = GetTopRight();

        return topRight.y + trafficVehicle.GetHalfLength() + spawnYExtraOffset;
    }

    public float GetBottomY()
    {
        return GetBottomLeft().y;
    }

    public float GetTopY()
    {
        return GetTopRight().y;
    }

    public Vector3 GetBottomLeft()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            return Vector3.zero;

        float distanceFromCamera = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);

        return mainCamera.ViewportToWorldPoint(
            new Vector3(0f, 0f, distanceFromCamera)
        );
    }

    public Vector3 GetTopRight()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            return Vector3.zero;

        float distanceFromCamera = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);

        return mainCamera.ViewportToWorldPoint(
            new Vector3(1f, 1f, distanceFromCamera)
        );
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos)
            return;

        float roadWidth = laneCount * laneWidth;

        float roadLeftX = -roadWidth / 2f;
        float roadRightX = roadWidth / 2f;

        float cameraAreaLeftX = roadLeftX - leftMargin;
        float cameraAreaRightX = roadRightX + rightMargin;

        if (drawCameraMargins)
        {
            Gizmos.color = Color.green;

            Gizmos.DrawLine(
                new Vector3(cameraAreaLeftX, gizmoLineBottomY, 0f),
                new Vector3(cameraAreaLeftX, gizmoLineTopY, 0f)
            );

            Gizmos.DrawLine(
                new Vector3(cameraAreaRightX, gizmoLineBottomY, 0f),
                new Vector3(cameraAreaRightX, gizmoLineTopY, 0f)
            );
        }

        if (drawRoadBoundaries)
        {
            Gizmos.color = Color.cyan;

            Gizmos.DrawLine(
                new Vector3(roadLeftX, gizmoLineBottomY, 0f),
                new Vector3(roadLeftX, gizmoLineTopY, 0f)
            );

            Gizmos.DrawLine(
                new Vector3(roadRightX, gizmoLineBottomY, 0f),
                new Vector3(roadRightX, gizmoLineTopY, 0f)
            );
        }

        if (drawLaneBoundaries)
        {
            Gizmos.color = Color.yellow;

            for (int i = 1; i < laneCount; i++)
            {
                float boundaryX = roadLeftX + laneWidth * i;

                Gizmos.DrawLine(
                    new Vector3(boundaryX, gizmoLineBottomY, 0f),
                    new Vector3(boundaryX, gizmoLineTopY, 0f)
                );
            }
        }
    }
}