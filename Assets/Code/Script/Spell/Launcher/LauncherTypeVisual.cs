using System;
using UnityEngine;
using Sirenix.OdinInspector;

[Serializable]
public class LauncherTypeVisual
{
    [HorizontalGroup("Row", Width = 120), EnumToggleButtons, HideLabel]
    public LauncherType type;

    [HorizontalGroup("Row")]
    [PreviewField(48, ObjectFieldAlignment.Left), LabelWidth(64)]
    public Sprite outline;

    [HorizontalGroup("Row")]
    [PreviewField(48, ObjectFieldAlignment.Left), LabelWidth(80)]
    public Sprite background;

    [LabelWidth(100), MinValue(0f)]
    public float outlineSize = 100f;

    [LabelWidth(100)]
    public Vector2 outlineOffset = Vector2.zero;
}
