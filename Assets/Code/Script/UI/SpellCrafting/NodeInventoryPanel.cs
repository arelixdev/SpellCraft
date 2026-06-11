using UnityEngine;

/// Populates the scrollable left panel with one NodeCardView per node in the catalog.
public class NodeInventoryPanel : MonoBehaviour
{
    public GameObject      NodeCardPrefab;
    public Transform       ContentRoot;
    public SpellNodeCatalogSO Catalog;

    public void Populate()
    {
        foreach (Transform child in ContentRoot) Destroy(child.gameObject);
        if (Catalog == null) return;
        foreach (var node in Catalog.allNodes)
        {
            if (node == null) continue;
            var go   = Instantiate(NodeCardPrefab, ContentRoot);
            go.SetActive(true);
            var card = go.GetComponent<NodeCardView>();
            card.Init(node);
        }
    }
}
