using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DefaultExecutionOrder(12000)]
public class StatusHudUI : MonoBehaviour
{
    [Header("Bars")]
    [SerializeField] private Image staminaFill;
    [SerializeField] private TMP_Text staminaText;

    [SerializeField] private Image batteryFill;
    [SerializeField] private TMP_Text batteryText;

    [Header("Counters")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text keysText;

    [Header("Labels")]
    [SerializeField] private string staminaLabel = "STAMINA";
    [SerializeField] private string batteryLabel = "FLASHLIGHT";
    [SerializeField] private string scorePrefix = "Score count = ";
    [SerializeField] private string keysPrefix = "Keys count = ";

    [Header("Update")]
    [SerializeField] private float updateInterval = 0.05f;

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
        if (updateInterval > 0f && Time.unscaledTime < nextUpdateTime)
            return;

        nextUpdateTime = Time.unscaledTime + updateInterval;
        RefreshBars();
    }

    private void OnScoreChanged_Event(int value)
    {
        if (scoreText != null)
            scoreText.text = scorePrefix + value;
    }

    private void OnKeysChanged_Event(int value)
    {
        if (keysText != null)
            keysText.text = keysPrefix + value;
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
                staminaText.text = $"{staminaLabel}  {Mathf.RoundToInt(ratio * 100f)}%";
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
                    ? $"{batteryLabel}  {Mathf.RoundToInt(ratio * 100f)}%"
                    : $"{batteryLabel}  {Mathf.RoundToInt(ratio * 100f)}%  (OFF)";
            }
        }
    }
}