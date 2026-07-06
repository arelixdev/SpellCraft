using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "NodeRarityPalette", menuName = "Spell/Node Rarity Palette")]
public class NodeRarityPaletteSO : ScriptableObject
{
    [ListDrawerSettings(ShowIndexLabels = false, DraggableItems = false)]
    public List<NodeRarityColor> colors = new();

    public bool TryGetColor(NodeRarity rarity, out Color color)
    {
        foreach (var c in colors)
        {
            if (c.rarity == rarity) { color = c.color; return true; }
        }
        color = Color.gray;
        return false;
    }

#if UNITY_EDITOR
    [Button("Auto-fill missing entries"), PropertySpace(8)]
    private void AutoFill()
    {
        foreach (NodeRarity r in System.Enum.GetValues(typeof(NodeRarity)))
        {
            bool exists = false;
            foreach (var c in colors) if (c.rarity == r) { exists = true; break; }
            if (!exists) colors.Add(new NodeRarityColor { rarity = r, color = NodeView.ColorForRarity(r) });
        }
    }
#endif
}
