using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SlotSidebarController : MonoBehaviour
{
    [Header("References")]
    public Transform           SlotContainer;
    public GameObject          SlotPrefab;
    public SpellCaster         Caster;
    public SpellCraftingPanel  Panel;

    private readonly List<(Image shape, RectTransform port, Image portImg)> _slots = new();
    private int _selected = -1;

    public int SlotCount => _slots.Count;

    static readonly Color ColActive     = new(1.00f, 0.75f, 0.20f);
    static readonly Color ColInactive   = new(0.45f, 0.35f, 0.10f);
    static readonly Color PortConnected = new(0.25f, 0.90f, 0.30f);
    static readonly Color PortFree      = new(1.00f, 0.55f, 0.15f);

    private void Start()
    {
        if (Caster == null || SlotPrefab == null) return;
        var slots = Caster.GetSlots();
        for (int i = 0; i < slots.Length; i++)
            SpawnSlot(i, slots[i]);
        HighlightSlot(_selected >= 0 ? _selected : 0);
    }

    public void Init(SpellCraftingPanel panel)
    {
        Panel = panel;
        int initial = _selected >= 0 ? _selected : 0;
        Panel.OnSlotChanged(initial);
        HighlightSlot(initial);
        RefreshAllPortColors();
    }

    /// Returns the output port RectTransform for a given slot index.
    public RectTransform GetPortRT(int slotIndex) =>
        (slotIndex >= 0 && slotIndex < _slots.Count) ? _slots[slotIndex].port : null;

    public void RefreshAllPortColors()
    {
        var graph = Panel != null ? Panel.WorkingGraph : null;
        for (int i = 0; i < _slots.Count; i++)
        {
            var portImg = _slots[i].portImg;
            if (portImg == null) continue;
            bool connected = graph != null && graph.HasSlotEntry(i);
            portImg.color = connected ? PortConnected : PortFree;
        }
    }

    // ── Private ─────────────────────────────────────────────────────────────

    private void SpawnSlot(int index, SpellSlot slot)
    {
        var go   = Instantiate(SlotPrefab, SlotContainer);
        var view = go.GetComponent<SlotIconView>();
        go.SetActive(true);

        if (view != null)
        {
            if (view.Shape != null)     view.Shape.color     = ColInactive;
            if (view.PortImage != null) view.PortImage.color = PortFree;
            if (view.Label != null)     view.Label.text      = (index + 1).ToString();
        }

        int captured = index;
        go.GetComponent<Button>()?.onClick.AddListener(() => SelectSlot(captured));

        var portGO = view?.Port?.gameObject;
        if (portGO != null)
        {
            var lp = portGO.AddComponent<LauncherPortView>();
            lp.SlotIndex = captured;
            lp.Sidebar   = this;
        }

        _slots.Add((view?.Shape, view?.Port, view?.PortImage));
    }

    private void HighlightSlot(int index)
    {
        _selected = index;
        for (int i = 0; i < _slots.Count; i++)
            if (_slots[i].shape != null)
                _slots[i].shape.color = (i == _selected) ? ColActive : ColInactive;
    }

    private void SelectSlot(int index)
    {
        Panel?.OnSlotChanged(index);
        HighlightSlot(index);
    }
}
