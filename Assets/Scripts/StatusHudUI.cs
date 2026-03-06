using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DefaultExecutionOrder(12000)]
public class StatusHudUI : MonoBehaviour
{
    [Header("Targets (optional, auto-find if empty)")]
    [SerializeField] private StaminaSystem staminaSystem;
    [SerializeField] private FlashlightSystem flashlightSystem;

    [Header("UI Refs (optional). If empty and Auto Create UI enabled -> created at runtime.")]
    [SerializeField] private Image staminaFill;
    [SerializeField] private TMP_Text staminaText;

    [SerializeField] private Image batteryFill;
    [SerializeField] private TMP_Text batteryText;

    [Header("Auto Create UI")]
    [SerializeField] private bool autoCreateUI = true;

    [Tooltip("Bottom padding from screen edge.")]
    [SerializeField] private float bottomPadding = 22f;

    [Tooltip("Side padding from screen edge.")]
    [SerializeField] private float sidePadding = 22f;

    [Tooltip("Bar size in pixels.")]
    [SerializeField] private Vector2 barSize = new Vector2(260f, 18f);

    [Tooltip("0 = every frame. Otherwise updates at this interval (seconds).")]
    [SerializeField] private float updateInterval = 0.05f;

    private float _nextUpdateTime;

    private Sprite _whiteSprite;
    private TMP_FontAsset _refFont;
    private Material _refMaterial;

    private void Awake()
    {
        AutoFindTargets();
        CacheTmpStyleReference();

        if (autoCreateUI)
            EnsureUiExists();
    }

    private void Update()
    {
        if (updateInterval > 0f)
        {
            if (Time.unscaledTime < _nextUpdateTime)
                return;
            _nextUpdateTime = Time.unscaledTime + updateInterval;
        }

        if (staminaSystem == null || flashlightSystem == null)
            AutoFindTargets();

        UpdateBars();
    }

    private void AutoFindTargets()
    {
        if (staminaSystem == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                staminaSystem = player.GetComponent<StaminaSystem>();

            if (staminaSystem == null)
                staminaSystem = FindFirstObjectByType<StaminaSystem>();
        }

        if (flashlightSystem == null)
            flashlightSystem = FindFirstObjectByType<FlashlightSystem>();
    }

    private void CacheTmpStyleReference()
    {
        // Copy font/material from any TMP text already in your Canvas (Score/Keys/etc)
        var anyTmp = GetComponentInChildren<TMP_Text>(true);
        if (anyTmp != null)
        {
            _refFont = anyTmp.font;
            _refMaterial = anyTmp.fontSharedMaterial;
        }

        if (_refFont == null)
            _refFont = TMP_Settings.defaultFontAsset;
    }

    private void EnsureUiExists()
    {
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = GetComponent<Canvas>();

        if (canvas == null)
        {
            Debug.LogError("StatusHudUI: No Canvas found. Put this script on Canvas or under Canvas.", this);
            return;
        }

        if (_whiteSprite == null)
            _whiteSprite = CreateWhiteSprite();

        // Stamina (bottom-left)
        if (staminaFill == null || staminaText == null)
        {
            var staminaGroup = CreateGroup(
                parent: canvas.transform,
                name: "HUD_Stamina",
                anchorMin: new Vector2(0f, 0f),
                anchorMax: new Vector2(0f, 0f),
                pivot: new Vector2(0f, 0f),
                anchoredPos: new Vector2(sidePadding, bottomPadding)
            );

            CreateBarWithText(
                parent: staminaGroup,
                label: "STAMINA",
                outFill: out staminaFill,
                outText: out staminaText
            );
        }

        // Battery (bottom-right)
        if (batteryFill == null || batteryText == null)
        {
            var batteryGroup = CreateGroup(
                parent: canvas.transform,
                name: "HUD_Battery",
                anchorMin: new Vector2(1f, 0f),
                anchorMax: new Vector2(1f, 0f),
                pivot: new Vector2(1f, 0f),
                anchoredPos: new Vector2(-sidePadding, bottomPadding)
            );

            CreateBarWithText(
                parent: batteryGroup,
                label: "FLASHLIGHT",
                outFill: out batteryFill,
                outText: out batteryText,
                rightAligned: true
            );
        }
    }

    private void UpdateBars()
    {
        // Stamina
        if (staminaSystem != null)
        {
            float s = SafeRatio(staminaSystem.CurrentStamina, staminaSystem.MaxStamina);
            if (staminaFill != null) staminaFill.fillAmount = s;

            if (staminaText != null)
            {
                int pct = Mathf.RoundToInt(s * 100f);
                staminaText.text = $"STAMINA  {pct}%";
            }
        }

        // Battery
        if (flashlightSystem != null)
        {
            float b = SafeRatio(flashlightSystem.CurrentBattery, flashlightSystem.MaxBattery);
            if (batteryFill != null) batteryFill.fillAmount = b;

            if (batteryText != null)
            {
                int pct = Mathf.RoundToInt(b * 100f);
                batteryText.text = flashlightSystem.IsOn
                    ? $"FLASHLIGHT  {pct}%"
                    : $"FLASHLIGHT  {pct}%  (OFF)";
            }
        }
    }

    private float SafeRatio(float value, float max)
    {
        if (max <= 0.0001f) return 0f;
        return Mathf.Clamp01(value / max);
    }

    // ---------- UI building helpers ----------

    private RectTransform CreateGroup(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rt = (RectTransform)go.transform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = Vector2.zero;

        return rt;
    }

    private void CreateBarWithText(RectTransform parent, string label, out Image outFill, out TMP_Text outText, bool rightAligned = false)
    {
        // Container
        parent.sizeDelta = new Vector2(barSize.x, barSize.y + 28f);

        // Label text (single line, includes %)
        var textGo = new GameObject(label + "_Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(parent, false);

        var textRt = (RectTransform)textGo.transform;
        textRt.anchorMin = new Vector2(0f, 1f);
        textRt.anchorMax = new Vector2(1f, 1f);
        textRt.pivot = new Vector2(0.5f, 1f);
        textRt.anchoredPosition = new Vector2(0f, 0f);
        textRt.sizeDelta = new Vector2(0f, 24f);

        var tmp = textGo.GetComponent<TextMeshProUGUI>();
        tmp.font = _refFont;
        if (_refMaterial != null) tmp.fontSharedMaterial = _refMaterial;
        tmp.fontSize = 18;
        tmp.alignment = rightAligned ? TextAlignmentOptions.Right : TextAlignmentOptions.Left;
        tmp.text = label;

        // Bar background
        var bgGo = new GameObject(label + "_BarBG", typeof(RectTransform), typeof(Image));
        bgGo.transform.SetParent(parent, false);

        var bgRt = (RectTransform)bgGo.transform;
        bgRt.anchorMin = new Vector2(0f, 0f);
        bgRt.anchorMax = new Vector2(1f, 0f);
        bgRt.pivot = new Vector2(0.5f, 0f);
        bgRt.anchoredPosition = new Vector2(0f, 0f);
        bgRt.sizeDelta = new Vector2(0f, barSize.y);

        var bg = bgGo.GetComponent<Image>();
        bg.sprite = _whiteSprite;
        bg.type = Image.Type.Sliced;
        bg.color = new Color(0f, 0f, 0f, 0.45f);

        // Bar fill
        var fillGo = new GameObject(label + "_BarFill", typeof(RectTransform), typeof(Image));
        fillGo.transform.SetParent(bgGo.transform, false);

        var fillRt = (RectTransform)fillGo.transform;
        fillRt.anchorMin = new Vector2(0f, 0f);
        fillRt.anchorMax = new Vector2(1f, 1f);
        fillRt.pivot = new Vector2(0f, 0.5f);
        fillRt.anchoredPosition = Vector2.zero;
        fillRt.sizeDelta = Vector2.zero;

        var fill = fillGo.GetComponent<Image>();
        fill.sprite = _whiteSprite;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
        fill.fillAmount = 1f;
        fill.color = Color.white;

        outFill = fill;
        outText = tmp;
    }

    private Sprite CreateWhiteSprite()
    {
        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply(false, true);

        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f);
    }
}