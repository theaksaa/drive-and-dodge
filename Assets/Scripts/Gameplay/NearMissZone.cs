using UnityEngine;

public class NearMissZone : MonoBehaviour
{
    [Header("Near Miss")]
    [SerializeField] private NearMissQuality quality = NearMissQuality.Normal;
    [SerializeField] private int baseNearMissScore = 100;

    private ComboSystem comboSystem;
    private ScoreManager scoreManager;

    private bool hasTriggered;

    private void Awake()
    {
        comboSystem = FindFirstObjectByType<ComboSystem>();
        scoreManager = FindFirstObjectByType<ScoreManager>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered)
            return;

        if (!other.CompareTag("Player"))
            return;

        hasTriggered = true;

        if (comboSystem != null)
        {
            comboSystem.RegisterNearMiss(quality);

            int scoreToAdd = baseNearMissScore * comboSystem.ComboLevel;

            if (scoreManager != null)
            {
                scoreManager.AddBonusScore(scoreToAdd);
            }

            Debug.Log($"Near Miss! +{scoreToAdd} score | Combo x{comboSystem.ComboLevel}");
        }
        else
        {
            if (scoreManager != null)
            {
                scoreManager.AddBonusScore(baseNearMissScore);
            }

            Debug.Log($"Near Miss! +{baseNearMissScore} score | No ComboSystem found");
        }
    }
}