using UnityEngine;
using TMPro;

public class KeyManager : MonoBehaviour
{
    [SerializeField] private TMP_Text keyText;

    public int Keys { get; private set; }

    private void Start()
    {
        UpdateUI();
    }

    public void AddKey()
    {
        Keys++;
        UpdateUI();
    }

    public void SetKeys(int value)
    {
        Keys = value;
        UpdateUI();
    }

    public bool TrySpend(int amount = 1)
    {
        if (Keys < amount)
            return false;

        Keys -= amount;
        UpdateUI();
        return true;
    }

    public void ResetKeys()
    {
        Keys = 0;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (keyText != null)
            keyText.text = "Keys count = " + Keys.ToString();
    }
}
