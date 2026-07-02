using UnityEngine;

public class PlayerCollisionHandler : MonoBehaviour
{
    private PlayerHealth playerHealth;

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();

        if (playerHealth == null)
            Debug.LogError("PlayerCollisionHandler: PlayerHealth not found on Player.");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Player trigger with: " + other.gameObject.name + " | Tag: " + other.tag);

        if (other.CompareTag("NearMissZone"))
        {
            Debug.Log("Near miss zone ignored by damage system.");
            return;
        }

        DamageDealer damageDealer = other.GetComponentInParent<DamageDealer>();

        if (damageDealer == null)
            return;

        damageDealer.TryDealDamageTo(playerHealth);
    }
}