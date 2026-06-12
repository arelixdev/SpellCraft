using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// Vertical sidebar that shows one button per spell slot.
/// AutoCast launchers render as circles (Knob sprite), all others as squares.
public class SlotSidebarController : MonoBehaviour
{
    [Header("References")]
    public Transform SlotContainer;   // VerticalLayoutGroup parent
    public Button    ApplyButton;
    public TMP_Text  BudgetLabel;
    public Slider    BudgetSlider;

    private SpellCraftingPanel _panel;
    private readonly List<(Image shape, RectTransform port, Image portImg)> _slots = new();
    private int _selected = -1;

    static readonly Color ColActive      = new(1.00f, 0.75f, 0.20f);
    static readonly Color ColInactive    = new(0.45f, 0.35f, 0.10f);
    static readonly Color PortConnected  = new(0.25f, 0.90f, 0.30f);
    static readonly Color PortFree       = new(1.00f, 0.55f, 0.15f);

    public void Init(SpellCraftingPanel panel, SpellCaster caster)
    {
        _panel = panel;
        _slots.Clear();
        foreach (Transform child in SlotContainer) Destroy(child.gameObject);

        var slots = caster ? caster.GetSlots() : System.Array.Empty<SpellSlot>();
        for (int i = 0; i < slots.Length; i++)
        {
            bool isAutocast = slots[i]?.launcherConfig?.launcherType == LauncherType.AutoCast;
            _slots.Add(BuildSlotButton(i, isAutocast));
        }

        ApplyButton?.onClick.RemoveAllListeners();
        ApplyButton?.onClick.AddListener(_panel.OnApply);
        SelectSlot(0);
        RefreshPortColor();
    }

    public void RefreshBudget(int current, int max)
    {
        if (BudgetLabel  != null) BudgetLabel.text  = $"{current} / {max}";
        if (BudgetSlider != null) { BudgetSlider.maxValue = max; BudgetSlider.value = current; }
    }

    public RectTransform GetActivePortRT() =>
        (_selected >= 0 && _selected < _slots.Count) ? _slots[_selected].port : null;

    public void RefreshPortColor()
    {
        if (_selected < 0 || _selected >= _slots.Count) return;
        bool connected = _panel != null && _panel.WorkingGraph != null && _panel.WorkingGraph.launcherConnected;
        _slots[_selected].portImg.color = connected ? PortConnected : PortFree;
    }

    // ── Private ─────────────────────────────────────────────────────────────

    private (Image shape, RectTransform port, Image portImg) BuildSlotButton(int index, bool circle)
    {
        // Root — transparent, catches raycasts for the Button
        var root = new GameObject($"Slot{index}", typeof(RectTransform));
        root.transform.SetParent(SlotContainer, false);
        var le               = root.AddComponent<LayoutElement>();
        le.preferredWidth    = 64f;
        le.preferredHeight   = 64f;
        le.minWidth          = 64f;
        le.minHeight         = 64f;
        var hitImg           = root.AddComponent<Image>();
        hitImg.color         = Color.clear;

        // Visual shape
        var shapeGO = new GameObject("Shape", typeof(RectTransform));
        shapeGO.transform.SetParent(root.transform, false);
        var srt         = shapeGO.GetComponent<RectTransform>();
        srt.anchorMin   = new Vector2(0.1f, 0.1f);
        srt.anchorMax   = new Vector2(0.9f, 0.9f);
        srt.offsetMin   = srt.offsetMax = Vector2.zero;
        var shapeImg    = shapeGO.AddComponent<Image>();
        shapeImg.color  = ColInactive;
        shapeImg.raycastTarget = false;

        if (circle)
            shapeImg.sprite = MakeCircleSprite(64);

        // Output port — small orange square on the right edge, pointing toward graph canvas
        var portGO  = new GameObject("OutputPort", typeof(RectTransform));
        portGO.transform.SetParent(root.transform, false);
        var portRT  = portGO.GetComponent<RectTransform>();
        portRT.anchorMin = new Vector2(1f, 0.35f);
        portRT.anchorMax = new Vector2(1f, 0.65f);
        portRT.pivot     = new Vector2(0f, 0.5f);
        portRT.sizeDelta = new Vector2(12f, 0f);
        var portImg       = portGO.AddComponent<Image>();
        portImg.color     = PortFree;
        portImg.raycastTarget = true; // must catch clicks

        // Click toggles the launcher→node0 connection
        var portBtn = portGO.AddComponent<Button>();
        portBtn.onClick.AddListener(ToggleLauncherConnection);

        // Number label
        var lblGO              = new GameObject("Label", typeof(RectTransform));
        lblGO.transform.SetParent(root.transform, false);
        var lrt                = lblGO.GetComponent<RectTransform>();
        lrt.anchorMin          = Vector2.zero;
        lrt.anchorMax          = Vector2.one;
        lrt.offsetMin          = lrt.offsetMax = Vector2.zero;
        var txt                = lblGO.AddComponent<TextMeshProUGUI>();
        txt.text               = (index + 1).ToString();
        txt.alignment          = TextAlignmentOptions.Center;
        txt.fontSize           = 20;
        txt.color              = Color.white;
        txt.raycastTarget      = false;

        // Button
        int captured = index;
        root.AddComponent<Button>().onClick.AddListener(() => SelectSlot(captured));

        return (shapeImg, portRT, portImg);
    }

    private void ToggleLauncherConnection()
    {
        var graph = _panel != null ? _panel.WorkingGraph : null;
        if (graph == null) return;
        graph.launcherConnected = !graph.launcherConnected;
        RefreshPortColor();
    }

    private void SelectSlot(int index)
    {
        if (_selected == index) return; // port click bubbles to root — guard against reload

        _selected = index;
        _panel?.OnSlotChanged(index);   // updates WorkingGraph
        for (int i = 0; i < _slots.Count; i++)
            _slots[i].shape.color = (i == _selected) ? ColActive : ColInactive;
        RefreshPortColor();             // reflect new slot's launcherConnected state
    }

    private static Sprite MakeCircleSprite(int res)
    {
        var tex    = new Texture2D(res, res, TextureFormat.RGBA32, false);
        float half = res * 0.5f;
        float r    = half - 1f;
        for (int y = 0; y < res; y++)
        for (int x = 0; x < res; x++)
        {
            float dx = x - half, dy = y - half;
            float a  = Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res);
    }
}
