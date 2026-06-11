using System;
using Sirenix.OdinInspector;

[Serializable]
public class SpellSlot
{
    [HorizontalGroup(LabelWidth = 10), Required, HideLabel]
    public SpellLauncherConfigSO launcherConfig;

    [HorizontalGroup, HideLabel]
    public SpellGraphSO connectedSpell;
}
