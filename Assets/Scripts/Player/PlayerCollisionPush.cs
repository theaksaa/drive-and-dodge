using UnityEngine;

public class PlayerCollisionPush : MonoBehaviour
{
    [Header("Collision Push")]
    [SerializeField] private float collisionPushDistance = 0.7f;
    [SerializeField] private float collisionBounceDuration = 0.24f;
    [SerializeField] private float collisionDriftAngle = 14f;

    private PlayerController playerController;
    private float collisionBounceDirection;
    private float activeCollisionPushDistance;
    private float activeCollisionBounceDuration;
    private float collisionBounceElapsed;
    private float collisionBounceStartX;
    private Quaternion baseLocalRotation;

    public bool IsBounceActive =>
        collisionBounceElapsed < activeCollisionBounceDuration &&
        !Mathf.Approximately(collisionBounceDirection, 0f);

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        baseLocalRotation = transform.localRotation;

        if (playerController == null)
            Debug.LogWarning("PlayerCollisionPush: PlayerController not found on Player.");
    }

    public void ApplyPush(float direction)
    {
        if (Mathf.Approximately(direction, 0f))
            return;

        collisionBounceDirection = Mathf.Sign(direction);
        activeCollisionPushDistance = collisionPushDistance;
        activeCollisionBounceDuration = Mathf.Max(0.05f, collisionBounceDuration);
        collisionBounceElapsed = 0f;
        collisionBounceStartX = transform.position.x;

        if (playerController != null)
            playerController.CancelDragForExternalMovement();
    }

    public void ApplyBounceStep(Transform targetTransform)
    {
        if (!IsBounceActive || targetTransform == null)
            return;

        collisionBounceElapsed = Mathf.Min(
            collisionBounceElapsed + Time.deltaTime,
            activeCollisionBounceDuration);

        float progress = collisionBounceElapsed / activeCollisionBounceDuration;
        float easedProgress = SmootherStep(progress);

        Vector3 position = targetTransform.position;
        position.x = collisionBounceStartX +
                     collisionBounceDirection * activeCollisionPushDistance * easedProgress;
        targetTransform.position = position;

        float driftAmount = Mathf.Sin(progress * Mathf.PI);
        float driftAngle = -collisionBounceDirection * collisionDriftAngle * driftAmount;
        targetTransform.localRotation = baseLocalRotation * Quaternion.Euler(0f, 0f, driftAngle);

        if (collisionBounceElapsed >= activeCollisionBounceDuration)
        {
            targetTransform.localRotation = baseLocalRotation;
            collisionBounceDirection = 0f;
        }

        if (playerController != null)
            playerController.SyncDragTargetToCurrentPosition();
    }

    private static float SmootherStep(float value)
    {
        value = Mathf.Clamp01(value);
        return value * value * value * (value * (value * 6f - 15f) + 10f);
    }
}
