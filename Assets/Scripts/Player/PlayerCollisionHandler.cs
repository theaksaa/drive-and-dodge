using UnityEngine;

public class PlayerCollisionHandler : MonoBehaviour
{
    private PlayerHealth playerHealth;
    private PlayerCollisionPush collisionPush;

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        collisionPush = GetComponent<PlayerCollisionPush>();

        if (playerHealth == null)
            Debug.LogError("PlayerCollisionHandler: PlayerHealth not found on Player.");

        if (collisionPush == null)
            Debug.LogWarning("PlayerCollisionHandler: PlayerCollisionPush not found. Collision push will be skipped.");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Player trigger with: " + other.gameObject.name + " | Tag: " + other.tag);

        if (other.CompareTag("NearMissZone"))
        {
            Debug.Log("Near miss zone ignored by damage system.");
            return;
        }

        TrafficVehicle trafficVehicle = other.GetComponentInParent<TrafficVehicle>();

        if (trafficVehicle != null)
        {
            trafficVehicle.BeginHitReaction(transform.position.x);

            if (collisionPush != null &&
                trafficVehicle.TryGetCollisionPushDirection(transform.position.x, out float pushDirection))
            {
                collisionPush.ApplyPush(pushDirection);
            }
        }

        DamageDealer damageDealer = other.GetComponentInParent<DamageDealer>();

        if (damageDealer == null)
            return;

        damageDealer.TryDealDamageTo(playerHealth);
    }
}
