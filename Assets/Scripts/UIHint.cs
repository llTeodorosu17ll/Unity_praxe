using UnityEngine;
using TMPro;

public class UIHint : MonoBehaviour
{
    [SerializeField] private TMP_Text hintText;

    private void Reset()
    {
        hintText = GetComponentInChildren<TMP_Text>(true);
    }

    public void Show(string msg)
    {
        if (hintText == null)
        {
            Debug.LogError("UIHint: hintText is NOT assigned!", this);
            return;
        }

        hintText.gameObject.SetActive(true);

        // ВАЖНО: включаем сам компонент TMP тоже
        if (!hintText.enabled) hintText.enabled = true;

        hintText.text = msg;
    }

    public void Hide()
    {
        if (hintText == null) return;

        hintText.text = "";

        // можно просто прятать объект
        hintText.gameObject.SetActive(false);
    }


    private void Awake()
    {
        Debug.Log("UIHint Awake: " + gameObject.name, this);
        Show("UI TEST");
        Invoke(nameof(Hide), 2f);
    }


}
