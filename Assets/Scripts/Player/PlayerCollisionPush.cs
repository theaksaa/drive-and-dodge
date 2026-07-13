using UnityEngine;

public class PlayerCollisionPush : MonoBehaviour
{
    [Header("Collision Push")]
    [SerializeField] private float collisionPushDistance = 0.7f;
    [SerializeField] private float collisionBounceDuration = 0.09f;

    private PlayerController playerController;
    private float collisionBounceDirection;
    private float activeCollisionPushDistance;
    private float collisionBounceTimer;

    public bool IsBounceActive =>
        collisionBounceTimer > 0f &&
        !Mathf.Approximately(collisionBounceDirection, 0f);

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();

        if (playerController == null)
            Debug.LogWarning("PlayerCollisionPush: PlayerController not found on Player.");
    }

    public void ApplyPush(float direction)
    {
        if (Mathf.Approximately(direction, 0f))
            return;

        collisionBounceDirection = Mathf.Sign(direction);
        activeCollisionPushDistance = collisionPushDistance;
        collisionBounceTimer = Mathf.Max(0.01f, collisionBounceDuration);

        if (playerController != null)
            playerController.CancelDragForExternalMovement();
    }

    public void ApplyBounceStep(Transform targetTransform)
    {
        if (!IsBounceActive || targetTransform == null)
            return;

        float duration = Mathf.Max(0.01f, collisionBounceDuration);
        float normalizedTime = 1f - (collisionBounceTimer / duration);
        float bounceStrength = 1f - normalizedTime;
        float frameDistance = collisionBounceDirection * activeCollisionPushDistance * bounceStrength * (Time.deltaTime / duration);

        Vector3 position = targetTransform.position;
        position.x += frameDistance;
        targetTransform.position = position;

        collisionBounceTimer -= Time.deltaTime;

        if (collisionBounceTimer <= 0f)
        {
            collisionBounceTimer = 0f;
            collisionBounceDirection = 0f;
        }

        if (playerController != null)
            playerController.SyncDragTargetToCurrentPosition();
    }
}
