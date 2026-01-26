// PauseMenu.cs
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject optionsPanel;

    [Header("Menu Music (ONE track for both panels)")]
    [SerializeField] private AudioSource menuMusicSource;

    [Header("Options UI")]
    [SerializeField] private Slider volumeSlider;

    [Header("Save/Load (optional reference)")]
    [SerializeField] private SaveGameManager saveGameManager;

    private const string VolumeKey = "volume";
    private bool isPaused;

    private void Awake()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);

        Time.timeScale = 1f;
        isPaused = false;

        SetupSource(menuMusicSource);

        float v = PlayerPrefs.GetFloat(VolumeKey, 1f);
        ApplyVolume(v);

        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.wholeNumbers = false;
            volumeSlider.value = v;
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        // runtime safety: make buttons work even if Inspector shows Missing(Object)
        BindButtonsByName();
    }

    private void OnDestroy()
    {
        if (volumeSlider != null)
            volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (!isPaused) Pause();
            else Resume();
        }
    }

    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;

        if (pausePanel != null) pausePanel.SetActive(true);
        if (optionsPanel != null) optionsPanel.SetActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // start from beginning only when entering pause mode
        PlayFromStart(menuMusicSource);
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null) pausePanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        StopAndReset(menuMusicSource);
    }

    public void OpenOptions()
    {
        if (!isPaused) return;

        if (pausePanel != null) pausePanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(true);

        // DO NOT stop/restart music here (it must continue)
    }

    public void CloseOptions()
    {
        if (!isPaused) return;

        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(true);

        // DO NOT stop/restart music here (it must continue)
    }

    public void SaveGame()
    {
        var m = saveGameManager != null ? saveGameManager : SaveGameManager.Instance;
        if (m != null) m.Save();
    }

    public void LoadGame()
    {
        var m = saveGameManager != null ? saveGameManager : SaveGameManager.Instance;
        if (m != null)
        {
            Time.timeScale = 1f;
            StopAndReset(menuMusicSource);
            m.Load();
        }
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

    private void SetupSource(AudioSource src)
    {
        if (src == null) return;
        src.playOnAwake = false;
        src.loop = true;
        src.spatialBlend = 0f;
        src.volume = AudioListener.volume;
    }

    private void PlayFromStart(AudioSource src)
    {
        if (src == null || src.clip == null) return;
        if (src.isPlaying) return; // keeps it continuous when switching panels
        src.Stop();
        src.time = 0f;
        src.volume = AudioListener.volume;
        src.Play();
    }

    private void StopAndReset(AudioSource src)
    {
        if (src == null) return;
        src.Stop();
        src.time = 0f;
    }

    private void BindButtonsByName()
    {
        // Finds buttons by GameObject name (case-insensitive):
        // Resume, Save, Load, Options, Back
        Bind("Resume", Resume);
        Bind("Save", SaveGame);
        Bind("Load", LoadGame);
        Bind("Options", OpenOptions);
        Bind("Back", CloseOptions);
    }

    private void Bind(string buttonName, UnityEngine.Events.UnityAction action)
    {
        var btn = FindButton(buttonName);
        if (btn == null) return;

        btn.onClick.RemoveListener(action);
        btn.onClick.AddListener(action);
    }

    private Button FindButton(string name)
    {
        var buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null && buttons[i].gameObject.name.ToLower() == name.ToLower())
                return buttons[i];
        }
        return null;
    }
}
