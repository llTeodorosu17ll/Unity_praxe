using UnityEngine;
using TMPro;

public class UIHint : MonoBehaviour
{
    [SerializeField] private TMP_Text hintText;

    // optional debug toggle
    [SerializeField] private bool debugTest = false;

    private void Reset()
    {
        hintText = GetComponentInChildren<TMP_Text>(true);
    }

    private void Awake()
    {
        if (!debugTest) return;

        Debug.Log("UIHint Awake (debug): " + gameObject.name, this);
        Show("UI TEST");
        Invoke(nameof(Hide), 2f);
    }

    public void Show(string msg)
    {
        if (hintText == null)
        {
            Debug.LogError("UIHint: hintText is NOT assigned!", this);
            return;
        }

        hintText.gameObject.SetActive(true);
        if (!hintText.enabled) hintText.enabled = true;

        hintText.text = msg;
    }

    public void Hide()
    {
        if (hintText == null) return;

        hintText.text = "";
        hintText.gameObject.SetActive(false);
    }
}
