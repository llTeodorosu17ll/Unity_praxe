using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    private const string VolumeKey = "volume";
    private const string UiActionMapName = "UI";
    private const string GameplayActionMapName = "Player";

    [Header("Panels")]
    [SerializeField] private RectTransform pausePanel;
    [SerializeField] private RectTransform optionsPanel;

    [Header("Gameplay Scripts To Disable")]
    [SerializeField] private MonoBehaviour[] disableOnPause;

    [Header("Input")]
    [SerializeField] private PlayerInput playerInput;

    [Header("Audio / UI")]
    [SerializeField] private AudioSource menuMusicSource;
    [SerializeField] private Slider volumeSlider;

    public static bool IsPaused { get; private set; }

    private string previousActionMap;
    private InputActionMap uiMap;
    private InputActionMap gameplayMap;
    private bool isWarmupRunning;

    private void Awake()
    {
        IsPaused = false;
        Time.timeScale = 1f;

        ValidateRefs();

        SetPanelActive(pausePanel, false);
        SetPanelActive(optionsPanel, false);

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
            else if (optionsPanel != null && optionsPanel.gameObject.activeSelf)
                CloseOptions_Button();
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

        SetPanelActive(pausePanel, false);
        SetPanelActive(optionsPanel, true);
    }

    // Called by Button OnClick
    public void CloseOptions_Button()
    {
        if (!IsPaused)
            return;

        SetPanelActive(optionsPanel, false);
        SetPanelActive(pausePanel, true);
    }

    private void Pause_Internal()
    {
        IsPaused = true;
        Time.timeScale = 0f;

        SetPanelActive(pausePanel, true);
        SetPanelActive(optionsPanel, false);

        SetGameplayEnabled(false);
        PauseInput();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (menuMusicSource != null && menuMusicSource.clip != null && !menuMusicSource.isPlaying)
            menuMusicSource.Play();
    }

    private void Resume_Internal()
    {
        IsPaused = false;
        Time.timeScale = 1f;

        SetPanelActive(pausePanel, false);
        SetPanelActive(optionsPanel, false);

        SetGameplayEnabled(true);
        ResumeInput();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (menuMusicSource != null)
        {
            menuMusicSource.Stop();
            menuMusicSource.time = 0f;
        }
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

        SetPanelActive(pausePanel, false);
        SetPanelActive(optionsPanel, false);

        isWarmupRunning = false;
    }

    private void ValidateRefs()
    {
        if (pausePanel == null)
            Debug.LogError("PauseMenu: pausePanel is not assigned.", this);

        if (optionsPanel == null)
            Debug.LogError("PauseMenu: optionsPanel is not assigned.", this);

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

        uiMap = playerInput.actions.FindActionMap(UiActionMapName, false);
        gameplayMap = playerInput.actions.FindActionMap(GameplayActionMapName, false);
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

    private void SetPanelActive(RectTransform panel, bool active)
    {
        if (panel != null)
            panel.gameObject.SetActive(active);
    }
}