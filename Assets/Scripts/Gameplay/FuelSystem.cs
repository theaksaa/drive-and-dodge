using UnityEngine;

public class FuelSystem : MonoBehaviour
{
    [Header("Fuel")]
    [SerializeField] private float maxFuel = 100f;
    [SerializeField] private float fuelDrainPerSecond = 3f;

    [Header("Out Of Fuel")]
    [SerializeField] private float slowdownPerSecond = 2f;

    public float CurrentFuel { get; private set; }
    public float MaxFuel => maxFuel;
    public float FuelNormalized => maxFuel <= 0f ? 0f : CurrentFuel / maxFuel;
    public bool IsOutOfFuel => CurrentFuel <= 0f;

    private bool hasStartedSlowdown;

    private void Awake()
    {
        CurrentFuel = maxFuel;
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;

        if (!IsOutOfFuel)
        {
            DrainFuel();
        }
        else
        {
            SlowDownGameSpeed();
        }
    }

    private void DrainFuel()
    {
        CurrentFuel -= fuelDrainPerSecond * Time.deltaTime;
        CurrentFuel = Mathf.Max(CurrentFuel, 0f);

        if (CurrentFuel <= 0f)
        {
            StartOutOfFuelSlowdown();
        }
    }

    private void StartOutOfFuelSlowdown()
    {
        if (hasStartedSlowdown)
            return;

        hasStartedSlowdown = true;

        if (GameManager.Instance != null)
            GameManager.Instance.SetSpeedIncreaseEnabled(false);

        Debug.Log("Out of fuel. Game speed is slowing down.");
    }

    private void SlowDownGameSpeed()
    {
        if (GameManager.Instance == null)
            return;

        GameManager.Instance.AddGameSpeed(-slowdownPerSecond * Time.deltaTime);

        if (GameManager.Instance.CurrentGameSpeed <= 0.01f)
        {
            GameManager.Instance.SetGameSpeed(0f);
            GameManager.Instance.GameOver();
        }
    }

    public void AddFuel(float amount)
    {
        if (amount <= 0f)
            return;

        CurrentFuel += amount;
        CurrentFuel = Mathf.Min(CurrentFuel, maxFuel);

        if (CurrentFuel > 0f)
        {
            hasStartedSlowdown = false;

            if (GameManager.Instance != null)
                GameManager.Instance.SetSpeedIncreaseEnabled(true);
        }

        Debug.Log($"Fuel: {CurrentFuel}/{maxFuel}");
    }

    public void FillFull()
    {
        CurrentFuel = maxFuel;
        hasStartedSlowdown = false;

        if (GameManager.Instance != null)
            GameManager.Instance.SetSpeedIncreaseEnabled(true);

        Debug.Log("Fuel tank full.");
    }
}