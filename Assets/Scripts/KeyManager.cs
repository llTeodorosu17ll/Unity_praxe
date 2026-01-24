using UnityEngine;
using TMPro;

public class KeyManager : MonoBehaviour
{
    public static KeyManager Instance { get; private set; }

    [SerializeField] private TMP_Text keysText;
    [SerializeField] private string prefix = "Keys = ";

    private int m_keys;

    public int Keys
    {
        get => m_keys;
        private set
        {
            m_keys = Mathf.Max(0, value);

            if (keysText != null)
                keysText.text = prefix + m_keys;
        }
    }

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
        Keys = Keys; // refresh UI
    }

    public void Add(int amount = 1) => Keys += amount;

    public bool TrySpend(int amount = 1)
    {
        if (Keys < amount) return false;
        Keys -= amount;
        return true;
    }

    public void SetKeys(int value)
    {
        Keys = value;
    }
}
