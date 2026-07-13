using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public event Action<bool> PauseStateChanged;

    [Header("Game State")]
    public bool IsGameOver { get; private set; }
    public bool IsPaused { get; private set; }
    public bool IsGameplayStopped => IsGameOver || IsPaused;

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
            // During a Single scene reload Unity can awaken the replacement scene
            // before destroying the previous scene. Allow the new scene's manager
            // to take ownership, while still rejecting duplicates in the same scene.
            if (Instance.gameObject.scene.Equals(gameObject.scene))
            {
                Destroy(gameObject);
                return;
            }
        }

        Instance = this;
        InitializeState();

        if (comboSystem == null)
            comboSystem = FindAnyObjectByType<ComboSystem>();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (IsGameplayStopped)
            return;

        if (canIncreaseGameSpeed)
            IncreaseGameSpeed();
    }

    private void InitializeState()
    {
        Time.timeScale = 1f;
        CurrentGameSpeed = startGameSpeed;
        IsGameOver = false;
        IsPaused = false;
        canIncreaseGameSpeed = true;
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

        if (IsPaused)
            SetPauseState(false);

        IsGameOver = true;

        if (comboSystem != null)
            comboSystem.ResetCombo();

        Debug.Log("GAME OVER");

        Time.timeScale = 0f;
    }

    public void PauseGame()
    {
        if (IsGameOver || IsPaused)
            return;

        SetPauseState(true);
    }

    public void ResumeGame()
    {
        if (!IsPaused)
            return;

        SetPauseState(false);
    }

    public void TogglePause()
    {
        if (IsPaused)
            ResumeGame();
        else
            PauseGame();
    }

    public void RestartGame()
    {
        PrepareForSceneReload();
        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.name, LoadSceneMode.Single);
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

    private void SetPauseState(bool paused)
    {
        IsPaused = paused;
        Time.timeScale = paused ? 0f : 1f;
        PauseStateChanged?.Invoke(IsPaused);
    }

    private void PrepareForSceneReload()
    {
        IsGameOver = false;
        SetPauseState(false);
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
