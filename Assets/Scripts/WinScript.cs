using UnityEngine;
using TMPro;

public class WinScript : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject coinsWinText;
    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private MonoBehaviour playerMovementScript;
    [SerializeField] private ScoreManager scoreManager;

    private bool won;

    private void Awake()
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (coinsWinText != null) coinsWinText.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (won) return;
        if (!other.CompareTag(playerTag)) return;

        won = true;

        if (playerMovementScript != null)
            playerMovementScript.enabled = false;

        if (winPanel != null)
            winPanel.SetActive(true);

        if (coinsText != null && scoreManager != null)
        {
            if (coinsWinText != null) coinsWinText.SetActive(true);
            coinsText.text = "You got - " + scoreManager.Score + " coins";
        }
    }
}
