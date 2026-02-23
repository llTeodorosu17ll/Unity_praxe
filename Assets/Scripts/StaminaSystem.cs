using UnityEngine;

public class StaminaSystem : MonoBehaviour
{
    [Header("Stamina Settings")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float drainPerSecond = 20f;
    [SerializeField] private float regenPerSecond = 15f;

    private float currentStamina;

    public float CurrentStamina => currentStamina;
    public float MaxStamina => maxStamina;

    public bool CanSprint => currentStamina > 0f;

    private void Awake()
    {
        currentStamina = maxStamina;
    }

    public void UpdateStamina(bool isSprinting)
    {
        if (isSprinting && currentStamina > 0f)
        {
            currentStamina -= drainPerSecond * Time.deltaTime;
        }
        else
        {
            currentStamina += regenPerSecond * Time.deltaTime;
        }

        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
    }
}