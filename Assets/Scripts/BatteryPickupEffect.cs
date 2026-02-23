using UnityEngine;

public class BatteryPickupEffect : MonoBehaviour
{
    [SerializeField] private float batteryPercent = 25f;

    public void Apply()
    {
        FlashlightSystem flashlight = FindFirstObjectByType<FlashlightSystem>();

        if (flashlight != null)
            flashlight.AddBattery(batteryPercent);
    }
}