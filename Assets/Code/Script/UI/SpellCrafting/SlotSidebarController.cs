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

    private readonly List<(Image shape, RectTransform port, TextMeshProUGUI spellLabel, Image cooldownOverlay)> _slots = new();
    private int _selected = -1;

    public int SlotCount => _slots.Count;

    private void OnEnable()
    {
        // S'abonne avant la phase Start() de tout le projet (règle Unity : toutes les
        // OnEnable() s'exécutent avant tous les Start()), donc on capte à coup sûr le
        // rebuild fait par SpellCaster.Start(), même si son ordre relatif au nôtre
        // n'est pas garanti.
        if (Caster != null)
            Caster.OnCraftingGraphChanged += RefreshAllSlotLabels;
    }

    private void OnDisable()
    {
        if (Caster != null)
            Caster.OnCraftingGraphChanged -= RefreshAllSlotLabels;
    }

    private void Start()
    {
        EnsureCaster();
        if (Caster == null || SlotPrefab == null) return;
        var slots = Caster.GetSlots();
        for (int i = 0; i < slots.Length; i++)
            SpawnSlot(i, slots[i]);
        HighlightSlot(_selected >= 0 ? _selected : 0);
        RefreshAllSlotLabels();
    }

    private void Update()
    {
        EnsureCaster();
        if (Caster == null) return;
        for (int i = 0; i < _slots.Count; i++)
        {
            var overlay = _slots[i].cooldownOverlay;
            if (overlay != null) overlay.fillAmount = Caster.GetCooldownRatio(i);
        }
    }

    // Caster est câblé dans l'Inspector vers le Player placé dans CETTE instance de la
    // scène Gameplay — détruit comme doublon par PlayerPersistence dès la 2e partie (le
    // Player réellement vivant est celui du tout premier chargement, DontDestroyOnLoad).
    // Start() s'exécute juste avant que ce doublon soit effectivement détruit (en fin de
    // frame), donc les slots s'affichent quand même — mais sans ce fallback, Update() se
    // fige ensuite : le cooldown affiché reste bloqué à sa dernière valeur pour de bon.
    private void EnsureCaster()
    {
        if (Caster != null) return;

        Caster = GameObject.FindWithTag("Player")?.GetComponent<SpellCaster>();
        if (Caster == null) return;

        // Le doublon précédent est déjà détruit (voir commentaire ci-dessus) : pas besoin de
        // s'en désabonner, mais sans ce réabonnement sur la vraie référence persistante, les
        // labels restent figés pour de bon (l'event dont ils dépendaient appartenait à
        // l'ancien Caster mort). Refresh immédiat pour rattraper le rebuild de
        // ResetForNewRun survenu pendant qu'on était sans abonnement valide.
        Caster.OnCraftingGraphChanged += RefreshAllSlotLabels;
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
            if (view?.CooldownOverlay != null) view.CooldownOverlay.sprite = visual.background;
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

        _slots.Add((view?.Background, view?.Port, view?.SpellLabelTemp, view?.CooldownOverlay));
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
