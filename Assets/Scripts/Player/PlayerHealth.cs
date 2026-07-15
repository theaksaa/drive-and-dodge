using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 100;

    [Header("Low Health Visual")]
    [SerializeField] private int lowHealthThreshold = 100;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite lowHealthSprite;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;

    private Sprite normalSprite;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
            normalSprite = spriteRenderer.sprite;

        CurrentHealth = maxHealth;
        UpdateHealthSprite();
    }

    public void TakeDamage(int damage)
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;

        if (damage <= 0)
            return;

        CurrentHealth -= damage;
        CurrentHealth = Mathf.Max(CurrentHealth, 0);

        UpdateHealthSprite();

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
        UpdateHealthSprite();
        Debug.Log($"Player repaired: {CurrentHealth}/{maxHealth}");
    }

    public void Repair(int amount)
    {
        if (amount <= 0)
            return;

        CurrentHealth += amount;
        CurrentHealth = Mathf.Min(CurrentHealth, maxHealth);

        UpdateHealthSprite();

        Debug.Log($"Player repaired: {CurrentHealth}/{maxHealth}");
    }

    private void UpdateHealthSprite()
    {
        if (spriteRenderer == null)
            return;

        bool isBelowThreshold = CurrentHealth < lowHealthThreshold;

        if (isBelowThreshold && lowHealthSprite != null)
            spriteRenderer.sprite = lowHealthSprite;
        else
            spriteRenderer.sprite = normalSprite;
    }
}
