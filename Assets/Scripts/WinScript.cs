using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class WinScript : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private string playerTag = "Player";

    [Header("UI object names (must match Hierarchy names)")]
    [SerializeField] private string winPanelName = "WinPanel";      // your panel that shows victory
    [SerializeField] private string winTextName = "WinText";        // TMP inside win panel (yellow "You got - ...")
    [SerializeField] private string hudScoreTextName = "score";     // HUD score TMP object name (top-right). In your Canvas it might be "score"
    [SerializeField] private string nextLevelButtonName = "Next Level Button"; // optional: if your button has a name, put it. If empty -> first Button in panel

    [Header("Disable movement on win")]
    [SerializeField] private MonoBehaviour playerMovementScript;

    private GameObject winPanel;
    private TMP_Text winText;
    private TMP_Text hudScoreText;
    private Button nextLevelButton;

    private bool won;

    private void Awake()
    {
        CacheRefs();

        if (winPanel != null)
            winPanel.SetActive(false);

        // normal gameplay cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void CacheRefs()
    {
        // Player movement
        if (playerMovementScript == null)
        {
            var player = GameObject.FindGameObjectWithTag(playerTag);
            if (player != null)
            {
                var pm = player.GetComponent<PlayerMovement>();
                if (pm != null) playerMovementScript = pm;
            }
        }

        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        // Win panel
        if (winPanel == null)
        {
            var t = FindInChildren(canvas.transform, winPanelName);
            if (t != null) winPanel = t.gameObject;
        }

        // Win text inside panel
        if (winText == null && winPanel != null)
        {
            var t = FindInChildren(winPanel.transform, winTextName);
            if (t != null) winText = t.GetComponent<TMP_Text>();
        }

        // HUD score text (top-right)
        if (hudScoreText == null)
        {
            var t = FindInChildren(canvas.transform, hudScoreTextName);
            if (t != null) hudScoreText = t.GetComponent<TMP_Text>();
        }

        // Next level button inside panel
        if (nextLevelButton == null && winPanel != null)
        {
            if (!string.IsNullOrEmpty(nextLevelButtonName))
            {
                var bt = FindInChildren(winPanel.transform, nextLevelButtonName);
                if (bt != null) nextLevelButton = bt.GetComponent<Button>();
            }

            // fallback: take first button in panel
            if (nextLevelButton == null)
                nextLevelButton = winPanel.GetComponentInChildren<Button>(true);

            // Wire callback in code so it never becomes Missing after scene load
            if (nextLevelButton != null)
            {
                nextLevelButton.onClick.RemoveAllListeners();
                nextLevelButton.onClick.AddListener(Continue);
            }
        }
    }

    private Transform FindInChildren(Transform root, string objectName)
    {
        if (root == null) return null;
        if (root.name == objectName) return root;

        for (int i = 0; i < root.childCount; i++)
        {
            var found = FindInChildren(root.GetChild(i), objectName);
            if (found != null) return found;
        }
        return null;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (won) return;
        if (other == null || !other.CompareTag(playerTag)) return;

        won = true;
        CacheRefs();

        if (playerMovementScript != null)
            playerMovementScript.enabled = false;

        if (winPanel != null)
            winPanel.SetActive(true);

        // Read score from HUD text (this is what you see as truth)
        int coins = ParseFirstInt(hudScoreText != null ? hudScoreText.text : "");

        if (winText != null)
            winText.text = "You got - " + coins + " coins";

        // Unlock cursor so button can be clicked
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Prepare stats for next level
        if (RunProgressManager.Instance != null)
            RunProgressManager.Instance.PrepareForContinue();
    }

    private int ParseFirstInt(string s)
    {
        if (string.IsNullOrEmpty(s)) return 0;

        int value = 0;
        bool foundAny = false;

        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (c >= '0' && c <= '9')
            {
                foundAny = true;
                value = value * 10 + (c - '0');
            }
            else if (foundAny)
            {
                break;
            }
        }

        return foundAny ? value : 0;
    }

    public void Continue()
    {
        if (!won) return;

        // back to gameplay cursor for next level
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (RunProgressManager.Instance == null)
        {
            Debug.LogError("RunProgressManager missing. Add it in Level1 scene.");
            return;
        }

        RunProgressManager.Instance.ContinueToNextLevel();
    }
}