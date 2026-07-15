using UnityEngine;

public class TrafficVehicle : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speedOffset = 0f;
    [SerializeField] private float minMoveSpeed = 3f;

    [Header("Animation Speed")]
    [SerializeField] private float animationReferenceSpeed = 6f;
    [SerializeField] [Range(0.25f, 1f)] private float minimumAnimationDurationMultiplier = 0.55f;

    [Header("Lane Change")]
    [SerializeField] [Range(0f, 1f)] private float laneChangeProbability = 0.2f;
    [SerializeField] private float minLaneChangeDelay = 0.45f;
    [SerializeField] private float maxLaneChangeDelay = 1.3f;
    [SerializeField] private float defaultLaneChangeDuration = 0.45f;
    [SerializeField] private float laneChangeSafetyPadding = 0.35f;
    [SerializeField] private int laneSafetySamples = 5;

    [Header("Blinkers")]
    [SerializeField] private float blinkerInterval = 0.09f;
    [SerializeField] private float preLaneChangeBlinkDuration = 2f;

    [Header("Hit Reaction")]
    [SerializeField] private float hitDriftDuration = 0.65f;
    [SerializeField] private float hitDriftAngle = 22f;
    [SerializeField] private float hitDriftEdgeOvershoot = 0.75f;

    public float SpeedOffset => speedOffset;
    public int LaneIndex { get; private set; } = -1;
    public bool HasPlannedLaneChange => hasPlannedLaneChange;
    public int PlannedTargetLaneIndex => plannedTargetLaneIndex;
    public float PlannedLaneChangeStartDelay => plannedLaneChangeStartDelay;
    public float PlannedLaneChangeDuration => plannedLaneChangeDuration;

    private Camera mainCamera;
    private LaneSystem laneSystem;
    private SpriteRenderer[] leftBlinkers;
    private SpriteRenderer[] rightBlinkers;

    private bool hasPlannedLaneChange;
    private int plannedTargetLaneIndex = -1;
    private float plannedLaneChangeStartDelay;
    private float plannedLaneChangeDuration;
    private float initializedAtTime;
    private float laneChangeStartedAtTime;
    private float laneChangeStartX;
    private float laneChangeTargetX;
    private int laneChangeSourceLaneIndex = -1;
    private bool laneChangeInProgress;
    private bool laneChangeCompleted;

    private bool isHitReacting;
    private float hitReactionStartedAtTime;
    private float hitReactionStartX;
    private float hitReactionTargetX;
    private float hitReactionStartAngle;
    private float hitReactionDirection;
    private float activeHitReactionDuration;

    private void Awake()
    {
        mainCamera = Camera.main;
        leftBlinkers = GetBlinkers("Blinkers/Left");
        rightBlinkers = GetBlinkers("Blinkers/Right");
        SetBlinkersActive(false, false);
    }

    public bool TryCreateLaneChangePlan(
        int laneIndex,
        int laneCount,
        out int targetLaneIndex,
        out float startDelay,
        out float duration)
    {
        targetLaneIndex = laneIndex;
        startDelay = 0f;
        duration = 0f;

        if (laneCount <= 1 || laneIndex < 0 || laneIndex >= laneCount)
            return false;

        if (laneChangeProbability <= 0f || Random.value > laneChangeProbability)
            return false;

        int direction = 0;

        if (laneIndex == 0)
        {
            direction = 1;
        }
        else if (laneIndex == laneCount - 1)
        {
            direction = -1;
        }
        else
        {
            direction = Random.value < 0.5f ? -1 : 1;
        }

        targetLaneIndex = laneIndex + direction;
        startDelay = GetSpeedAdjustedDuration(
            Random.Range(minLaneChangeDelay, Mathf.Max(minLaneChangeDelay, maxLaneChangeDelay)));
        duration = GetSpeedAdjustedDuration(defaultLaneChangeDuration);
        return true;
    }

    public void Initialize(
        int laneIndex,
        LaneSystem laneSystem,
        bool hasLaneChangePlan = false,
        int targetLaneIndex = -1,
        float laneChangeStartDelay = 0f,
        float laneChangeDuration = 0f)
    {
        LaneIndex = laneIndex;
        this.laneSystem = laneSystem;
        initializedAtTime = Time.time;

        this.hasPlannedLaneChange = hasLaneChangePlan && targetLaneIndex != laneIndex;
        plannedTargetLaneIndex = this.hasPlannedLaneChange ? targetLaneIndex : -1;
        plannedLaneChangeStartDelay = this.hasPlannedLaneChange ? Mathf.Max(0f, laneChangeStartDelay) : 0f;
        plannedLaneChangeDuration = this.hasPlannedLaneChange
            ? Mathf.Max(0.05f, laneChangeDuration)
            : 0f;

        laneChangeSourceLaneIndex = laneIndex;
        laneChangeInProgress = false;
        laneChangeCompleted = false;
        SetBlinkersActive(false, false);
    }

    public bool TryGetLaneChangePrediction(
        out int targetLaneIndex,
        out float startDelay,
        out float duration)
    {
        targetLaneIndex = -1;
        startDelay = 0f;
        duration = 0f;

        if (!hasPlannedLaneChange || plannedTargetLaneIndex < 0 || laneChangeCompleted)
            return false;

        targetLaneIndex = plannedTargetLaneIndex;

        if (laneChangeInProgress)
        {
            startDelay = 0f;
            duration = Mathf.Max(0f, plannedLaneChangeDuration - (Time.time - laneChangeStartedAtTime));
            return duration > 0f;
        }

        float elapsedSinceSpawn = Time.time - initializedAtTime;
        startDelay = Mathf.Max(0f, plannedLaneChangeStartDelay - elapsedSinceSpawn);
        duration = plannedLaneChangeDuration;
        return true;
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameplayStopped)
            return;

        if (isHitReacting)
        {
            UpdateHitReaction();
            MoveDown();
            return;
        }

        UpdateLaneChange();
        MoveDown();
        DestroyIfOutsideScreen();
    }

    public void BeginHitReaction(float playerX)
    {
        if (isHitReacting)
            return;

        hitReactionDirection = Mathf.Sign(transform.position.x - playerX);

        if (Mathf.Approximately(hitReactionDirection, 0f))
        {
            float roadCenterX = laneSystem != null
                ? (laneSystem.RoadLeftX + laneSystem.RoadRightX) * 0.5f
                : 0f;

            hitReactionDirection = transform.position.x >= roadCenterX ? 1f : -1f;
        }

        float halfWidth = GetHalfWidth();
        float edgeX;

        if (laneSystem != null)
        {
            edgeX = hitReactionDirection < 0f
                ? laneSystem.RoadLeftX - halfWidth - hitDriftEdgeOvershoot
                : laneSystem.RoadRightX + halfWidth + hitDriftEdgeOvershoot;
        }
        else
        {
            float fallbackDistance = Mathf.Max(2f, halfWidth * 3f + hitDriftEdgeOvershoot);
            edgeX = transform.position.x + hitReactionDirection * fallbackDistance;
        }

        isHitReacting = true;
        hitReactionStartedAtTime = Time.time;
        hitReactionStartX = transform.position.x;
        hitReactionTargetX = edgeX;
        hitReactionStartAngle = transform.eulerAngles.z;
        activeHitReactionDuration = GetSpeedAdjustedDuration(hitDriftDuration);

        hasPlannedLaneChange = false;
        laneChangeInProgress = false;
        plannedTargetLaneIndex = -1;
        SetBlinkersActive(false, false);
        DisableCollidersForHitReaction();
    }

    private void UpdateHitReaction()
    {
        float progress = Mathf.Clamp01(
            (Time.time - hitReactionStartedAtTime) / activeHitReactionDuration);
        float easedProgress = progress * progress * (3f - 2f * progress);

        Vector3 position = transform.position;
        position.x = Mathf.Lerp(hitReactionStartX, hitReactionTargetX, easedProgress);
        transform.position = position;

        float steeringAmount = Mathf.Sin(progress * Mathf.PI);
        float targetAngle = hitReactionStartAngle - hitReactionDirection * hitDriftAngle * steeringAmount;
        transform.rotation = Quaternion.Euler(0f, 0f, targetAngle);

        if (progress >= 1f)
            Destroy(gameObject);
    }

    private float GetSpeedAdjustedDuration(float baseDuration)
    {
        float referenceSpeed = Mathf.Max(0.1f, animationReferenceSpeed);
        float speedRatio = GetFinalMoveSpeed() / referenceSpeed;
        float durationMultiplier = Mathf.Clamp(
            1f / Mathf.Max(1f, speedRatio),
            minimumAnimationDurationMultiplier,
            1f);

        return Mathf.Max(0.05f, baseDuration * durationMultiplier);
    }

    private void DisableCollidersForHitReaction()
    {
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();

        foreach (Collider2D collider in colliders)
            collider.enabled = false;
    }

    private void UpdateLaneChange()
    {
        if (!hasPlannedLaneChange || plannedTargetLaneIndex < 0 || laneChangeCompleted)
        {
            SetBlinkersActive(false, false);
            return;
        }

        if (!laneChangeInProgress)
        {
            float elapsedSinceSpawn = Time.time - initializedAtTime;
            float blinkStartTime = Mathf.Max(0f, plannedLaneChangeStartDelay - preLaneChangeBlinkDuration);

            if (elapsedSinceSpawn < blinkStartTime)
            {
                SetBlinkersActive(false, false);
                return;
            }

            UpdateBlinkers();

            if (elapsedSinceSpawn < plannedLaneChangeStartDelay)
                return;

            if (CanStartLaneChange())
                BeginLaneChange();

            return;
        }

        UpdateBlinkers();

        float elapsedSinceStart = Time.time - laneChangeStartedAtTime;
        float progress = Mathf.Clamp01(elapsedSinceStart / plannedLaneChangeDuration);
        Vector3 position = transform.position;
        position.x = Mathf.Lerp(laneChangeStartX, laneChangeTargetX, progress);
        transform.position = position;

        if (progress >= 1f)
        {
            LaneIndex = plannedTargetLaneIndex;
            laneChangeInProgress = false;
            laneChangeCompleted = true;
            hasPlannedLaneChange = false;
            plannedTargetLaneIndex = -1;
            SetBlinkersActive(false, false);
        }
    }

    private void BeginLaneChange()
    {
        laneChangeInProgress = true;
        laneChangeStartedAtTime = Time.time;
        laneChangeSourceLaneIndex = LaneIndex;
        laneChangeStartX = transform.position.x;
        laneChangeTargetX = laneSystem != null
            ? laneSystem.GetLaneX(plannedTargetLaneIndex)
            : transform.position.x;
    }

    private bool CanStartLaneChange()
    {
        if (laneSystem == null)
            return false;

        if (plannedTargetLaneIndex < 0 || plannedTargetLaneIndex >= laneSystem.LaneCount)
            return false;

        TrafficVehicle[] activeTraffic = FindObjectsByType<TrafficVehicle>();
        float selfHalfLength = GetHalfLength();
        int samples = Mathf.Max(2, laneSafetySamples);

        for (int sampleIndex = 0; sampleIndex < samples; sampleIndex++)
        {
            float sampleT = plannedLaneChangeDuration * sampleIndex / (samples - 1);

            float selfFutureY = transform.position.y - GetFinalMoveSpeed() * sampleT;

            foreach (TrafficVehicle other in activeTraffic)
            {
                if (other == null || other == this)
                    continue;

                if (!other.OccupiesLaneAtTime(plannedTargetLaneIndex, sampleT))
                    continue;

                float otherFutureY = other.transform.position.y - other.GetFinalMoveSpeed() * sampleT;
                float requiredGap = selfHalfLength + other.GetHalfLength() + laneChangeSafetyPadding;

                if (Mathf.Abs(otherFutureY - selfFutureY) < requiredGap)
                    return false;
            }
        }

        return true;
    }

    private bool OccupiesLaneAtTime(int laneIndex, float futureTime)
    {
        if (isHitReacting)
            return false;

        if (laneChangeInProgress)
        {
            float remainingTime = Mathf.Max(0f, plannedLaneChangeDuration - (Time.time - laneChangeStartedAtTime));

            if (futureTime < remainingTime)
                return laneIndex == laneChangeSourceLaneIndex || laneIndex == plannedTargetLaneIndex;

            return laneIndex == plannedTargetLaneIndex;
        }

        if (hasPlannedLaneChange && plannedTargetLaneIndex >= 0)
        {
            float elapsedSinceSpawn = Time.time - initializedAtTime;
            float waitUntilStart = Mathf.Max(0f, plannedLaneChangeStartDelay - elapsedSinceSpawn);

            if (futureTime < waitUntilStart)
                return laneIndex == LaneIndex;

            return laneIndex == LaneIndex || laneIndex == plannedTargetLaneIndex;
        }

        return laneIndex == LaneIndex;
    }

    private void UpdateBlinkers()
    {
        bool blinkOn = Mathf.Repeat(Time.time, blinkerInterval * 2f) < blinkerInterval;
        bool wantsLeft = plannedTargetLaneIndex < LaneIndex;
        bool wantsRight = plannedTargetLaneIndex > LaneIndex;

        SetBlinkersActive(wantsLeft && blinkOn, wantsRight && blinkOn);
    }

    private void SetBlinkersActive(bool leftActive, bool rightActive)
    {
        SetRendererGroupEnabled(leftBlinkers, leftActive);
        SetRendererGroupEnabled(rightBlinkers, rightActive);
    }

    private static void SetRendererGroupEnabled(SpriteRenderer[] renderers, bool isEnabled)
    {
        if (renderers == null)
            return;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].enabled = isEnabled;
        }
    }

    private SpriteRenderer[] GetBlinkers(string relativePath)
    {
        Transform blinkerRoot = transform.Find(relativePath);
        return blinkerRoot != null
            ? blinkerRoot.GetComponentsInChildren<SpriteRenderer>(true)
            : System.Array.Empty<SpriteRenderer>();
    }

    private void MoveDown()
    {
        float finalSpeed = GetFinalMoveSpeed();

        transform.position += Vector3.down * finalSpeed * Time.deltaTime;
    }

    public float GetFinalMoveSpeed()
    {
        float globalSpeed = GameManager.Instance != null
            ? GameManager.Instance.CurrentGameSpeed
            : minMoveSpeed;

        float calculatedSpeed = globalSpeed + speedOffset;

        return Mathf.Max(minMoveSpeed, calculatedSpeed);
    }

    public float GetHalfLength()
    {
        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();

        if (boxCollider != null)
        {
            return boxCollider.size.y * Mathf.Abs(transform.localScale.y) / 2f;
        }

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            return spriteRenderer.sprite.bounds.size.y * Mathf.Abs(transform.localScale.y) / 2f;
        }

        return 0.5f;
    }

    private float GetHalfWidth()
    {
        Collider2D vehicleCollider = GetComponent<Collider2D>();

        if (vehicleCollider != null)
            return vehicleCollider.bounds.extents.x;

        SpriteRenderer spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
            return spriteRenderer.bounds.extents.x;

        return 0.5f;
    }

    public bool TryGetCollisionPushDirection(float playerX, out float direction)
    {
        direction = 0f;

        if (laneChangeInProgress)
        {
            float laneChangeDirection = Mathf.Sign(laneChangeTargetX - laneChangeStartX);

            if (!Mathf.Approximately(laneChangeDirection, 0f))
            {
                direction = laneChangeDirection;
                return true;
            }
        }

        float relativeDirection = Mathf.Sign(playerX - transform.position.x);

        if (Mathf.Approximately(relativeDirection, 0f))
            return false;

        direction = relativeDirection;
        return true;
    }

    private void DestroyIfOutsideScreen()
    {
        float bottomY;

        if (laneSystem != null)
        {
            bottomY = laneSystem.GetBottomY();
        }
        else
        {
            if (mainCamera == null)
                return;

            float distanceFromCamera = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);

            Vector3 bottomLeft = mainCamera.ViewportToWorldPoint(
                new Vector3(0f, 0f, distanceFromCamera)
            );

            bottomY = bottomLeft.y;
        }

        if (transform.position.y < bottomY - GetHalfLength() - 1f)
        {
            Destroy(gameObject);
        }
    }
}
