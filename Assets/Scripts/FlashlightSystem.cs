using UnityEngine;

public class FlashlightSystem : MonoBehaviour
{
    [Header("Light")]
    [SerializeField] private Light flashlightLight;

    [Header("Battery")]
    [SerializeField] private float maxBattery = 100f;
    [SerializeField] private float drainPerSecond = 10f;

    private float currentBattery;
    private bool isOn;

    public float CurrentBattery => currentBattery;
    public float MaxBattery => maxBattery;
    public bool IsOn => isOn;

    private void Awake()
    {
        currentBattery = maxBattery;

        if (flashlightLight != null)
            flashlightLight.enabled = false;
    }

    private void Update()
    {
        if (!isOn)
            return;

        currentBattery -= drainPerSecond * Time.deltaTime;
        currentBattery = Mathf.Clamp(currentBattery, 0f, maxBattery);

        if (currentBattery <= 0f)
            TurnOff();
    }

    public void Toggle()
    {
        if (isOn)
            TurnOff();
        else
            TurnOn();
    }

    private void TurnOn()
    {
        if (currentBattery <= 0f)
            return;

        isOn = true;

        if (flashlightLight != null)
            flashlightLight.enabled = true;
    }

    private void TurnOff()
    {
        isOn = false;

        if (flashlightLight != null)
            flashlightLight.enabled = false;
    }

    public void AddBattery(float percent)
    {
        currentBattery += percent;
        currentBattery = Mathf.Clamp(currentBattery, 0f, maxBattery);
    }
}