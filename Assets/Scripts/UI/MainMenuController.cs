using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class MainMenuController : MonoBehaviour
{
    [Header("Navigation")]
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("Scene UI")]
    [SerializeField] private Image logoImage;
    [SerializeField] private GameObject logoPlaceholder;

    [Header("Traffic Background")]
    [SerializeField, Min(0f)] private float demoSpeed = 6f;

    private void Awake()
    {
        Time.timeScale = 1f;
    }

    private void Start()
    {
        if (logoPlaceholder != null && logoImage != null)
        {
            bool hasLogo = logoImage.sprite != null;
            logoPlaceholder.SetActive(!hasLogo);
            logoImage.color = hasLogo ? Color.white : new Color(1f, 1f, 1f, 0f);
        }

        KeepDemoSpeedFixed();
    }

    private void Update()
    {
        KeepDemoSpeedFixed();
    }

    private void KeepDemoSpeedFixed()
    {
        if (GameManager.Instance == null)
            return;

        GameManager.Instance.SetSpeedIncreaseEnabled(false);
        GameManager.Instance.SetGameSpeed(demoSpeed);
    }

    public void StartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
