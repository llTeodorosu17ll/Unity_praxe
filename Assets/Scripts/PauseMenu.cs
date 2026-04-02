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

    [Header("Disable On Pause")]
    [SerializeField] private MonoBehaviour[] disableOnPause;

    [Header("Input")]
    [SerializeField] private PlayerInput playerInput;

    [Header("Audio / UI")]
    [SerializeField] private AudioSource menuMusicSource;
    [SerializeField] private Slider volumeSlider;

    public static bool IsPaused { get; private set; }

    private Graphic[] pauseGraphics;
    private Selectable[] pauseSelectables;

    private Graphic[] optionsGraphics;
    private Selectable[] optionsSelectables;

    private void Awake()
    {
        IsPaused = false;
        Time.timeScale = 1f;

        CachePanelUi();

        SetPanelVisible(pauseGraphics, pauseSelectables, false);
        SetPanelVisible(optionsGraphics, optionsSelectables, false);

        float volume = PlayerPrefs.GetFloat(VolumeKey, 1f);
        ApplyVolume(volume);

        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.value = volume;
            volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged_Event);
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged_Event);
        }

        if (menuMusicSource != null)
        {
            menuMusicSource.playOnAwake = false;
            menuMusicSource.loop = true;
            menuMusicSource.spatialBlend = 0f;
            menuMusicSource.volume = AudioListener.volume;

            if (menuMusicSource.clip != null)
            {
                bool oldMute = menuMusicSource.mute;
                menuMusicSource.mute = true;
                menuMusicSource.Play();
                menuMusicSource.Pause();
                menuMusicSource.time = 0f;
                menuMusicSource.mute = oldMute;
            }
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDestroy()
    {
        if (volumeSlider != null)
            volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged_Event);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (!IsPaused)
            {
                Pause_Internal();
            }
            else if (IsPanelVisible(optionsGraphics))
            {
                CloseOptions_Button();
            }
            else
            {
                Resume_Internal();
            }
        }
    }

    public void Resume_Button()
    {
        Resume_Internal();
    }

    public void OpenOptions_Button()
    {
        if (!IsPaused)
            return;

        SetPanelVisible(pauseGraphics, pauseSelectables, false);
        SetPanelVisible(optionsGraphics, optionsSelectables, true);
    }

    public void CloseOptions_Button()
    {
        if (!IsPaused)
            return;

        SetPanelVisible(optionsGraphics, optionsSelectables, false);
        SetPanelVisible(pauseGraphics, pauseSelectables, true);
    }

    public void SaveGame_Button()
    {
        if (GameManager.HasInstance)
            GameManager.Instance.SaveGame();
    }

    public void LoadGame_Button()
    {
        if (GameManager.HasInstance)
            GameManager.Instance.LoadGame();
    }

    private void Pause_Internal()
    {
        IsPaused = true;
        Time.timeScale = 0f;

        SetPanelVisible(pauseGraphics, pauseSelectables, true);
        SetPanelVisible(optionsGraphics, optionsSelectables, false);

        SetGameplayEnabled(false);
        SwitchToMap(UiActionMapName);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (menuMusicSource != null && menuMusicSource.clip != null && !menuMusicSource.isPlaying)
            menuMusicSource.UnPause();
    }

    private void Resume_Internal()
    {
        IsPaused = false;
        Time.timeScale = 1f;

        SetPanelVisible(pauseGraphics, pauseSelectables, false);
        SetPanelVisible(optionsGraphics, optionsSelectables, false);

        SetGameplayEnabled(true);
        SwitchToMap(GameplayActionMapName);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (menuMusicSource != null)
        {
            menuMusicSource.Pause();
            menuMusicSource.time = 0f;
        }
    }

    private void SwitchToMap(string mapName)
    {
        if (playerInput == null || !playerInput.enabled || playerInput.actions == null)
            return;

        InputActionMap map = playerInput.actions.FindActionMap(mapName, false);
        if (map != null)
            playerInput.SwitchCurrentActionMap(map.name);
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

    private void CachePanelUi()
    {
        if (pausePanel != null)
        {
            pauseGraphics = pausePanel.GetComponentsInChildren<Graphic>(true);
            pauseSelectables = pausePanel.GetComponentsInChildren<Selectable>(true);
        }

        if (optionsPanel != null)
        {
            optionsGraphics = optionsPanel.GetComponentsInChildren<Graphic>(true);
            optionsSelectables = optionsPanel.GetComponentsInChildren<Selectable>(true);
        }
    }

    private void SetPanelVisible(Graphic[] graphics, Selectable[] selectables, bool visible)
    {
        if (graphics != null)
        {
            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] != null)
                    graphics[i].enabled = visible;
            }
        }

        if (selectables != null)
        {
            for (int i = 0; i < selectables.Length; i++)
            {
                if (selectables[i] != null)
                    selectables[i].interactable = visible;
            }
        }
    }

    private bool IsPanelVisible(Graphic[] graphics)
    {
        if (graphics == null || graphics.Length == 0)
            return false;

        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] != null && graphics[i].enabled)
                return true;
        }

        return false;
    }
}