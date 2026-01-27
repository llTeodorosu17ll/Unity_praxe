// ScoreManager.cs
using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private string prefix = "Score = ";

    private int score;
    public int Score => score;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        RefreshUI();
    }

    public void Add(int amount)
    {
        score += amount;
        RefreshUI();
    }

    public void SetScore(int value)
    {
        score = Mathf.Max(0, value);
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (scoreText != null)
            scoreText.text = prefix + score;
    }
}
