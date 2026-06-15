using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SlotSidebarController : MonoBehaviour
{
    [Header("References")]
    public Transform                SlotContainer;
    public GameObject               SlotPrefab;
    public SpellCaster              Caster;
    public SpellCraftingPanel       Panel;
    public LauncherVisualRegistrySO VisualRegistry;

    private readonly List<(Image shape, RectTransform port, TextMeshProUGUI spellLabel)> _slots = new();
    private int _selected = -1;

    public int SlotCount => _slots.Count;

    private void Start()
    {
        if (Caster == null || SlotPrefab == null) return;
        var slots = Caster.GetSlots();
        for (int i = 0; i < slots.Length; i++)
            SpawnSlot(i, slots[i]);
        HighlightSlot(_selected >= 0 ? _selected : 0);
        RefreshAllSlotLabels();
    }

    public void Init(SpellCraftingPanel panel)
    {
        Panel = panel;
        int initial = _selected >= 0 ? _selected : 0;
        Panel.OnSlotChanged(initial);
        HighlightSlot(initial);
        RefreshAllSlotLabels();
    }

    /// Returns the output port RectTransform for a given slot index.
    public RectTransform GetPortRT(int slotIndex) =>
        (slotIndex >= 0 && slotIndex < _slots.Count) ? _slots[slotIndex].port : null;

    public void RefreshAllSlotLabels()
    {
        var graph = Panel?.WorkingGraph ?? Caster?.craftingGraph;
        var slots = Caster?.GetSlots();

        for (int i = 0; i < _slots.Count; i++)
        {
            var lbl = _slots[i].spellLabel;
            if (lbl == null) continue;

            string text = "";

            if (graph != null && graph.nodes.Count > 0)
            {
                text = BuildSlotLabel(i, graph);
            }
            else if (slots != null && i < slots.Length && slots[i]?.connectedSpell != null)
            {
                text = TraverseGraph(slots[i].connectedSpell, 0);
            }

            lbl.text = text;
        }
    }

    // ── Private ─────────────────────────────────────────────────────────────

    private static string BuildSlotLabel(int slotIndex, SpellGraphSO graph)
    {
        if (graph == null || !graph.TryGetSlotEntry(slotIndex, out int entryNode))
            return "";
        return TraverseGraph(graph, entryNode);
    }

    private static string TraverseGraph(SpellGraphSO graph, int entryNode)
    {
        var visited = new HashSet<int>();
        var queue   = new Queue<int>();
        var sb      = new StringBuilder();

        queue.Enqueue(entryNode);
        while (queue.Count > 0)
        {
            int idx = queue.Dequeue();
            if (!visited.Add(idx)) continue;
            if (idx < 0 || idx >= graph.nodes.Count) continue;

            var node = graph.nodes[idx];
            if (node != null && !string.IsNullOrEmpty(node.nodeName))
                sb.Append(node.nodeName[0]);

            foreach (int next in graph.GetOutputIndices(idx))
                queue.Enqueue(next);
        }

        return sb.ToString();
    }

    private void SpawnSlot(int index, SpellSlot slot)
    {
        var go   = Instantiate(SlotPrefab, SlotContainer);
        var view = go.GetComponent<SlotIconView>();
        go.SetActive(true);

        if (view?.SpellLabelTemp != null)
            view.SpellLabelTemp.text = "";

        if (VisualRegistry != null && slot?.launcherConfig != null
            && VisualRegistry.TryGet(slot.launcherConfig.launcherType, out var visual))
        {
            if (view?.Background != null) view.Background.sprite = visual.background;
            if (view?.Outline != null)
            {
                view.Outline.sprite = visual.outline;
                var rt = view.Outline.rectTransform;
                rt.sizeDelta        = Vector2.one * visual.outlineSize;
                rt.anchoredPosition = visual.outlineOffset;
            }
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

        _slots.Add((view?.Background, view?.Port, view?.SpellLabelTemp));
    }

    private void HighlightSlot(int index)
    {
        _selected = index;
    }

    private void SelectSlot(int index)
    {
        Panel?.OnSlotChanged(index);
        HighlightSlot(index);
    }
}
