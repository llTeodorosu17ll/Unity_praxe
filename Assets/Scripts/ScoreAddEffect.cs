using UnityEngine;

public class ScoreAddEffect : MonoBehaviour
{
    [SerializeField] private int amount = 1;
    [SerializeField] private ScoreManager scoreManager;

    public void Apply()
    {
        if (scoreManager != null)
            scoreManager.AddScore(amount);
    }
}
