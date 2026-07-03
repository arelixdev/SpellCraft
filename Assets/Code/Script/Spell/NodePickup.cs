using UnityEngine;
using TMPro;

/// World object that holds a random SpellNodeSO.
/// When the player enters its trigger collider the node is added to their crafting graph.
public class NodePickup : MonoBehaviour
{
    [Header("Config")]
    public LootPoolSO LootPool;

    [Header("Visual (optional)")]
    public TMP_Text NodeLabel;
    public Renderer NodeColorRenderer; // set its material color to match the node type

    private SpellNodeSO _pickedNode;

    private void Awake() => Configure(LootPool);

    // Permet à un spawner externe (ex: BossPortal) d'imposer une pool différente de celle sérialisée sur le prefab.
    public void Initialize(LootPoolSO pool) => Configure(pool);

    private void Configure(LootPoolSO pool)
    {
        if (pool == null)
        {
            Debug.LogWarning($"[NodePickup] '{name}' has no LootPool assigned.");
            return;
        }

        var playerGraph = GameObject.FindWithTag("Player")?.GetComponent<SpellCaster>()?.craftingGraph;
        _pickedNode = pool.DrawOneWithBias(playerGraph);

        if (_pickedNode == null)
        {
            Debug.LogWarning($"[NodePickup] '{name}' : LootPool '{pool.name}' n'a retourné aucune node.");
            return;
        }

        if (NodeLabel != null)
            NodeLabel.text = _pickedNode.nodeName;

        if (NodeColorRenderer != null)
            NodeColorRenderer.material.color = NodeView.ColorForType(_pickedNode.nodeType);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_pickedNode == null) return;

        // Accept any collider that has or is parented to a SpellCaster
        var caster = other.GetComponent<SpellCaster>()
                  ?? other.GetComponentInParent<SpellCaster>();
        if (caster == null) return;

        caster.CollectNode(_pickedNode);
        Destroy(gameObject);
    }
}
