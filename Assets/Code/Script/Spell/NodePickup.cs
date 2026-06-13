using UnityEngine;
using TMPro;

/// World object that holds a random SpellNodeSO.
/// When the player enters its trigger collider the node is added to their crafting graph.
public class NodePickup : MonoBehaviour
{
    [Header("Config")]
    public SpellNodeCatalogSO Catalog;

    [Header("Visual (optional)")]
    public TMP_Text NodeLabel;
    public Renderer NodeColorRenderer; // set its material color to match the node type

    private SpellNodeSO _pickedNode;

    private void Awake()
    {
        if (Catalog == null || Catalog.allNodes.Count == 0)
        {
            Debug.LogWarning($"[NodePickup] '{name}' has no catalog or catalog is empty.");
            return;
        }

        _pickedNode = Catalog.allNodes[Random.Range(0, Catalog.allNodes.Count)];

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
