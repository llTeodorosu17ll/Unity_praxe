using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RunProgressManager : MonoBehaviour
{
    public static RunProgressManager Instance { get; private set; }

    [Header("Apply delay after loading next scene")]
    [SerializeField] private int applyDelayFrames = 2;

    private bool prepared;
    private bool hasPendingApply;

    private int savedScore;
    private int savedKeys;
    private float savedStamina;
    private float savedBattery;
    private bool savedFlashlightOn;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ✅ This is what WinScript calls on win
    public void PrepareForContinue()
    {
        CaptureCurrentStats();
        prepared = true;
    }

    public void ContinueToNextLevel()
    {
        if (!prepared)
            CaptureCurrentStats();

        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        int nextIndex = currentIndex + 1;

        if (nextIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogWarning("No next level in Build Settings.");
            return;
        }

        hasPendingApply = true;
        SceneManager.LoadScene(nextIndex);
    }

    private void CaptureCurrentStats()
    {
        // Score
        var scoreManager = FindFirstObjectByType<ScoreManager>();
        savedScore = scoreManager != null ? scoreManager.Score : 0;

        // Keys
        var keyManager = FindFirstObjectByType<KeyManager>();
        savedKeys = keyManager != null ? keyManager.Keys : 0;

        // Stamina
        StaminaSystem stamina = null;
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) stamina = player.GetComponent<StaminaSystem>();
        if (stamina == null) stamina = FindFirstObjectByType<StaminaSystem>();
        savedStamina = stamina != null ? stamina.CurrentStamina : 0f;

        // Flashlight
        var flashlight = FindFirstObjectByType<FlashlightSystem>();
        savedBattery = flashlight != null ? flashlight.CurrentBattery : 0f;
        savedFlashlightOn = flashlight != null && flashlight.IsOn;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!hasPendingApply) return;
        StartCoroutine(ApplyStatsAfterDelay());
    }

    private IEnumerator ApplyStatsAfterDelay()
    {
        for (int i = 0; i < applyDelayFrames; i++)
            yield return null;

        // Apply Score
        var scoreManager = FindFirstObjectByType<ScoreManager>();
        if (scoreManager != null) scoreManager.SetScore(savedScore);

        // Apply Keys
        var keyManager = FindFirstObjectByType<KeyManager>();
        if (keyManager != null) keyManager.SetKeys(savedKeys);

        // Apply Stamina
        StaminaSystem stamina = null;
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) stamina = player.GetComponent<StaminaSystem>();
        if (stamina == null) stamina = FindFirstObjectByType<StaminaSystem>();
        if (stamina != null) stamina.SetCurrentStamina(savedStamina);

        // Apply Flashlight
        var flashlight = FindFirstObjectByType<FlashlightSystem>();
        if (flashlight != null) flashlight.SetBatteryAndState(savedBattery, savedFlashlightOn, silent: true);

        // Back to gameplay cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        prepared = false;
        hasPendingApply = false;
    }
}