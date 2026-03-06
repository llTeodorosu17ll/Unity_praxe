using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DefaultExecutionOrder(11000)]
public class FlashlightSystem : MonoBehaviour
{
    [Header("Light")]
    [SerializeField] private Light flashlightLight;

    [Header("Battery")]
    [SerializeField] private float maxBattery = 100f;
    [SerializeField] private float drainPerSecond = 10f;

    [Header("Click Sound (uses AudioSource.clip)")]
    [SerializeField] private AudioSource clickAudioSource;
    [SerializeField, Range(0f, 1f)] private float clickVolume = 1f;
    [SerializeField] private bool playClickOnToggle = true;

    [Header("UI (optional)")]
    [SerializeField] private Image batteryFill;
    [SerializeField] private TMP_Text batteryText;
    [SerializeField] private string batteryLabel = "FLASHLIGHT";
    [SerializeField] private bool batteryFillFromRight = false;
    [SerializeField] private bool showPercentText = true;
    [SerializeField] private bool showOnOffState = true;
    [SerializeField] private bool autoConfigureFillImage = true;

    [Header("Follow (flashlight can be outside Player)")]
    [SerializeField] private Transform mountPoint;
    [SerializeField] private bool followPosition = true;

    [Header("Aim / Rotate With Camera")]
    [SerializeField] private bool rotateWithAim = true;
    [SerializeField] private Transform aimSource;
    [SerializeField] private bool autoFindAimSource = true;
    [SerializeField] private bool aimOnlyWhenOn = false;
    [SerializeField] private bool ignoreRoll = true;
    [SerializeField] private Vector3 extraRotationOffsetEuler = Vector3.zero;
    [SerializeField] private float rotationSmooth = 0f;

    private float currentBattery;
    private bool isOn;

    public float CurrentBattery => currentBattery;
    public float MaxBattery => maxBattery;
    public bool IsOn => isOn;

    private Vector3 mountLocalPosOffset;
    private Quaternion aimToFlashOffset = Quaternion.identity;
    private Quaternion extraRotationOffset = Quaternion.identity;

    private bool posOffsetCaptured;
    private bool rotOffsetCaptured;

    private void Awake()
    {
        maxBattery = Mathf.Max(0.01f, maxBattery);
        drainPerSecond = Mathf.Max(0f, drainPerSecond);
        clickVolume = Mathf.Clamp01(clickVolume);

        if (clickAudioSource == null)
            clickAudioSource = GetComponent<AudioSource>();

        extraRotationOffset = Quaternion.Euler(extraRotationOffsetEuler);

        if (autoFindAimSource && aimSource == null)
            aimSource = TryFindAimSource();

        ConfigureFillImage(batteryFill, batteryFillFromRight);

        currentBattery = maxBattery;

        SetOnInternal(false, silent: true);

        CaptureOffsetsIfPossible();
        UpdateUI();
    }

    private void OnValidate()
    {
        maxBattery = Mathf.Max(0.01f, maxBattery);
        drainPerSecond = Mathf.Max(0f, drainPerSecond);
        clickVolume = Mathf.Clamp01(clickVolume);
        extraRotationOffset = Quaternion.Euler(extraRotationOffsetEuler);
    }

    private void Update()
    {
        if (!isOn)
            return;

        if (currentBattery <= 0f)
        {
            TurnOff(silent: false);
            return;
        }

        float before = currentBattery;

        currentBattery -= drainPerSecond * Time.deltaTime;
        currentBattery = Mathf.Clamp(currentBattery, 0f, maxBattery);

        if (!Mathf.Approximately(before, currentBattery))
            UpdateUI();

        if (currentBattery <= 0f)
            TurnOff(silent: false);
    }

    private void LateUpdate()
    {
        if (autoFindAimSource && aimSource == null)
            aimSource = TryFindAimSource();

        CaptureOffsetsIfPossible();

        if (followPosition && mountPoint != null && posOffsetCaptured)
            transform.position = mountPoint.TransformPoint(mountLocalPosOffset);

        if (!rotateWithAim)
            return;

        if (aimOnlyWhenOn && !isOn)
            return;

        if (aimSource == null || !rotOffsetCaptured)
            return;

        Quaternion baseAimRotation = aimSource.rotation;

        if (ignoreRoll)
        {
            Vector3 e = baseAimRotation.eulerAngles;
            baseAimRotation = Quaternion.Euler(e.x, e.y, 0f);
        }

        Quaternion targetRotation = baseAimRotation * aimToFlashOffset * extraRotationOffset;

        if (rotationSmooth <= 0f)
        {
            transform.rotation = targetRotation;
        }
        else
        {
            float t = 1f - Mathf.Exp(-rotationSmooth * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, t);
        }
    }

    public void Toggle()
    {
        if (isOn) TurnOff(silent: false);
        else TurnOn(silent: false);
    }

    public void TurnOn(bool silent)
    {
        if (currentBattery <= 0f) return;
        if (isOn) return;

        isOn = true;

        if (flashlightLight != null)
            flashlightLight.enabled = true;

        if (!silent && playClickOnToggle)
            PlayClick();

        UpdateUI();
    }

    public void TurnOff(bool silent)
    {
        if (!isOn) return;

        isOn = false;

        if (flashlightLight != null)
            flashlightLight.enabled = false;

        if (!silent && playClickOnToggle)
            PlayClick();

        UpdateUI();
    }

    private void SetOnInternal(bool on, bool silent)
    {
        isOn = on;

        if (flashlightLight != null)
            flashlightLight.enabled = isOn;

        if (!silent && playClickOnToggle)
            PlayClick();
    }

    private void PlayClick()
    {
        if (clickAudioSource == null) return;
        if (clickAudioSource.clip == null) return;

        clickAudioSource.PlayOneShot(clickAudioSource.clip, clickVolume);
    }

    public void AddBattery(float percent)
    {
        if (percent <= 0f) return;

        float add = (percent / 100f) * maxBattery;
        currentBattery = Mathf.Clamp(currentBattery + add, 0f, maxBattery);
        UpdateUI();
    }

    public void SetBatteryAndState(float batteryValue, bool turnOn, bool silent = true)
    {
        currentBattery = Mathf.Clamp(batteryValue, 0f, maxBattery);

        if (turnOn && currentBattery > 0f) TurnOn(silent);
        else TurnOff(silent);

        UpdateUI();
    }

    public void ResetBatteryToFullAndOff()
    {
        currentBattery = maxBattery;
        TurnOff(true);
        UpdateUI();
    }

    public void SetUI(Image fill, TMP_Text text)
    {
        batteryFill = fill;
        batteryText = text;
        ConfigureFillImage(batteryFill, batteryFillFromRight);
        UpdateUI();
    }

    private void UpdateUI()
    {
        float ratio = (maxBattery <= 0.0001f) ? 0f : Mathf.Clamp01(currentBattery / maxBattery);

        if (batteryFill != null)
            batteryFill.fillAmount = ratio;

        if (batteryText != null)
        {
            if (showPercentText)
            {
                int pct = Mathf.RoundToInt(ratio * 100f);
                batteryText.text = showOnOffState
                    ? (isOn ? $"{batteryLabel}  {pct}%" : $"{batteryLabel}  {pct}%  (OFF)")
                    : $"{batteryLabel}  {pct}%";
            }
            else
            {
                batteryText.text = batteryLabel;
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

    private void CaptureOffsetsIfPossible()
    {
        if (!posOffsetCaptured && mountPoint != null)
        {
            mountLocalPosOffset = mountPoint.InverseTransformPoint(transform.position);
            posOffsetCaptured = true;
        }

        if (!rotOffsetCaptured && aimSource != null)
        {
            aimToFlashOffset = Quaternion.Inverse(aimSource.rotation) * transform.rotation;
            rotOffsetCaptured = true;
        }
    }

    private Transform TryFindAimSource()
    {
        var pm = FindFirstObjectByType<PlayerMovement>();
        if (pm != null)
        {
            var t = pm.transform.Find("CameraTarget");
            if (t != null) return t;
        }

        if (Camera.main != null)
            return Camera.main.transform;

        var cam = FindFirstObjectByType<Camera>();
        if (cam != null)
            return cam.transform;

        return null;
    }
}