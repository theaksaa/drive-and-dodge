using TMPro;
using UnityEngine;

public class FuelDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FuelSystem fuelSystem;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI fuelText;

    private void Awake()
    {
        if (fuelSystem == null)
            fuelSystem = FindAnyObjectByType<FuelSystem>();
    }

    private void Update()
    {
        if (fuelSystem == null)
            return;

        int fuelPercent = Mathf.RoundToInt(fuelSystem.FuelNormalized * 100f);
        fuelText.text = $"Fuel {fuelPercent}%";
    }
}
