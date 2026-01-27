using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject optionsPanel;

    [Header("Disable While Paused")]
    [SerializeField] private MonoBehaviour[] disableOnPause; // drag PlayerMovement (+ Player) here

    [Header("Menu Music (optional)")]
    [SerializeField] private AudioSource menuMusicSource;

    [Header("Options UI (optional)")]
    [SerializeField] private Slider volumeSlider;

    [Header("Save/Load (optional)")]
    [SerializeField] private SaveGameManager saveGameManager;

    private const string VolumeKey = "volume";
    private bool isPaused;

    private void Awake()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);

        Time.timeScale = 1f;
        isPaused = false;

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

        // lock cursor at start (gameplay)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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

        // stop gameplay scripts so look/move doesn't run in pause
        SetGameplayEnabled(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // start menu music from beginning when opening pause
        if (menuMusicSource != null && menuMusicSource.clip != null && !menuMusicSource.isPlaying)
        {
            menuMusicSource.time = 0f;
            menuMusicSource.Play();
        }
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null) pausePanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);

        SetGameplayEnabled(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // stop menu music when leaving pause
        if (menuMusicSource != null)
        {
            menuMusicSource.Stop();
            menuMusicSource.time = 0f;
        }
    }

    public void OpenOptions()
    {
        if (!isPaused) return;
        if (pausePanel != null) pausePanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(true);
        // music continues
    }

    public void CloseOptions()
    {
        if (!isPaused) return;
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(true);
        // music continues
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
            isPaused = false;

            if (pausePanel != null) pausePanel.SetActive(false);
            if (optionsPanel != null) optionsPanel.SetActive(false);

            SetGameplayEnabled(true);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (menuMusicSource != null)
            {
                menuMusicSource.Stop();
                menuMusicSource.time = 0f;
            }

            m.Load();
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
