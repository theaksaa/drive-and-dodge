using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 100;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;

        if (damage <= 0)
            return;

        CurrentHealth -= damage;
        CurrentHealth = Mathf.Max(CurrentHealth, 0);

        Debug.Log($"Player HP: {CurrentHealth}/{maxHealth}");

        if (CurrentHealth <= 0)
        {
            if (GameManager.Instance != null)
                GameManager.Instance.GameOver();
        }
    }

    public void RepairFull()
    {
        CurrentHealth = maxHealth;
        Debug.Log($"Player repaired: {CurrentHealth}/{maxHealth}");
    }

    public void Repair(int amount)
    {
        if (amount <= 0)
            return;

        CurrentHealth += amount;
        CurrentHealth = Mathf.Min(CurrentHealth, maxHealth);

        Debug.Log($"Player repaired: {CurrentHealth}/{maxHealth}");
    }
}