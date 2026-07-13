using TMPro;
using UnityEngine;

public class ComboDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ComboSystem comboSystem;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI comboText;
    [SerializeField] private TextMeshProUGUI meterText;

    private void Awake()
    {
        if (comboSystem == null)
            comboSystem = FindAnyObjectByType<ComboSystem>();
    }

    private void Update()
    {
        if (comboSystem == null)
            return;

        comboText.text = $"x{comboSystem.ComboLevel}";

        int meterPercent = Mathf.RoundToInt(comboSystem.MeterNormalized * 100f);
        meterText.text = $"{meterPercent}%";
    }
}
