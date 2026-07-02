using UnityEngine;

public class DamageDealer : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private int damage = 25;
    [SerializeField] private bool instantKill;

    [Header("After Hit")]
    [SerializeField] private bool disableAfterHit = true;

    private bool hasDealtDamage;

    public int Damage => damage;
    public bool InstantKill => instantKill;
    public bool HasDealtDamage => hasDealtDamage;

    public bool TryDealDamageTo(PlayerHealth playerHealth)
    {
        if (hasDealtDamage)
            return false;

        if (playerHealth == null)
            return false;

        hasDealtDamage = true;

        if (instantKill)
        {
            if (GameManager.Instance != null)
                GameManager.Instance.GameOver();
        }
        else
        {
            playerHealth.TakeDamage(damage);
        }

        if (disableAfterHit)
            DisableAllColliders();

        return true;
    }

    private void DisableDamageColliders()
    {
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();

        foreach (Collider2D col in colliders)
        {
            if (col.CompareTag("NearMissZone"))
                continue;

            col.enabled = false;
        }
    }

    private void DisableAllColliders()
    {
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();

        foreach (Collider2D col in colliders)
        {
            col.enabled = false;
        }
    }
}