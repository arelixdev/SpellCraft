using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Sirenix.OdinInspector;

public class SpellCaster : MonoBehaviour
{
    [Title("Casting")]
    [Tooltip("Point from which projectiles are spawned. If null, uses the caster's transform position.")]
    [SerializeField] private Transform _firePoint;

    [Title("Crit (base values, boostable via nodes)")]
    [Range(0f, 1f)]  public float baseCritChance     = 0f;
    [Range(1f, 5f)]  public float baseCritMultiplier = 1.5f;

    [Title("Crafting Graph")]
    [Tooltip("Shared spell graph built in the crafting panel. Populated automatically on first open.")]
    public SpellGraphSO craftingGraph;

    [Title("Spell Loadout (initial defaults)")]
    [ListDrawerSettings(ShowIndexLabels = true, NumberOfItemsPerPage = 6)]
    [SerializeField] private SpellSlot[] _spellSlots;

    private float[]       _cooldownTimers;
    private bool           _castingEnabled = true;
    private RelicManager   _relicManager;

    // Fired whenever craftingGraph is rebuilt or mutated (RebuildCraftingGraphFromSlots,
    // CollectNode). Subscribers refresh instead of relying on Start() order (unordered
    // relative to SpellCaster's own Start(), which is when the graph actually becomes
    // valid) or on the player manually reopening the crafting panel.
    public event System.Action OnCraftingGraphChanged;

    // Fired when AddSlot() grows _spellSlots at runtime (relique "nouveau sort"), pour que
    // l'UI ajoute juste l'icône du nouveau slot sans reconstruire toute la sidebar.
    public event System.Action<int, SpellSlot> OnSlotAdded;

    // Corrupted effects currently applied as permanent player debuffs/buffs —
    // tracked by CorruptedEffectSO so a shared asset referenced by several nodes
    // stays active as long as at least one of them is wired into a slot.
    private readonly HashSet<CorruptedEffectSO> _activePassiveEffects = new();

    private void Awake()
    {
        _spellSlots     ??= System.Array.Empty<SpellSlot>();
        _cooldownTimers   = new float[_spellSlots.Length];
        _relicManager     = GetComponent<RelicManager>();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        RebuildCraftingGraphFromSlots();
        if (craftingGraph == null) return;
        foreach (var node in craftingGraph.nodes)
            node?.RuntimeInit();
        RefreshPassiveCorruption();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnEnable() => SubscribeKeybinds();

    private void OnDisable() => UnsubscribeKeybinds();

    private void SubscribeKeybinds()
    {
        foreach (var slot in _spellSlots)
        {
            var config = slot?.launcherConfig;
            if (config == null || config.launcherType != LauncherType.KeyBind || config.inputAction == null) continue;
            config.inputAction.action.performed += TryCastKeybind;
            config.inputAction.action.Enable();
        }
    }

    private void UnsubscribeKeybinds()
    {
        foreach (var slot in _spellSlots)
        {
            var config = slot?.launcherConfig;
            if (config == null || config.launcherType != LauncherType.KeyBind || config.inputAction == null) continue;
            config.inputAction.action.performed -= TryCastKeybind;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _castingEnabled = true;
    }

    // Appeler depuis OnZoneCompleted du ZoneManager (via l'Inspector)
    public void DisableCasting() => _castingEnabled = false;

    private void Update()
    {
        if (!_castingEnabled) return;

        for (int i = 0; i < _spellSlots.Length; i++)
        {
            if (!IsSlotReady(i)) continue;
            _cooldownTimers[i] -= Time.deltaTime;

            if (_spellSlots[i].launcherConfig.launcherType == LauncherType.AutoCast
                && _cooldownTimers[i] <= 0f)
                CastSlot(i);
        }
    }

    // Called by input events wired in the Inspector (e.g. via PlayerInput component)
    public void TryCastKeybind(InputAction.CallbackContext context)
    {
        if (!_castingEnabled || !context.performed) return;

        for (int i = 0; i < _spellSlots.Length; i++)
        {
            var config = _spellSlots[i].launcherConfig;
            if (config?.launcherType != LauncherType.KeyBind) continue;
            if (config.inputAction?.action != context.action) continue;
            if (_cooldownTimers[i] > 0f) continue;
            CastSlot(i);
        }
    }

    // Called by other systems (kill feed, damage events, etc.)
    public void NotifyEvent(GameEventType eventType)
    {
        if (!_castingEnabled) return;
        for (int i = 0; i < _spellSlots.Length; i++)
        {
            var config = _spellSlots[i].launcherConfig;
            if (config?.launcherType != LauncherType.OnEvent) continue;
            if (config.eventType != eventType) continue;
            if (_cooldownTimers[i] > 0f) continue;
            CastSlot(i);
        }
    }

    private void CastSlot(int i)
    {
        var slot = _spellSlots[i];

        var ctx = new SpellContext
        {
            Caster         = gameObject,
            Origin         = _firePoint != null ? _firePoint.position : transform.position,
            Direction      = _firePoint != null ? _firePoint.forward  : transform.forward,
            CritChance     = Mathf.Clamp01(baseCritChance + (_relicManager?.CritChanceBonus ?? 0f)),
            CritMultiplier = baseCritMultiplier + (_relicManager?.CritMultiplierBonus ?? 0f),
        };
        ctx.Damage *= slot.launcherConfig.bonusMultiplier * (_relicManager?.DamageMultiplier ?? 1f);

        if (craftingGraph != null && craftingGraph.TryGetSlotEntry(i, out int startNode))
        {
            if (slot.launcherConfig.launcherType == LauncherType.AutoCast)
                Debug.Log($"[SpellCaster] AutoCast slot {i} → {BuildChainString(craftingGraph, startNode)}");
            SpellExecutor.Execute(craftingGraph, startNode, ctx);
        }
        else if (slot.connectedSpell != null)
        {
            if (slot.launcherConfig.launcherType == LauncherType.AutoCast)
                Debug.Log($"[SpellCaster] AutoCast slot {i} (legacy) → {BuildChainString(slot.connectedSpell, 0)}");
            SpellExecutor.Execute(slot.connectedSpell, 0, ctx);
        }

        // Set after Execute so a corrupted node's CooldownMultiplier (accumulated on ctx) applies
        _cooldownTimers[i] = slot.launcherConfig.cooldown * ctx.CooldownMultiplier * (_relicManager?.CooldownMultiplier ?? 1f);
    }

    private bool IsSlotReady(int i)
    {
        var slot = _spellSlots[i];
        if (slot?.launcherConfig == null) return false;
        if (craftingGraph != null && craftingGraph.nodes.Count > 0)
            return craftingGraph.HasSlotEntry(i);
        return slot.connectedSpell != null;
    }

    private static string BuildChainString(SpellGraphSO graph, int startIndex, int depth = 0)
    {
        if (graph == null || startIndex < 0 || startIndex >= graph.nodes.Count || depth > 10) return "…";
        var node = graph.nodes[startIndex];
        string name = node != null ? node.nodeName : "?";
        var outputs = graph.GetOutputIndices(startIndex);
        if (outputs.Count == 0) return name;
        return name + " → " + BuildChainString(graph, outputs[0], depth + 1);
    }

    // Called by NodePickup when the player walks over it
    public void CollectNode(SpellNodeSO node)
    {
        var canvas = GraphCanvasController.Instance;
        if (canvas != null && canvas.IsLoaded)
        {
            // Panel open — add through canvas so the node appears immediately and auto-applies
            canvas.AddNodeAtRandom(node);
            Debug.Log($"[SpellCaster] Collected '{node.nodeName}' — added to open graph");
            return;
        }

        // Panel closed — make sure base spell nodes are already in craftingGraph before appending
        EnsureCraftingGraphInitialized();

        craftingGraph.nodes.Add(node);
        OnCraftingGraphChanged?.Invoke();

        Debug.Log($"[SpellCaster] Collected '{node.nodeName}' — stored, will appear when panel opens");
    }

    // Rebuilds craftingGraph from scratch from the equipped slots every time a new
    // play session starts, so edits to base spell assets (Fireball, Iceball, ...)
    // are always reflected instead of being stuck behind a stale cached merge.
    private void RebuildCraftingGraphFromSlots()
    {
        if (craftingGraph == null)
            craftingGraph = ScriptableObject.CreateInstance<SpellGraphSO>();

        craftingGraph.nodes.Clear();
        craftingGraph.connections.Clear();
        craftingGraph.editorLayout.Clear();
        craftingGraph.slotEntries.Clear();

        MergeSlotsInto(craftingGraph);
        OnCraftingGraphChanged?.Invoke();
    }

    // Merges legacy per-slot spells into craftingGraph (only if not already done).
    // Used when a node pickup is collected while the crafting panel is closed —
    // guarded so it doesn't wipe nodes already added during the current session.
    private void EnsureCraftingGraphInitialized()
    {
        if (craftingGraph != null && craftingGraph.nodes.Count > 0) return;

        if (craftingGraph == null)
            craftingGraph = ScriptableObject.CreateInstance<SpellGraphSO>();

        MergeSlotsInto(craftingGraph);
    }

    private void MergeSlotsInto(SpellGraphSO graph)
    {
        for (int i = 0; i < _spellSlots.Length; i++)
            AppendSlotInto(graph, i, _spellSlots[i]?.connectedSpell);
    }

    // Fusionne le graphe par défaut d'UN slot à la SUITE du graphe existant (nodeOffset =
    // graph.nodes.Count courant) plutôt que de tout reconstruire depuis zéro — utilisé à la
    // fois par MergeSlotsInto (boucle sur tous les slots dans un graphe fraîchement vidé) et
    // par AddSlot (un seul slot ajouté à un graphe déjà en cours d'utilisation, dont le
    // câblage ne doit pas être perturbé, cf. commentaire sur AddSlot).
    private void AppendSlotInto(SpellGraphSO graph, int slotIndex, SpellGraphSO source)
    {
        if (source == null || source.nodes.Count == 0) return;

        int nodeOffset = graph.nodes.Count;
        graph.SetSlotEntry(slotIndex, nodeOffset);

        for (int j = 0; j < source.nodes.Count; j++)
        {
            graph.nodes.Add(source.nodes[j]);
            graph.editorLayout.Add(new SpellGraphSO.NodePlacement
            {
                nodeIndex      = nodeOffset + j,
                canvasPosition = new Vector2(-200f + j * 180f, 80f - slotIndex * 150f)
            });
        }

        foreach (var conn in source.connections)
        {
            // Guard against a stray connection in the source asset referencing an
            // index past its own node list — left uncaught, the offset below would
            // silently bridge it into whichever slot gets merged next.
            if (conn.fromIndex < 0 || conn.fromIndex >= source.nodes.Count ||
                conn.toIndex   < 0 || conn.toIndex   >= source.nodes.Count)
            {
                Debug.LogWarning($"[SpellCaster] '{source.name}' has an out-of-range connection " +
                                  $"{conn.fromIndex}->{conn.toIndex} (only {source.nodes.Count} nodes) — skipped.");
                continue;
            }

            graph.connections.Add(new SpellGraphSO.Connection
            {
                fromIndex = conn.fromIndex + nodeOffset,
                toIndex   = conn.toIndex   + nodeOffset
            });
        }
    }

    public void RefreshPermanentOrbitals()
    {
        if (craftingGraph == null) return;

        var activePrefabs = new HashSet<GameObject>();

        for (int i = 0; i < _spellSlots.Length; i++)
        {
            if (!craftingGraph.TryGetSlotEntry(i, out int startNode)) continue;

            var ctx = new SpellContext
            {
                Caster         = gameObject,
                Origin         = transform.position,
                Direction      = transform.forward,
                CritChance     = baseCritChance,
                CritMultiplier = baseCritMultiplier,
            };

            if (_spellSlots[i]?.launcherConfig != null)
                ctx.Damage *= _spellSlots[i].launcherConfig.bonusMultiplier;
            ctx.Damage *= _relicManager?.DamageMultiplier ?? 1f;

            var prefab = SpellExecutor.RefreshPermanentOrbitals(craftingGraph, startNode, ctx);
            if (prefab != null) activePrefabs.Add(prefab);
        }

        // A slot that lost its orbital node (disconnected/removed) no longer shows up above —
        // without this, OrbitalManager would keep orbiting it forever with no slot behind it.
        GetComponent<OrbitalManager>()?.PruneExcept(activePrefabs);
    }

    // Re-evaluates which corrupted nodes are currently reachable from an equipped
    // slot's entry point and (de)activates their permanent effects accordingly.
    // Call whenever the graph's wiring changes (slot connect/disconnect, node
    // add/remove) — SpellCraftingPanel.AutoApply does this alongside orbitals.
    public void RefreshPassiveCorruption()
    {
        if (craftingGraph == null) return;

        var connectedNodes = new HashSet<int>();
        for (int i = 0; i < _spellSlots.Length; i++)
            if (craftingGraph.TryGetSlotEntry(i, out int entryIdx))
                CollectReachableNodes(entryIdx, connectedNodes);

        var stillActive = new HashSet<CorruptedEffectSO>();
        foreach (var idx in connectedNodes)
        {
            var effect = craftingGraph.nodes[idx]?.corruptedEffect;
            if (effect == null) continue;

            stillActive.Add(effect);
            if (_activePassiveEffects.Add(effect))
                effect.ApplyPermanentTo(gameObject);
        }

        _activePassiveEffects.RemoveWhere(effect =>
        {
            if (stillActive.Contains(effect)) return false;
            effect.RemovePermanentFrom(gameObject);
            return true;
        });
    }

    private void CollectReachableNodes(int idx, HashSet<int> visited)
    {
        if (idx < 0 || craftingGraph == null || idx >= craftingGraph.nodes.Count) return;
        if (!visited.Add(idx)) return;
        foreach (var outIdx in craftingGraph.GetOutputIndices(idx))
            CollectReachableNodes(outIdx, visited);
    }

    // Normalized remaining cooldown for slot i: 1 right after cast, 0 once ready again.
    public float GetCooldownRatio(int i)
    {
        if (i < 0 || i >= _spellSlots.Length) return 0f;
        var config = _spellSlots[i]?.launcherConfig;
        if (config == null || config.cooldown <= 0f) return 0f;
        return Mathf.Clamp01(_cooldownTimers[i] / config.cooldown);
    }

    public SpellSlot   GetSlot(int i)  => (i >= 0 && i < _spellSlots.Length) ? _spellSlots[i] : null;
    public SpellSlot[] GetSlots()      => _spellSlots;

    // Appelé par RobotLoader (DefaultExecutionOrder -100) avant Awake() de ce
    // composant, pour que _cooldownTimers et les abonnements KeyBind d'OnEnable
    // se basent directement sur le loadout tiré au sort du robot choisi.
    public void SetSlots(SpellSlot[] slots) => _spellSlots = slots ?? System.Array.Empty<SpellSlot>();
    public void SetSlotGraph(int i, SpellGraphSO graph)
    {
        if (i >= 0 && i < _spellSlots.Length) _spellSlots[i].connectedSpell = graph;
    }

    // Appelé par un RelicEffect qui ajoute un nouveau sort au slotbar (contrairement à
    // CollectNode, qui n'ajoute qu'un node dans le graphe partagé) : grossit _spellSlots
    // d'un élément et rebranche tout ce qui dépend de sa taille. Pas de retrait — le
    // slot ajouté est de toute façon écrasé au prochain ResetForNewRun (nouveau loadout).
    public void AddSlot(SpellLauncherConfigSO config, SpellGraphSO spell)
    {
        if (config == null) return;

        int newIndex = _spellSlots.Length;
        var slots = new SpellSlot[newIndex + 1];
        _spellSlots.CopyTo(slots, 0);
        var newSlot = new SpellSlot { launcherConfig = config, connectedSpell = spell };
        slots[newIndex] = newSlot;
        _spellSlots = slots;

        var timers = new float[_cooldownTimers.Length + 1];
        _cooldownTimers.CopyTo(timers, 0);
        _cooldownTimers = timers;

        if (config.launcherType == LauncherType.KeyBind && config.inputAction != null)
        {
            config.inputAction.action.performed += TryCastKeybind;
            config.inputAction.action.Enable();
        }

        // Ajoute SEULEMENT ce nouveau slot au graphe existant — un rebuild complet
        // (RebuildCraftingGraphFromSlots) effacerait le câblage déjà en place, y compris
        // tout ce que le joueur a réorganisé dans le panel de craft (ex : rebrancher le
        // sort d'un slot autocast sur le keybind), et le ramènerait au mapping slot→sort
        // d'origine du robot.
        EnsureCraftingGraphInitialized();
        int nodesBefore = craftingGraph.nodes.Count;
        AppendSlotInto(craftingGraph, newIndex, spell);
        for (int i = nodesBefore; i < craftingGraph.nodes.Count; i++)
            craftingGraph.nodes[i]?.RuntimeInit();

        RefreshPassiveCorruption();
        OnCraftingGraphChanged?.Invoke();

        OnSlotAdded?.Invoke(newIndex, newSlot);
    }

    // Le Player survit au rechargement de scène (PlayerPersistence), donc SetSlots() seul
    // (pensé pour tourner avant Awake(), cf. commentaire ci-dessus) ne suffit plus une fois
    // le run précédent déjà démarré : il faut aussi retailler _cooldownTimers, réabonner les
    // KeyBind du nouveau loadout et reconstruire craftingGraph, sous peine de garder les
    // sorts (et leurs cooldowns) de l'archétype précédent au choix d'un nouveau robot.
    public void ResetForNewRun(SpellSlot[] slots, float critChance, float critMultiplier)
    {
        UnsubscribeKeybinds();

        _spellSlots         = slots ?? System.Array.Empty<SpellSlot>();
        _cooldownTimers      = new float[_spellSlots.Length];
        baseCritChance       = critChance;
        baseCritMultiplier   = critMultiplier;
        _castingEnabled      = true;

        SubscribeKeybinds();

        RebuildCraftingGraphFromSlots();
        foreach (var node in craftingGraph.nodes)
            node?.RuntimeInit();
        RefreshPassiveCorruption();
    }
}
