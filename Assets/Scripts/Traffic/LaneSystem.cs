using UnityEngine;

public class LaneSystem : MonoBehaviour
{
    [Header("Lanes")]
    [SerializeField] private int laneCount = 3;
    [SerializeField] private float roadWidthPercent = 0.75f;

    [Header("Spawn")]
    [SerializeField] private float spawnYExtraOffset = 1.5f;

    private Camera mainCamera;

    public int LaneCount => laneCount;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    public float GetLaneX(int laneIndex)
    {
        Vector3 bottomLeft = GetBottomLeft();
        Vector3 topRight = GetTopRight();

        float screenWidth = topRight.x - bottomLeft.x;
        float roadWidth = screenWidth * roadWidthPercent;

        float roadLeftX = -roadWidth / 2f;
        float laneWidth = roadWidth / laneCount;

        return roadLeftX + laneWidth * laneIndex + laneWidth / 2f;
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

    public float GetSpawnY(TrafficVehicle trafficVehicle)
    {
        Vector3 topRight = GetTopRight();

        return topRight.y + trafficVehicle.GetHalfLength() + spawnYExtraOffset;
    }

    public float GetBottomY()
    {
        return GetBottomLeft().y;
    }

    private Vector3 GetBottomLeft()
    {
        float distanceFromCamera = Mathf.Abs(mainCamera.transform.position.z);

        return mainCamera.ViewportToWorldPoint(
            new Vector3(0f, 0f, distanceFromCamera)
        );
    }

    private Vector3 GetTopRight()
    {
        float distanceFromCamera = Mathf.Abs(mainCamera.transform.position.z);

        return mainCamera.ViewportToWorldPoint(
            new Vector3(1f, 1f, distanceFromCamera)
        );
    }
}
