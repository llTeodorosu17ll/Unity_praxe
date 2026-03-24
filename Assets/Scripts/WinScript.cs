using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class WinScript : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private MonoBehaviour playerMovementScript;

    [Header("UI")]
    [SerializeField] private GameObject winPanel;
    [SerializeField] private TMP_Text winText;
    [SerializeField] private Button nextLevelButton;

    private bool won;

    private void Awake()
    {
        if (playerMovementScript == null && GameManager.HasInstance)
            playerMovementScript = GameManager.Instance.PlayerMovement;

        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.RemoveAllListeners();
            nextLevelButton.onClick.AddListener(Continue_Button);
        }

        if (winPanel != null)
            winPanel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (won)
            return;

        if (other == null || !other.CompareTag(playerTag))
            return;

        won = true;

        if (playerMovementScript == null && GameManager.HasInstance)
            playerMovementScript = GameManager.Instance.PlayerMovement;

        if (playerMovementScript != null)
            playerMovementScript.enabled = false;

        if (winPanel != null)
            winPanel.SetActive(true);

        if (winText != null && GameManager.HasInstance)
            winText.text = "You got - " + GameManager.Instance.Score + " coins";

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (GameManager.HasInstance)
            GameManager.Instance.CaptureSceneTransferState();
    }

    // Called by Button OnClick
    public void Continue_Button()
    {
        if (!won)
            return;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (!GameManager.HasInstance)
        {
            Debug.LogError("GameManager is missing.", this);
            return;
        }

        if (!GameManager.Instance.ContinueToNextLevel())
            Debug.LogError("No next level in Build Settings.", this);
    }
}