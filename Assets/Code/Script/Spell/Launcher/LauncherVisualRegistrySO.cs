using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(menuName = "Spell/Launcher Visual Registry")]
public class LauncherVisualRegistrySO : ScriptableObject
{
    [ListDrawerSettings(ShowIndexLabels = false, DraggableItems = false)]
    public List<LauncherTypeVisual> visuals = new();

    public bool TryGet(LauncherType type, out LauncherTypeVisual result)
    {
        foreach (var v in visuals)
        {
            if (v.type == type) { result = v; return true; }
        }
        result = null;
        return false;
    }

    public Sprite GetOutline(LauncherType type)    => TryGet(type, out var v) ? v.outline    : null;
    public Sprite GetBackground(LauncherType type) => TryGet(type, out var v) ? v.background : null;

#if UNITY_EDITOR
    [Button("Auto-fill missing entries"), PropertySpace(8)]
    private void AutoFill()
    {
        foreach (LauncherType t in System.Enum.GetValues(typeof(LauncherType)))
        {
            bool exists = false;
            foreach (var v in visuals) if (v.type == t) { exists = true; break; }
            if (!exists) visuals.Add(new LauncherTypeVisual { type = t });
        }
    }
#endif
}
