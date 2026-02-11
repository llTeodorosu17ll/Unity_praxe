using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [Header("Panels (keep them ACTIVE in hierarchy)")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject optionsPanel;

    [Header("Gameplay scripts to disable on pause")]
    [Tooltip("Drag your PlayerMovement + CameraLook scripts here.")]
    [SerializeField] private MonoBehaviour[] disableOnPause;

    [Header("Input System (optional)")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private string uiActionMapName = "UI";
    [SerializeField] private string gameplayActionMapName = "Player";

    [Header("Optional")]
    [SerializeField] private AudioSource menuMusicSource;
    [SerializeField] private Slider volumeSlider;

    [Header("Debug")]
    [SerializeField] private bool debugPauseSpike = true;
    [Tooltip("TEMP test: skip playing pause music to see if audio causes the freeze.")]
    [SerializeField] private bool skipPauseMusic = true;

    public static bool IsPaused { get; private set; }

    private const string VolumeKey = "volume";

    private CanvasGroup pauseGroup;
    private CanvasGroup optionsGroup;

    private string previousActionMap;
    private InputActionMap uiMap;
    private InputActionMap gameplayMap;

    private EventSystem eventSystem;
    private bool isWarmupRunning;

    // =========================
    // Unity native methods (order)
    // =========================
    private void Awake()
    {
        IsPaused = false;
        Time.timeScale = 1f;

        if (playerInput == null)
            playerInput = FindFirstObjectByType<PlayerInput>();

        eventSystem = FindFirstObjectByType<EventSystem>();

        pauseGroup = EnsureCanvasGroup(pausePanel);
        optionsGroup = EnsureCanvasGroup(optionsPanel);

        HideGroup(pauseGroup);
        HideGroup(optionsGroup);

        CacheActionMaps();

        float v = PlayerPrefs.GetFloat(VolumeKey, 1f);
        ApplyVolume(v);

        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.value = v;
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        SetupMusic(menuMusicSource);

        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;

        Canvas.ForceUpdateCanvases();

        if (playerInput != null)
            playerInput.ActivateInput();
    }

    private void Start()
    {
        StartCoroutine(WarmupRealPauseCycle());
    }

    private void OnDestroy()
    {
        if (volumeSlider != null)
            volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
    }

    private void Update()
    {
        if (isWarmupRunning) return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (!IsPaused) Pause();
            else Resume();
        }
    }

    // =========================
    // Public API
    // =========================
    public void Pause()
    {
        float t0 = Time.realtimeSinceStartup;
        LogStep("ESC pressed -> Pause()", t0);

        IsPaused = true;
        LogStep("IsPaused=true", t0);

        Time.timeScale = 0f;
        LogStep("Time.timeScale=0", t0);

        ShowGroup(pauseGroup);
        HideGroup(optionsGroup);
        LogStep("UI groups set", t0);

        SetGameplayEnabled(false);
        LogStep("Gameplay scripts disabled", t0);

        PauseInput();
        LogStep("PauseInput done", t0);

        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
        LogStep("Cursor unlocked + visible", t0);

        if (menuMusicSource != null && menuMusicSource.clip != null && !menuMusicSource.isPlaying)
        {
            if (skipPauseMusic)
            {
                LogStep("Music SKIPPED (debug flag)", t0);
            }
            else
            {
                menuMusicSource.time = 0f;
                menuMusicSource.Play();
                LogStep("Music Play()", t0);
            }
        }
    }

    public void Resume()
    {
        float t0 = Time.realtimeSinceStartup;
        LogStep("Resume()", t0);

        IsPaused = false;
        Time.timeScale = 1f;

        HideGroup(pauseGroup);
        HideGroup(optionsGroup);

        SetGameplayEnabled(true);
        ResumeInput();

        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;

        if (menuMusicSource != null)
        {
            menuMusicSource.Stop();
            menuMusicSource.time = 0f;
        }

        LogStep("Resumed OK", t0);
    }

    public void OpenOptions()
    {
        if (!IsPaused) return;
        HideGroup(pauseGroup);
        ShowGroup(optionsGroup);
    }

    public void CloseOptions()
    {
        if (!IsPaused) return;
        HideGroup(optionsGroup);
        ShowGroup(pauseGroup);
    }

    // =========================
    // Warmup (optional)
    // =========================
    private IEnumerator WarmupRealPauseCycle()
    {
        isWarmupRunning = true;

        yield return null;
        yield return new WaitForEndOfFrame();

        if (playerInput != null)
            playerInput.ActivateInput();

        CacheActionMaps();

        float prevTimeScale = Time.timeScale;

        // Pause without showing UI (groups already hidden)
        Time.timeScale = 0f;
        SetGameplayEnabled(false);
        PauseInput();

        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;

        yield return null;

        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;

        ResumeInput();
        SetGameplayEnabled(true);
        Time.timeScale = prevTimeScale;

        HideGroup(pauseGroup);
        HideGroup(optionsGroup);

        isWarmupRunning = false;
    }

    // =========================
    // Internal helpers
    // =========================
    private void LogStep(string label, float t0)
    {
        if (!debugPauseSpike) return;
        float ms = (Time.realtimeSinceStartup - t0) * 1000f;
        Debug.Log($"[PauseMenu] {label}  ({ms:F1} ms)");
    }

    private CanvasGroup EnsureCanvasGroup(GameObject panel)
    {
        if (panel == null) return null;

        if (!panel.activeSelf)
            panel.SetActive(true);

        var cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.AddComponent<CanvasGroup>();
        return cg;
    }

    private void ShowGroup(CanvasGroup cg)
    {
        if (cg == null) return;
        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;
    }

    private void HideGroup(CanvasGroup cg)
    {
        if (cg == null) return;
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
    }

    private void CacheActionMaps()
    {
        previousActionMap = string.Empty;
        uiMap = null;
        gameplayMap = null;

        if (playerInput == null || playerInput.actions == null) return;

        if (playerInput.currentActionMap != null)
            previousActionMap = playerInput.currentActionMap.name;

        uiMap = playerInput.actions.FindActionMap(uiActionMapName, false);
        gameplayMap = playerInput.actions.FindActionMap(gameplayActionMapName, false);
    }

    private void PauseInput()
    {
        if (playerInput == null) return;

        playerInput.ActivateInput();

        if (playerInput.currentActionMap != null)
            previousActionMap = playerInput.currentActionMap.name;

        if (playerInput.actions != null && uiMap == null)
            uiMap = playerInput.actions.FindActionMap(uiActionMapName, false);

        if (uiMap != null)
            playerInput.SwitchCurrentActionMap(uiMap.name);
    }

    private void ResumeInput()
    {
        if (playerInput == null) return;

        playerInput.ActivateInput();

        if (playerInput.actions != null && gameplayMap == null)
            gameplayMap = playerInput.actions.FindActionMap(gameplayActionMapName, false);

        if (gameplayMap != null)
        {
            playerInput.SwitchCurrentActionMap(gameplayMap.name);
            return;
        }

        if (!string.IsNullOrWhiteSpace(previousActionMap) && playerInput.actions != null)
        {
            var map = playerInput.actions.FindActionMap(previousActionMap, false);
            if (map != null) playerInput.SwitchCurrentActionMap(map.name);
        }
    }

    private void SetGameplayEnabled(bool enabled)
    {
        if (disableOnPause == null) return;

        for (int i = 0; i < disableOnPause.Length; i++)
        {
            if (disableOnPause[i] != null)
                disableOnPause[i].enabled = enabled;
        }
    }

    private void SetupMusic(AudioSource src)
    {
        if (src == null) return;
        src.playOnAwake = false;
        src.loop = true;
        src.spatialBlend = 0f;
        src.volume = AudioListener.volume;
    }

    private void OnVolumeChanged(float v)
    {
        ApplyVolume(v);
        PlayerPrefs.SetFloat(VolumeKey, v);
        PlayerPrefs.Save();
    }

    private void ApplyVolume(float v)
    {
        v = Mathf.Clamp01(v);
        AudioListener.volume = v;

        if (menuMusicSource != null)
            menuMusicSource.volume = v;
    }
}
