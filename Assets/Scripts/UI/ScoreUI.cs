using TMPro;
using UnityEngine;

public class ScoreUI : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;

    private void Update()
    {
        if (ScoreManager.Instance == null)
            return;

        scoreText.text = ScoreManager.Instance.TotalScore.ToString();
    }
}