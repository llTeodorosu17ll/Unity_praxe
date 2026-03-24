using UnityEngine;
using TMPro;

public class UIHint : MonoBehaviour
{
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private bool debugTest = false;

    private void Reset()
    {
        if (hintText == null)
            hintText = GetComponentInChildren<TMP_Text>(true);
    }

    private void OnEnable()
    {
        if (GameManager.HasInstance)
            GameManager.Instance.RegisterUIHint(this);
    }

    private void OnDisable()
    {
        if (GameManager.HasInstance)
            GameManager.Instance.UnregisterUIHint(this);
    }

    private void Start()
    {
        if (!debugTest) return;

        Show("UI TEST");
        Invoke(nameof(Hide), 2f);
    }

    public void Show(string message)
    {
        if (hintText == null)
        {
            Debug.LogError("UIHint: hintText is not assigned.", this);
            return;
        }

        hintText.gameObject.SetActive(true);
        hintText.enabled = true;
        hintText.text = message;
    }

    public void Hide()
    {
        if (hintText == null)
            return;

        hintText.text = "";
        hintText.gameObject.SetActive(false);
    }
}