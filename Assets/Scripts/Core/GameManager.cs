using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    public bool IsGameOver { get; private set; }

    [Header("Global Speed")]
    [SerializeField] private float startGameSpeed = 4f;
    [SerializeField] private float maxGameSpeed = 12f;
    [SerializeField] private float speedIncreasePerSecond = 0.05f;

    public float CurrentGameSpeed { get; private set; }
    public float MaxGameSpeed => maxGameSpeed;
    public float SpeedIncreasePerSecond => speedIncreasePerSecond;

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
    }

    private void Update()
    {
        if (IsGameOver)
            return;

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
}