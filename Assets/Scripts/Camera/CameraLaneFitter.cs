using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraLaneFitter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LaneSystem laneSystem;
    [SerializeField] private Transform playerTarget;

    [Header("Fit")]
    [SerializeField] private bool fitOnStart = true;
    [SerializeField] private bool fitEveryFrame = false;

    [Header("Camera Limits")]
    [SerializeField] private float minOrthographicSize = 5f;
    [SerializeField] private float maxOrthographicSize = 10f;

    [Header("Camera Position")]
    [SerializeField] private float cameraZ = -10f;

    [Header("Player Screen Anchor")]
    [SerializeField] private bool keepPlayerAtInitialScreenPosition = true;

    private Camera targetCamera;
    private Vector2 initialPlayerViewportPosition = new(0.5f, 0.5f);
    private bool hasCachedInitialViewportPosition;

    private void Awake()
    {
        targetCamera = GetComponent<Camera>();
        targetCamera.orthographic = true;
    }

    private void Start()
    {
        CacheInitialPlayerViewportPosition();

        if (fitOnStart)
            FitCameraToLanes();
    }

    private void Update()
    {
        if (fitEveryFrame)
            FitCameraToLanes();
    }

    private void OnValidate()
    {
        Camera cam = GetComponent<Camera>();

        if (cam != null)
            cam.orthographic = true;
    }

    public void FitCameraToLanes()
    {
        if (targetCamera == null)
            targetCamera = GetComponent<Camera>();

        if (laneSystem == null)
        {
            Debug.LogWarning("CameraLaneFitter: LaneSystem is not assigned.");
            return;
        }

        float requiredWorldWidth = laneSystem.RequiredCameraWorldWidth;
        float requiredOrthographicSize = requiredWorldWidth / (2f * targetCamera.aspect);

        targetCamera.orthographicSize = Mathf.Clamp(
            requiredOrthographicSize,
            minOrthographicSize,
            maxOrthographicSize
        );

        Vector3 fittedCameraPosition = transform.position;
        fittedCameraPosition.z = cameraZ;

        if (keepPlayerAtInitialScreenPosition && playerTarget != null && hasCachedInitialViewportPosition)
        {
            float halfHeight = targetCamera.orthographicSize;
            float halfWidth = halfHeight * targetCamera.aspect;

            fittedCameraPosition.x = playerTarget.position.x - (initialPlayerViewportPosition.x - 0.5f) * 2f * halfWidth;
            fittedCameraPosition.y = playerTarget.position.y - (initialPlayerViewportPosition.y - 0.5f) * 2f * halfHeight;
        }

        transform.position = fittedCameraPosition;
    }

    private void CacheInitialPlayerViewportPosition()
    {
        if (targetCamera == null || playerTarget == null)
        {
            hasCachedInitialViewportPosition = false;
            return;
        }

        initialPlayerViewportPosition = targetCamera.WorldToViewportPoint(playerTarget.position);
        hasCachedInitialViewportPosition = true;
    }
}
