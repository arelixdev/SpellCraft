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
        if (_slots.Count > 0) HighlightSlot(0);
    }

    public void Init(SpellCraftingPanel panel)
    {
        Panel = panel;
        if (_selected >= 0) Panel.OnSlotChanged(_selected);
        RefreshPortColor();
    }

    public RectTransform GetActivePortRT() =>
        (_selected >= 0 && _selected < _slots.Count) ? _slots[_selected].port : null;

    public void RefreshPortColor()
    {
        if (_selected < 0 || _selected >= _slots.Count) return;
        var portImg = _slots[_selected].portImg;
        if (portImg == null) return;
        bool connected = Panel != null && Panel.WorkingGraph != null && Panel.WorkingGraph.launcherConnected;
        portImg.color = connected ? PortConnected : PortFree;
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
        view?.Port.GetComponent<Button>()?.onClick.AddListener(ToggleLauncherConnection);

        _slots.Add((view?.Shape, view?.Port, view?.PortImage));
    }

    private void ToggleLauncherConnection()
    {
        if (Panel?.WorkingGraph == null) return;
        Panel.WorkingGraph.launcherConnected = !Panel.WorkingGraph.launcherConnected;
        RefreshPortColor();
    }

    private void HighlightSlot(int index)
    {
        _selected = index;
        for (int i = 0; i < _slots.Count; i++)
            if (_slots[i].shape != null)
                _slots[i].shape.color = (i == _selected) ? ColActive : ColInactive;
        RefreshPortColor();
    }

    private void SelectSlot(int index)
    {
        if (_selected == index) return;
        Panel?.OnSlotChanged(index);
        HighlightSlot(index);
    }

}
