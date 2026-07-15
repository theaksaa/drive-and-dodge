using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-100)]
public class EnvironmentManager : MonoBehaviour
{
    public static EnvironmentManager Instance { get; private set; }
    public static event Action<EnvironmentDefinition> EnvironmentChanged;

    [Header("Optional Starting Environment")]
    [SerializeField] private EnvironmentDefinition startingEnvironment;

    [Header("Side Road Transition")]
    [Min(0.05f)] [SerializeField] private float sideRoadDriveDuration = 0.65f;
    [Tooltip("Extra distance the player travels past the camera edge before the side-road drive ends.")]
    [Min(0f)] [SerializeField] private float sideRoadDriveDistance = 0.75f;
    [Min(0f)] [SerializeField] private float sideRoadDriveRise = 0.7f;
    [Range(0f, 90f)] [SerializeField] private float sideRoadTurnAngle = 55f;
    [Min(0.01f)] [SerializeField] private float fadeOutDuration = 0.3f;
    [Min(0.01f)] [SerializeField] private float fadeInDuration = 0.35f;

    private EnvironmentDefinition currentEnvironment;
    private GameObject currentEnvironmentVisual;
    private Image fadeImage;
    private bool sideRoadTransitionInProgress;

    public EnvironmentDefinition CurrentEnvironment => currentEnvironment;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstanceExists()
    {
        if (FindAnyObjectByType<EnvironmentManager>() == null)
            new GameObject(nameof(EnvironmentManager)).AddComponent<EnvironmentManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // A replacement scene may awaken before the previous scene is fully
            // unloaded. Transfer ownership across scenes instead of deleting the
            // new manager and leaving Instance null after the old one is destroyed.
            if (Instance.gameObject.scene.Equals(gameObject.scene))
            {
                Destroy(gameObject);
                return;
            }
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void OnEnable()
    {
        SideRoad.PlayerEnteredSideRoad += HandleSideRoadEntered;
    }

    private void Start()
    {
        if (startingEnvironment != null)
            SwitchEnvironment(startingEnvironment);
    }

    private void OnDisable()
    {
        SideRoad.PlayerEnteredSideRoad -= HandleSideRoadEntered;
    }

    private void HandleSideRoadEntered(SideRoad sideRoad)
    {
        if (sideRoad == null || sideRoad.DestinationEnvironment == null ||
            sideRoadTransitionInProgress)
            return;

        StartCoroutine(TransitionThroughSideRoad(sideRoad));
    }

    private IEnumerator TransitionThroughSideRoad(SideRoad sideRoad)
    {
        sideRoadTransitionInProgress = true;
        EnvironmentDefinition destination = sideRoad.DestinationEnvironment;

        PlayerController playerController = FindAnyObjectByType<PlayerController>();
        Transform playerTransform = playerController != null ? playerController.transform : null;
        Quaternion startingRotation = playerTransform != null
            ? playerTransform.rotation
            : Quaternion.identity;

        playerController?.BeginExternalMovement();

        if (playerTransform != null)
            yield return AnimatePlayerIntoSideRoad(playerTransform, sideRoad, startingRotation);

        EnsureFadeOverlay();
        yield return FadeTo(1f, fadeOutDuration, playerTransform, sideRoad);

        if (destination != null)
            SwitchEnvironment(destination);

        if (playerTransform != null)
            playerTransform.rotation = startingRotation;

        // Let destroyed world objects and the new environment visual settle while
        // the screen is fully covered.
        yield return null;
        yield return FadeTo(0f, fadeInDuration);

        playerController?.EndExternalMovement();
        sideRoadTransitionInProgress = false;
    }

    private IEnumerator AnimatePlayerIntoSideRoad(
        Transform playerTransform,
        SideRoad sideRoad,
        Quaternion startingRotation)
    {
        Vector3 startingPosition = playerTransform.position;
        Vector3 startingSideRoadPosition = sideRoad.transform.position;
        float direction = sideRoad.Direction == SideRoadDirection.Left ? -1f : 1f;
        Vector3 targetPositionAtStart = new Vector3(
            GetOffscreenTargetX(playerTransform, sideRoad, direction),
            startingPosition.y + sideRoadDriveRise,
            startingPosition.z);
        Quaternion targetRotation = startingRotation * Quaternion.Euler(
            0f,
            0f,
            -direction * sideRoadTurnAngle);

        float duration = Mathf.Max(0.05f, sideRoadDriveDuration);
        float elapsed = 0f;
        Vector3 sideRoadDisplacement = Vector3.zero;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);

            if (sideRoad != null)
            {
                sideRoadDisplacement = sideRoad.transform.position - startingSideRoadPosition;
            }
            else if (GameManager.Instance != null)
            {
                sideRoadDisplacement += Vector3.down *
                                        GameManager.Instance.CurrentGameSpeed *
                                        Time.deltaTime;
            }

            playerTransform.position = Vector3.Lerp(
                startingPosition + sideRoadDisplacement,
                targetPositionAtStart + sideRoadDisplacement,
                easedProgress);
            playerTransform.rotation = Quaternion.Slerp(
                startingRotation,
                targetRotation,
                easedProgress);

            yield return null;
        }

        // Set the exact final pose as the loop can finish one frame past the
        // duration. The target is beyond the camera edge, so the car can never
        // be left visibly stopped while the transition fades out.
        playerTransform.position = targetPositionAtStart + sideRoadDisplacement;
        playerTransform.rotation = targetRotation;
    }

    private float GetOffscreenTargetX(
        Transform playerTransform,
        SideRoad sideRoad,
        float direction)
    {
        float sideRoadTargetX = sideRoad.transform.position.x +
                                direction * sideRoadDriveDistance;
        Camera mainCamera = Camera.main;

        if (mainCamera == null)
            return sideRoadTargetX;

        float distanceFromCamera = Mathf.Abs(
            mainCamera.transform.position.z - playerTransform.position.z);
        float viewportX = direction < 0f ? 0f : 1f;
        float cameraEdgeX = mainCamera.ViewportToWorldPoint(
            new Vector3(viewportX, 0.5f, distanceFromCamera)).x;

        Renderer playerRenderer = playerTransform.GetComponentInChildren<Renderer>();
        float playerClearance = playerRenderer != null
            ? playerRenderer.bounds.extents.magnitude
            : 0f;
        float offscreenTargetX = cameraEdgeX + direction *
                                 (playerClearance + sideRoadDriveDistance);

        return direction < 0f
            ? Mathf.Min(sideRoadTargetX, offscreenTargetX)
            : Mathf.Max(sideRoadTargetX, offscreenTargetX);
    }

    private void EnsureFadeOverlay()
    {
        if (fadeImage != null)
            return;

        GameObject canvasObject = new GameObject(
            "EnvironmentTransitionFade",
            typeof(Canvas),
            typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        GameObject imageObject = new GameObject("Fade", typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(canvasObject.transform, false);

        RectTransform rectTransform = imageObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        fadeImage = imageObject.GetComponent<Image>();
        fadeImage.color = new Color(0f, 0f, 0f, 0f);
        fadeImage.raycastTarget = false;
    }

    private IEnumerator FadeTo(
        float targetAlpha,
        float duration,
        Transform movingObject = null,
        SideRoad movingSideRoad = null)
    {
        if (fadeImage == null)
            yield break;

        Color color = fadeImage.color;
        float startingAlpha = color.a;
        float safeDuration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;
        Vector3 previousSideRoadPosition = movingSideRoad != null
            ? movingSideRoad.transform.position
            : Vector3.zero;

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            if (movingObject != null && movingSideRoad != null)
            {
                Vector3 currentSideRoadPosition = movingSideRoad.transform.position;
                movingObject.position += currentSideRoadPosition - previousSideRoadPosition;
                previousSideRoadPosition = currentSideRoadPosition;
            }
            else if (movingObject != null && GameManager.Instance != null)
            {
                movingObject.position += Vector3.down *
                                         GameManager.Instance.CurrentGameSpeed *
                                         Time.deltaTime;
            }

            float progress = Mathf.Clamp01(elapsed / safeDuration);
            color.a = Mathf.Lerp(startingAlpha, targetAlpha, progress);
            fadeImage.color = color;
            yield return null;
        }

        color.a = targetAlpha;
        fadeImage.color = color;
    }

    public void SwitchEnvironment(EnvironmentDefinition environment)
    {
        if (environment == null)
            return;

        ResetDynamicWorldState();

        LaneSystem laneSystem = FindAnyObjectByType<LaneSystem>();
        laneSystem?.ApplyEnvironment(environment);

        GameManager.Instance?.ApplyEnvironment(environment);

        FuelSystem fuelSystem = FindAnyObjectByType<FuelSystem>();
        fuelSystem?.SetConsumptionMultiplier(environment.FuelConsumptionMultiplier);

        PlayerController playerController = FindAnyObjectByType<PlayerController>();
        playerController?.SetEnvironmentSpeedMultiplier(environment.HorizontalSpeedMultiplier);

        ApplySpawnerConfig(environment.SpawnerConfig);

        playerController?.ResetToEnvironmentStart();

        CameraLaneFitter cameraFitter = FindAnyObjectByType<CameraLaneFitter>();
        cameraFitter?.FitCameraToLanes();

        FindAnyObjectByType<SpawnReservationMap>()?.ClearAllReservations();
        FindAnyObjectByType<SpawnDirector>()?.ResetRuntimeState();

        ReplaceEnvironmentVisual(environment.EnvironmentPrefab);
        currentEnvironment = environment;
        EnvironmentChanged?.Invoke(environment);

        Debug.Log($"Environment changed to {environment.DisplayName}. Player progress was preserved.");
    }

    private static void ApplySpawnerConfig(EnvironmentSpawnerConfig config)
    {
        if (config == null)
        {
            Debug.LogWarning("Environment has no spawner config assigned.");
            return;
        }

        FindAnyObjectByType<TrafficSpawner>()?.ApplyConfig(config);
        FindAnyObjectByType<SideRoadSpawner>()?.ApplyConfig(config);
        FindAnyObjectByType<RoadSignSpawner>()?.ApplyConfig(config);

        SideRoadEventSource[] eventSources = FindObjectsByType<SideRoadEventSource>();
        foreach (SideRoadEventSource eventSource in eventSources)
            eventSource.ApplyConfig(config);

        FindAnyObjectByType<SpawnDirector>()?.ApplyConfig(config);
    }

    private static void ResetDynamicWorldState()
    {
        DestroyAllActive<TrafficVehicle>();
        DestroyAllActive<RoadSign>();
        DestroyAllActive<SideRoad>();
    }

    private static void DestroyAllActive<T>() where T : MonoBehaviour
    {
        T[] activeObjects = FindObjectsByType<T>();

        foreach (T activeObject in activeObjects)
        {
            if (activeObject != null)
                Destroy(activeObject.gameObject);
        }
    }

    private void ReplaceEnvironmentVisual(GameObject visualPrefab)
    {
        if (currentEnvironmentVisual != null)
            Destroy(currentEnvironmentVisual);

        currentEnvironmentVisual = visualPrefab != null
            ? Instantiate(visualPrefab, Vector3.zero, Quaternion.identity)
            : null;
    }
}
