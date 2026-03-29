using UnityEngine;

[DefaultExecutionOrder(11000)]
public class FlashlightSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Light flashlightLight;
    [SerializeField] private AudioSource clickAudioSource;
    [SerializeField] private Transform mountPoint;
    [SerializeField] private Transform aimSource;

    [Header("Battery")]
    [SerializeField] private float maxBattery = 100f;
    [SerializeField] private float drainPerSecond = 10f;

    [Header("Sound")]
    [SerializeField, Range(0f, 1f)] private float clickVolume = 1f;

    [Header("Rotation")]
    [SerializeField] private Vector3 extraRotationOffsetEuler = Vector3.zero;
    [SerializeField] private float rotationSmooth = 0f;

    private float currentBattery;
    private bool isOn;

    private Vector3 mountLocalPosOffset;
    private Quaternion aimToFlashOffset = Quaternion.identity;
    private Quaternion extraRotationOffset = Quaternion.identity;

    private bool posOffsetCaptured;
    private bool rotOffsetCaptured;

    public float CurrentBattery => currentBattery;
    public float MaxBattery => maxBattery;
    public bool IsOn => isOn;

    private void Reset()
    {
        if (flashlightLight == null)
            flashlightLight = GetComponentInChildren<Light>(true);

        if (clickAudioSource == null)
            clickAudioSource = GetComponent<AudioSource>();
    }

    private void Awake()
    {
        maxBattery = Mathf.Max(0.01f, maxBattery);
        drainPerSecond = Mathf.Max(0f, drainPerSecond);
        clickVolume = Mathf.Clamp01(clickVolume);

        if (flashlightLight == null)
            Debug.LogError("FlashlightSystem: flashlightLight is not assigned.", this);

        if (mountPoint == null)
            Debug.LogError("FlashlightSystem: mountPoint is not assigned.", this);

        if (aimSource == null)
            Debug.LogError("FlashlightSystem: aimSource is not assigned.", this);

        extraRotationOffset = Quaternion.Euler(extraRotationOffsetEuler);
        currentBattery = maxBattery;

        SetOnInternal(false, true);
        CaptureOffsetsIfPossible();
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
            TurnOff(false);
            return;
        }

        currentBattery -= drainPerSecond * Time.deltaTime;
        currentBattery = Mathf.Clamp(currentBattery, 0f, maxBattery);

        if (currentBattery <= 0f)
            TurnOff(false);
    }

    private void LateUpdate()
    {
        CaptureOffsetsIfPossible();

        if (mountPoint != null && posOffsetCaptured)
            transform.position = mountPoint.TransformPoint(mountLocalPosOffset);

        if (aimSource == null || !rotOffsetCaptured)
            return;

        Quaternion targetRotation = aimSource.rotation * aimToFlashOffset * extraRotationOffset;

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
        if (isOn)
            TurnOff(false);
        else
            TurnOn(false);
    }

    public void TurnOn(bool silent)
    {
        if (currentBattery <= 0f)
            return;

        if (isOn)
            return;

        isOn = true;

        if (flashlightLight != null)
            flashlightLight.enabled = true;

        if (!silent)
            PlayClick();
    }

    public void TurnOff(bool silent)
    {
        if (!isOn)
            return;

        isOn = false;

        if (flashlightLight != null)
            flashlightLight.enabled = false;

        if (!silent)
            PlayClick();
    }

    public void AddBattery(float percent)
    {
        if (percent <= 0f)
            return;

        float amount = (percent / 100f) * maxBattery;
        currentBattery = Mathf.Clamp(currentBattery + amount, 0f, maxBattery);
    }

    public void SetBatteryAndState(float batteryValue, bool turnOn, bool silent = true)
    {
        currentBattery = Mathf.Clamp(batteryValue, 0f, maxBattery);

        if (turnOn && currentBattery > 0f)
            TurnOn(silent);
        else
            TurnOff(silent);
    }

    public void ResetBatteryToFullAndOff()
    {
        currentBattery = maxBattery;
        TurnOff(true);
    }

    private void SetOnInternal(bool on, bool silent)
    {
        isOn = on;

        if (flashlightLight != null)
            flashlightLight.enabled = isOn;

        if (!silent)
            PlayClick();
    }

    private void PlayClick()
    {
        if (clickAudioSource == null || clickAudioSource.clip == null)
            return;

        clickAudioSource.PlayOneShot(clickAudioSource.clip, clickVolume);
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
}