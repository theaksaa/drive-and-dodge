using UnityEngine;

public class ComboSystem : MonoBehaviour
{
    [Header("Combo Level")]
    [SerializeField] private int minComboLevel = 1;
    [SerializeField] private int maxComboLevel = 5;

    [Header("Meter")]
    [SerializeField] private float maxMeter = 100f;
    [SerializeField] private float meterDrainPerSecond = 18f;
    [SerializeField] private float meterAfterLevelDown = 50f;

    [Header("Near Miss Gain")]
    [SerializeField] private float normalNearMissGain = 20f;
    [SerializeField] private float riskyNearMissGain = 35f;
    [SerializeField] private float insaneNearMissGain = 50f;

    [Header("Anti Spam")]
    [SerializeField] private float nearMissCooldown = 0.45f;

    private int comboLevel = 1;
    private float meter;
    private float nearMissCooldownTimer;

    public int ComboLevel => comboLevel;
    public float MeterNormalized => meter / maxMeter;
    public float Meter => meter;

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;

        UpdateCooldown();
        DrainMeter();
    }

    private void UpdateCooldown()
    {
        if (nearMissCooldownTimer > 0f)
            nearMissCooldownTimer -= Time.deltaTime;
    }

    private void DrainMeter()
    {
        if (meter <= 0f)
            return;

        meter -= meterDrainPerSecond * Time.deltaTime;

        if (meter <= 0f)
        {
            meter = 0f;
            LevelDown();
        }
    }

    public void RegisterNearMiss(NearMissQuality quality)
    {
        if (nearMissCooldownTimer > 0f)
            return;

        nearMissCooldownTimer = nearMissCooldown;

        float gain = GetGainForQuality(quality);
        AddMeter(gain);
    }

    private float GetGainForQuality(NearMissQuality quality)
    {
        switch (quality)
        {
            case NearMissQuality.Normal:
                return normalNearMissGain;

            case NearMissQuality.Risky:
                return riskyNearMissGain;

            case NearMissQuality.Insane:
                return insaneNearMissGain;

            default:
                return normalNearMissGain;
        }
    }

    private void AddMeter(float amount)
    {
        meter += amount;

        while (meter >= maxMeter)
        {
            meter -= maxMeter;
            LevelUp();

            if (comboLevel >= maxComboLevel)
            {
                comboLevel = maxComboLevel;
                meter = maxMeter;
                break;
            }
        }
    }

    private void LevelUp()
    {
        comboLevel = Mathf.Min(comboLevel + 1, maxComboLevel);
        Debug.Log($"Combo Up: x{comboLevel}");
    }

    private void LevelDown()
    {
        if (comboLevel <= minComboLevel)
        {
            comboLevel = minComboLevel;
            meter = 0f;
            return;
        }

        comboLevel--;
        meter = meterAfterLevelDown;

        Debug.Log($"Combo Down: x{comboLevel}");
    }

    public void ResetCombo()
    {
        comboLevel = minComboLevel;
        meter = 0f;
        nearMissCooldownTimer = 0f;

        Debug.Log("Combo Reset");
    }
}