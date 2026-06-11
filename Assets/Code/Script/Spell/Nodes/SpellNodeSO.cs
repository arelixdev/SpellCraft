using UnityEngine;
using Sirenix.OdinInspector;

public abstract class SpellNodeSO : ScriptableObject
{
    [HorizontalGroup("Header", Width = 58)]
    [PreviewField(55, ObjectFieldAlignment.Left), HideLabel]
    public Sprite icon;

    [VerticalGroup("Header/Info"), LabelWidth(130)]
    public string nodeName;

    [VerticalGroup("Header/Info"), LabelWidth(130)]
    [ReadOnly] public NodeType nodeType;

    [VerticalGroup("Header/Info"), LabelWidth(130)]
    [MinValue(1)] public int complexityCost = 1;

    public abstract void Execute(SpellContext ctx);
}
