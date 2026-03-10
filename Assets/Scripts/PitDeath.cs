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
        // If you didn't assign playerMovementScript, try to auto-find it on Player
        if (playerMovementScript == null)
        {
            var player = GameObject.FindGameObjectWithTag(playerTag);
            if (player != null)
                playerMovementScript = player.GetComponent<PlayerMovement>();
        }

        if (gameOverUI != null)
            gameOverUI.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (other == null) return;
        if (!other.CompareTag(playerTag)) return;

        triggered = true;
        HandleGameOver(other.gameObject);
    }

    private void HandleGameOver(GameObject playerObj)
    {
        if (playerMovementScript != null)
            playerMovementScript.enabled = false;

        if (disableCharacterController && playerObj != null)
        {
            var cc = playerObj.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
        }

        // Show UI
        if (gameOverUI != null)
            gameOverUI.SetActive(true);
    }
}