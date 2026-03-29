using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DefaultExecutionOrder(12000)]
public class StatusHudUI : MonoBehaviour
{
    private const string ScorePrefix = "Score count = ";
    private const string KeysPrefix = "Keys count = ";
    private const string StaminaLabel = "STAMINA";
    private const string BatteryLabel = "FLASHLIGHT";
    private const float UpdateInterval = 0.05f;

    [Header("Required")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text keysText;

    [Header("Optional Bars")]
    [SerializeField] private Image staminaFill;
    [SerializeField] private TMP_Text staminaText;
    [SerializeField] private Image batteryFill;
    [SerializeField] private TMP_Text batteryText;

    private float nextUpdateTime;

    private void OnEnable()
    {
        if (GameManager.HasInstance)
        {
            GameManager.Instance.ScoreChanged += OnScoreChanged_Event;
            GameManager.Instance.KeysChanged += OnKeysChanged_Event;
        }

        RefreshAll();
    }

    private void OnDisable()
    {
        if (!GameManager.HasInstance)
            return;

        GameManager.Instance.ScoreChanged -= OnScoreChanged_Event;
        GameManager.Instance.KeysChanged -= OnKeysChanged_Event;
    }

    private void Update()
    {
        if (Time.unscaledTime < nextUpdateTime)
            return;

        nextUpdateTime = Time.unscaledTime + UpdateInterval;
        RefreshBars();
    }

    private void OnScoreChanged_Event(int value)
    {
        if (scoreText != null)
            scoreText.text = ScorePrefix + value;
    }

    private void OnKeysChanged_Event(int value)
    {
        if (keysText != null)
            keysText.text = KeysPrefix + value;
    }

    private void RefreshAll()
    {
        if (!GameManager.HasInstance)
            return;

        OnScoreChanged_Event(GameManager.Instance.Score);
        OnKeysChanged_Event(GameManager.Instance.Keys);
        RefreshBars();
    }

    private void RefreshBars()
    {
        if (!GameManager.HasInstance)
            return;

        StaminaSystem stamina = GameManager.Instance.StaminaSystem;
        FlashlightSystem flashlight = GameManager.Instance.FlashlightSystem;

        if (stamina != null)
        {
            float ratio = stamina.MaxStamina <= 0.0001f
                ? 0f
                : Mathf.Clamp01(stamina.CurrentStamina / stamina.MaxStamina);

            if (staminaFill != null)
                staminaFill.fillAmount = ratio;

            if (staminaText != null)
                staminaText.text = $"{StaminaLabel} {Mathf.RoundToInt(ratio * 100f)}%";
        }

        if (flashlight != null)
        {
            float ratio = flashlight.MaxBattery <= 0.0001f
                ? 0f
                : Mathf.Clamp01(flashlight.CurrentBattery / flashlight.MaxBattery);

            if (batteryFill != null)
                batteryFill.fillAmount = ratio;

            if (batteryText != null)
            {
                batteryText.text = flashlight.IsOn
                    ? $"{BatteryLabel} {Mathf.RoundToInt(ratio * 100f)}%"
                    : $"{BatteryLabel} {Mathf.RoundToInt(ratio * 100f)}% (OFF)";
            }
        }
    }
}