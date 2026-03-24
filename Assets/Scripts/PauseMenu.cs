using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [Header("Panels (must already have CanvasGroup)")]
    [SerializeField] private CanvasGroup pauseGroup;
    [SerializeField] private CanvasGroup optionsGroup;

    [Header("Gameplay scripts to disable on pause")]
    [SerializeField] private MonoBehaviour[] disableOnPause;

    [Header("Input System")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private string uiActionMapName = "UI";
    [SerializeField] private string gameplayActionMapName = "Player";

    [Header("Optional")]
    [SerializeField] private AudioSource menuMusicSource;
    [SerializeField] private Slider volumeSlider;

    [Header("Debug")]
    [SerializeField] private bool debugPauseSpike = false;
    [SerializeField] private bool skipPauseMusic = true;

    public static bool IsPaused { get; private set; }

    private const string VolumeKey = "volume";

    private string previousActionMap;
    private InputActionMap uiMap;
    private InputActionMap gameplayMap;
    private bool isWarmupRunning;

    private void Awake()
    {
        IsPaused = false;
        Time.timeScale = 1f;

        ValidateRefs();

        HideGroup(pauseGroup);
        HideGroup(optionsGroup);

        CacheActionMaps();

        float volume = PlayerPrefs.GetFloat(VolumeKey, 1f);
        ApplyVolume(volume);

        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.value = volume;
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged_Event);
        }

        SetupMusic(menuMusicSource);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Canvas.ForceUpdateCanvases();

        if (playerInput != null)
            playerInput.ActivateInput();
    }

    private void Start()
    {
        StartCoroutine(WarmupPauseCycle());
    }

    private void OnDestroy()
    {
        if (volumeSlider != null)
            volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged_Event);
    }

    private void Update()
    {
        if (isWarmupRunning)
            return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (!IsPaused)
                Pause_Internal();
            else
                Resume_Internal();
        }
    }

    // Called by Button OnClick
    public void Resume_Button()
    {
        Resume_Internal();
    }

    // Called by Button OnClick
    public void OpenOptions_Button()
    {
        if (!IsPaused)
            return;

        HideGroup(pauseGroup);
        ShowGroup(optionsGroup);
    }

    // Called by Button OnClick
    public void CloseOptions_Button()
    {
        if (!IsPaused)
            return;

        HideGroup(optionsGroup);
        ShowGroup(pauseGroup);
    }

    private void Pause_Internal()
    {
        float t0 = Time.realtimeSinceStartup;
        LogStep("Pause", t0);

        IsPaused = true;
        Time.timeScale = 0f;

        ShowGroup(pauseGroup);
        HideGroup(optionsGroup);

        SetGameplayEnabled(false);
        PauseInput();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (menuMusicSource != null && menuMusicSource.clip != null && !menuMusicSource.isPlaying)
        {
            if (!skipPauseMusic)
                menuMusicSource.Play();
        }

        LogStep("Pause done", t0);
    }

    private void Resume_Internal()
    {
        float t0 = Time.realtimeSinceStartup;
        LogStep("Resume", t0);

        IsPaused = false;
        Time.timeScale = 1f;

        HideGroup(pauseGroup);
        HideGroup(optionsGroup);

        SetGameplayEnabled(true);
        ResumeInput();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (menuMusicSource != null)
        {
            menuMusicSource.Stop();
            menuMusicSource.time = 0f;
        }

        LogStep("Resume done", t0);
    }

    private IEnumerator WarmupPauseCycle()
    {
        isWarmupRunning = true;

        yield return null;
        yield return new WaitForEndOfFrame();

        if (playerInput != null)
            playerInput.ActivateInput();

        CacheActionMaps();

        float previousTimeScale = Time.timeScale;

        Time.timeScale = 0f;
        SetGameplayEnabled(false);
        PauseInput();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        yield return null;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        ResumeInput();
        SetGameplayEnabled(true);
        Time.timeScale = previousTimeScale;

        HideGroup(pauseGroup);
        HideGroup(optionsGroup);

        isWarmupRunning = false;
    }

    private void ValidateRefs()
    {
        if (pauseGroup == null)
            Debug.LogError("PauseMenu: pauseGroup is not assigned.", this);

        if (optionsGroup == null)
            Debug.LogError("PauseMenu: optionsGroup is not assigned.", this);

        if (playerInput == null)
            Debug.LogError("PauseMenu: playerInput is not assigned.", this);
    }

    private void CacheActionMaps()
    {
        previousActionMap = string.Empty;
        uiMap = null;
        gameplayMap = null;

        if (playerInput == null || playerInput.actions == null)
            return;

        if (playerInput.currentActionMap != null)
            previousActionMap = playerInput.currentActionMap.name;

        uiMap = playerInput.actions.FindActionMap(uiActionMapName, false);
        gameplayMap = playerInput.actions.FindActionMap(gameplayActionMapName, false);
    }

    private void PauseInput()
    {
        if (playerInput == null)
            return;

        playerInput.ActivateInput();

        if (playerInput.currentActionMap != null)
            previousActionMap = playerInput.currentActionMap.name;

        if (uiMap != null)
            playerInput.SwitchCurrentActionMap(uiMap.name);
    }

    private void ResumeInput()
    {
        if (playerInput == null)
            return;

        playerInput.ActivateInput();

        if (gameplayMap != null)
        {
            playerInput.SwitchCurrentActionMap(gameplayMap.name);
            return;
        }

        if (!string.IsNullOrWhiteSpace(previousActionMap) && playerInput.actions != null)
        {
            InputActionMap map = playerInput.actions.FindActionMap(previousActionMap, false);
            if (map != null)
                playerInput.SwitchCurrentActionMap(map.name);
        }
    }

    private void SetGameplayEnabled(bool enabled)
    {
        if (disableOnPause == null)
            return;

        for (int i = 0; i < disableOnPause.Length; i++)
        {
            if (disableOnPause[i] != null)
                disableOnPause[i].enabled = enabled;
        }
    }

    private void SetupMusic(AudioSource source)
    {
        if (source == null)
            return;

        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.volume = AudioListener.volume;
    }

    private void OnVolumeChanged_Event(float value)
    {
        ApplyVolume(value);
        PlayerPrefs.SetFloat(VolumeKey, value);
        PlayerPrefs.Save();
    }

    private void ApplyVolume(float value)
    {
        value = Mathf.Clamp01(value);
        AudioListener.volume = value;

        if (menuMusicSource != null)
            menuMusicSource.volume = value;
    }

    private void ShowGroup(CanvasGroup group)
    {
        if (group == null)
            return;

        group.alpha = 1f;
        group.interactable = true;
        group.blocksRaycasts = true;
    }

    private void HideGroup(CanvasGroup group)
    {
        if (group == null)
            return;

        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    private void LogStep(string label, float t0)
    {
        if (!debugPauseSpike)
            return;

        float ms = (Time.realtimeSinceStartup - t0) * 1000f;
        Debug.Log($"[PauseMenu] {label} ({ms:F1} ms)", this);
    }
}