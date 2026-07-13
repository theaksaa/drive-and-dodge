using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PauseMenuUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject pauseMenuRoot;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;

    [Header("Behavior")]
    [SerializeField] private bool hidePauseMenuOnStart = true;
    [SerializeField] private bool allowKeyboardToggle = true;

    private GameManager gameManager;
    private bool isSubscribedToPauseEvents;

    private void Awake()
    {
        EnsureGameManagerReference();

        if (pauseMenuRoot == null)
            pauseMenuRoot = gameObject;

        if (pauseButton != null)
            pauseButton.onClick.AddListener(TogglePause);

        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);

        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);
    }

    private void OnEnable()
    {
        EnsureGameManagerReference();

        if (hidePauseMenuOnStart)
            SetPauseMenuVisible(false);
        else
            SyncWithGameManager();
    }

    private void OnDisable()
    {
        UnsubscribeFromPauseEvents();
    }

    private void Start()
    {
        EnsureGameManagerReference();
        SyncWithGameManager();
    }

    private void Update()
    {
        EnsureGameManagerReference();

        if (!allowKeyboardToggle || gameManager == null)
            return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            TogglePause();
    }

    public void TogglePause()
    {
        EnsureGameManagerReference();

        if (gameManager == null)
            return;

        if (gameManager.IsGameOver)
        {
            SetPauseMenuVisible(!IsPauseMenuVisible());
            RefreshButtonState();
            return;
        }

        gameManager.TogglePause();
        RefreshButtonState();
    }

    public void ResumeGame()
    {
        EnsureGameManagerReference();

        if (gameManager == null)
            return;

        gameManager.ResumeGame();
    }

    public void RestartGame()
    {
        EnsureGameManagerReference();

        if (gameManager == null)
            return;

        gameManager.RestartGame();
    }

    private void HandlePauseStateChanged(bool isPaused)
    {
        SetPauseMenuVisible(isPaused);
        RefreshButtonState();
    }

    private void SyncWithGameManager()
    {
        bool isPaused = gameManager != null && gameManager.IsPaused;
        SetPauseMenuVisible(isPaused);
        RefreshButtonState();
    }

    private void EnsureGameManagerReference()
    {
        if (gameManager == null)
            gameManager = GameManager.Instance ?? FindAnyObjectByType<GameManager>();

        if (gameManager != null && !isSubscribedToPauseEvents)
        {
            gameManager.PauseStateChanged += HandlePauseStateChanged;
            isSubscribedToPauseEvents = true;
        }
    }

    private void UnsubscribeFromPauseEvents()
    {
        if (gameManager == null || !isSubscribedToPauseEvents)
            return;

        gameManager.PauseStateChanged -= HandlePauseStateChanged;
        isSubscribedToPauseEvents = false;
    }

    private void SetPauseMenuVisible(bool isVisible)
    {
        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(isVisible);
    }

    private bool IsPauseMenuVisible()
    {
        return pauseMenuRoot != null && pauseMenuRoot.activeSelf;
    }

    private void RefreshButtonState()
    {
        bool isGameOver = gameManager != null && gameManager.IsGameOver;

        if (resumeButton != null)
            resumeButton.gameObject.SetActive(!isGameOver);

        if (pauseButton != null)
            pauseButton.interactable = true;
    }
}
