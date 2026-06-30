using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Distance Score")]
    [SerializeField] private float scoreMultiplier = 1f;

    private float distanceScore;
    private int bonusScore;

    public int TotalScore => Mathf.FloorToInt(distanceScore) + bonusScore;
    public float DistanceScore => distanceScore;
    public int BonusScore => bonusScore;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

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
        float gameSpeed = GameManager.Instance != null
            ? GameManager.Instance.CurrentGameSpeed
            : 0f;

        distanceScore += gameSpeed * scoreMultiplier * Time.deltaTime;
    }

    public void AddBonusScore(int amount)
    {
        bonusScore += amount;
    }

    public void ResetScore()
    {
        distanceScore = 0f;
        bonusScore = 0;
    }
}