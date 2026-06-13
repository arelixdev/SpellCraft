using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(menuName = "Spell/SpellGraph")]
public class SpellGraphSO : ScriptableObject
{
    [Serializable]
    public struct Connection
    {
        [HorizontalGroup, LabelWidth(80)] public int fromIndex;
        [HorizontalGroup, LabelWidth(80)] public int toIndex;
    }

    [Serializable]
    public struct NodePlacement
    {
        public int     nodeIndex;
        public Vector2 canvasPosition;
    }

    [Serializable]
    public struct SlotEntry
    {
        public int slotIndex;
        public int nodeIndex;
    }

    [BoxGroup("Budget"), LabelWidth(150), MinValue(1)]
    public int complexityBudget = 10;

    [ShowInInspector, BoxGroup("Budget"), ReadOnly, LabelWidth(150)]
    [ProgressBar(0, "complexityBudget", ColorMember = "BudgetBarColor", DrawValueLabel = true)]
    public int TotalCost => nodes?.Sum(n => n?.complexityCost ?? 0) ?? 0;

    [ShowInInspector, BoxGroup("Budget"), ReadOnly, LabelWidth(150)]
    public bool IsValid => TotalCost <= complexityBudget;

    private Color BudgetBarColor => TotalCost <= complexityBudget ? Color.green : Color.red;

    // Slot entry points: each entry maps a slot index to a node index in this graph
    [HideInInspector]
    public List<SlotEntry> slotEntries = new();

    [HideInInspector]
    public List<NodePlacement> editorLayout = new();

    public bool HasSlotEntry(int slotIndex) =>
        slotEntries.Exists(e => e.slotIndex == slotIndex);

    public bool TryGetSlotEntry(int slotIndex, out int nodeIndex)
    {
        for (int i = 0; i < slotEntries.Count; i++)
            if (slotEntries[i].slotIndex == slotIndex) { nodeIndex = slotEntries[i].nodeIndex; return true; }
        nodeIndex = -1;
        return false;
    }

    public void SetSlotEntry(int slotIndex, int nodeIndex)
    {
        for (int i = 0; i < slotEntries.Count; i++)
            if (slotEntries[i].slotIndex == slotIndex) { slotEntries[i] = new SlotEntry { slotIndex = slotIndex, nodeIndex = nodeIndex }; return; }
        slotEntries.Add(new SlotEntry { slotIndex = slotIndex, nodeIndex = nodeIndex });
    }

    public void RemoveSlotEntry(int slotIndex) =>
        slotEntries.RemoveAll(e => e.slotIndex == slotIndex);

    [Title("Nodes")]
    [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true)]
    public List<SpellNodeSO> nodes = new();

    [Title("Connections")]
    [TableList]
    public List<Connection> connections = new();

    public List<int> GetOutputIndices(int nodeIndex)
    {
        return connections
            .Where(c => c.fromIndex == nodeIndex)
            .Select(c => c.toIndex)
            .ToList();
    }

    [Button("Validate Graph"), GUIColor(0.4f, 0.8f, 0.4f)]
    private void ValidateGraph()
    {
        if (IsValid)
            Debug.Log($"[SpellGraph] '{name}' valide — coût : {TotalCost}/{complexityBudget}");
        else
            Debug.LogWarning($"[SpellGraph] '{name}' dépasse le budget ! Coût : {TotalCost}/{complexityBudget}");
    }
}
