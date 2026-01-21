using UnityEngine;
using TMPro;

public class WinScript : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject coinsWinText;
    [SerializeField] private GameObject keysCount;
    [SerializeField] private GameObject scoreCount;
    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private MonoBehaviour playerMovementScript;

    private bool won;

    private void Awake()
    {
        if (winPanel != null) winPanel.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (won) return;
        if (!other.CompareTag(playerTag)) return;

        won = true;

        if (playerMovementScript == null)
            playerMovementScript = other.GetComponentInParent<MonoBehaviour>();

        if (playerMovementScript != null)
            playerMovementScript.enabled = false;

        if (winPanel != null)
            winPanel.SetActive(true);

        if (coinsText != null && ScoreManager.Instance != null)
            coinsWinText.SetActive(true);
            coinsText.text = "You got - " + ScoreManager.Instance.Score.ToString() + " coins";
    }
}
