using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    public bool IsGameOver { get; private set; }

    [Header("Global Speed")]
    [SerializeField] private float startGameSpeed = 6f;
    [SerializeField] private float maxGameSpeed = 10f;
    [SerializeField] private float speedIncreasePerSecond = 0.02f;

    [Header("Systems")]
    [SerializeField] private ComboSystem comboSystem;

    public float CurrentGameSpeed { get; private set; }
    public float MaxGameSpeed => maxGameSpeed;
    public float SpeedIncreasePerSecond => speedIncreasePerSecond;

    private bool canIncreaseGameSpeed = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Time.timeScale = 1f;

        CurrentGameSpeed = startGameSpeed;
        IsGameOver = false;
        canIncreaseGameSpeed = true;

        if (comboSystem == null)
            comboSystem = FindFirstObjectByType<ComboSystem>();
    }

    private void Update()
    {
        if (IsGameOver)
            return;

        if (canIncreaseGameSpeed)
            IncreaseGameSpeed();
    }

    private void IncreaseGameSpeed()
    {
        CurrentGameSpeed += speedIncreasePerSecond * Time.deltaTime;
        CurrentGameSpeed = Mathf.Min(CurrentGameSpeed, maxGameSpeed);
    }

    public void GameOver()
    {
        if (IsGameOver)
            return;

        IsGameOver = true;

        if (comboSystem != null)
            comboSystem.ResetCombo();

        Debug.Log("GAME OVER");

        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void SetGameSpeed(float newSpeed)
    {
        CurrentGameSpeed = Mathf.Clamp(newSpeed, 0f, maxGameSpeed);
    }

    public void AddGameSpeed(float amount)
    {
        SetGameSpeed(CurrentGameSpeed + amount);
    }

    public void ResetGameSpeed()
    {
        CurrentGameSpeed = startGameSpeed;
    }

    public void SetSpeedIncreaseEnabled(bool enabled)
    {
        canIncreaseGameSpeed = enabled;
    }

    public void ApplyEnvironment(EnvironmentDefinition environment)
    {
        if (environment == null)
            return;

        maxGameSpeed = Mathf.Max(0f, environment.MaxGameSpeed);
        speedIncreasePerSecond = Mathf.Max(0f, environment.SpeedIncreasePerSecond);

        if (environment.SetSpeedOnEnter)
            SetGameSpeed(environment.SpeedOnEnter);
        else
            CurrentGameSpeed = Mathf.Min(CurrentGameSpeed, maxGameSpeed);
    }
}
