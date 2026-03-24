using UnityEngine;

public class PitDeath : MonoBehaviour
{
    [Header("Game Over")]
    [SerializeField] private MonoBehaviour playerMovementScript;
    [SerializeField] private GameObject gameOverUI;

    [Header("Trigger Settings")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool disableCharacterController = true;

    private bool triggered;

    private void Awake()
    {
        if (playerMovementScript == null && GameManager.HasInstance)
            playerMovementScript = GameManager.Instance.PlayerMovement;

        if (gameOverUI != null)
            gameOverUI.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered)
            return;

        if (other == null || !other.CompareTag(playerTag))
            return;

        triggered = true;
        HandleGameOver(other.gameObject);
    }

    private void HandleGameOver(GameObject playerObject)
    {
        if (playerMovementScript == null && GameManager.HasInstance)
            playerMovementScript = GameManager.Instance.PlayerMovement;

        if (playerMovementScript != null)
            playerMovementScript.enabled = false;

        if (disableCharacterController && playerObject != null)
        {
            CharacterController controller = playerObject.GetComponent<CharacterController>();
            if (controller != null)
                controller.enabled = false;
        }

        if (gameOverUI != null)
            gameOverUI.SetActive(true);
    }
}