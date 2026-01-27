// KeyManager.cs
using UnityEngine;
using TMPro;

public class KeyManager : MonoBehaviour
{
    public static KeyManager Instance { get; private set; }

    [SerializeField] private TMP_Text keysText;
    [SerializeField] private string prefix = "Keys = ";

    private int m_keys;
    public int Keys => m_keys;

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
        SetKeys(m_keys + amount);
    }

    public bool TrySpend(int amount)
    {
        if (m_keys < amount) return false;
        SetKeys(m_keys - amount);
        return true;
    }

    public void SetKeys(int value)
    {
        m_keys = Mathf.Max(0, value);
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (keysText != null)
            keysText.text = prefix + m_keys;
    }
}
