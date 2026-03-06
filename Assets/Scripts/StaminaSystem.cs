using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StaminaSystem : MonoBehaviour
{
    [Header("Stamina Settings")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float drainPerSecond = 20f;
    [SerializeField] private float regenPerSecond = 15f;

    [Header("Rules")]
    [Tooltip("If stamina is below this percent, you cannot START sprint/jump.")]
    [SerializeField] private float minPercentToStartActions = 10f;

    [Header("UI (optional)")]
    [SerializeField] private Image staminaFill;
    [SerializeField] private TMP_Text staminaText;
    [SerializeField] private string staminaLabel = "STAMINA";
    [SerializeField] private bool staminaFillFromRight = false;
    [SerializeField] private bool showPercentText = true;
    [SerializeField] private bool autoConfigureFillImage = true;

    private float currentStamina;

    public float CurrentStamina => currentStamina;
    public float MaxStamina => maxStamina;
    public float CurrentPercent01 => (maxStamina <= 0.0001f) ? 0f : Mathf.Clamp01(currentStamina / maxStamina);

    public bool HasAtLeastPercent(float percent)
    {
        float p01 = Mathf.Clamp01(percent / 100f);
        return CurrentPercent01 >= p01;
    }

    public bool HasEnoughToStart() => HasAtLeastPercent(minPercentToStartActions);
    public bool CanSprint => HasEnoughToStart();

    private void Awake()
    {
        maxStamina = Mathf.Max(0.01f, maxStamina);
        drainPerSecond = Mathf.Max(0f, drainPerSecond);
        regenPerSecond = Mathf.Max(0f, regenPerSecond);

        ConfigureFillImage(staminaFill, staminaFillFromRight);

        currentStamina = maxStamina;
        UpdateUI();
    }

    public void UpdateStamina(bool isSprinting)
    {
        if (isSprinting && currentStamina > 0f)
            currentStamina -= drainPerSecond * Time.deltaTime;
        else
            currentStamina += regenPerSecond * Time.deltaTime;

        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
        UpdateUI();
    }

    public bool TrySpendPercent(float percent)
    {
        float amount = (Mathf.Clamp01(percent / 100f)) * maxStamina;
        if (amount <= 0f) return true;

        if (currentStamina < amount)
            return false;

        currentStamina = Mathf.Clamp(currentStamina - amount, 0f, maxStamina);
        UpdateUI();
        return true;
    }

    public void SetCurrentStamina(float value)
    {
        currentStamina = Mathf.Clamp(value, 0f, maxStamina);
        UpdateUI();
    }

    public void ResetToFull()
    {
        currentStamina = maxStamina;
        UpdateUI();
    }

    public void SetUI(Image fill, TMP_Text text)
    {
        staminaFill = fill;
        staminaText = text;
        ConfigureFillImage(staminaFill, staminaFillFromRight);
        UpdateUI();
    }

    private void UpdateUI()
    {
        float ratio = CurrentPercent01;

        if (staminaFill != null)
            staminaFill.fillAmount = ratio;

        if (staminaText != null)
        {
            if (showPercentText)
            {
                int pct = Mathf.RoundToInt(ratio * 100f);
                staminaText.text = $"{staminaLabel}  {pct}%";
            }
            else
            {
                staminaText.text = staminaLabel;
            }
        }
    }

    private void ConfigureFillImage(Image img, bool fromRight)
    {
        if (!autoConfigureFillImage) return;
        if (img == null) return;

        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        img.fillOrigin = fromRight ? 1 : 0;
    }
}