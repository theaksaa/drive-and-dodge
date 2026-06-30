using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Distance Score")]
    [SerializeField] private float playerSpeed = 10f;
    [SerializeField] private float scoreMultiplier = 1f;

    private float distanceScore;
    private int bonusScore;

    public int TotalScore => Mathf.FloorToInt(distanceScore) + bonusScore;
    public float PlayerSpeed => playerSpeed;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;

        AddDistanceScore();
    }

    private void AddDistanceScore()
    {
        distanceScore += playerSpeed * scoreMultiplier * Time.deltaTime;
    }

    public void AddBonusScore(int amount)
    {
        bonusScore += amount;
    }

    public void SetPlayerSpeed(float newSpeed)
    {
        playerSpeed = newSpeed;
    }

    public void AddPlayerSpeed(float amount)
    {
        playerSpeed += amount;
    }
}