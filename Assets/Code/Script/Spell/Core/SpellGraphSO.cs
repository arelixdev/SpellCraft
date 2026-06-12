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

    [BoxGroup("Budget"), LabelWidth(150), MinValue(1)]
    public int complexityBudget = 10;

    [ShowInInspector, BoxGroup("Budget"), ReadOnly, LabelWidth(150)]
    [ProgressBar(0, "complexityBudget", ColorMember = "BudgetBarColor", DrawValueLabel = true)]
    public int TotalCost => nodes?.Sum(n => n?.complexityCost ?? 0) ?? 0;

    [ShowInInspector, BoxGroup("Budget"), ReadOnly, LabelWidth(150)]
    public bool IsValid => TotalCost <= complexityBudget;

    private Color BudgetBarColor => TotalCost <= complexityBudget ? Color.green : Color.red;

    [HideInInspector]
    public bool launcherConnected;

    [HideInInspector]
    public List<NodePlacement> editorLayout = new();

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
