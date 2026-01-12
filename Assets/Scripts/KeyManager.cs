using UnityEngine;
using TMPro;

public class KeyManager : MonoBehaviour
{
    public static KeyManager Instance { get; private set; }

    [SerializeField] private TMP_Text keysText;
    [SerializeField] private string prefix = "Keys = ";

    public int Keys { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        RefreshUI();
    }

    public void Add(int amount)
    {
        Keys += amount;
        if (Keys < 0) Keys = 0;
        RefreshUI();
    }

    public bool TrySpend(int amount)
    {
        if (Keys < amount) return false;
        Keys -= amount;
        RefreshUI();
        return true;
    }

    private void RefreshUI()
    {
        if (keysText != null)
            keysText.text = prefix + Keys;
    }
}
