using TMPro;
using UnityEngine;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI healthText;

    private void Awake()
    {
        if (playerHealth == null)
            playerHealth = FindAnyObjectByType<PlayerHealth>();
    }

    private void Update()
    {
        if (playerHealth == null)
            return;

        healthText.text = $"HP {playerHealth.CurrentHealth}/{playerHealth.MaxHealth}";
    }
}
