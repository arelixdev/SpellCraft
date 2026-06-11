using UnityEngine;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;

[CreateAssetMenu(menuName = "Spell/LauncherConfig")]
public class SpellLauncherConfigSO : ScriptableObject
{
    [BoxGroup("Identity"), LabelWidth(140)]
    public string launcherName;

    [BoxGroup("Identity")]
    [EnumToggleButtons, HideLabel]
    public LauncherType launcherType;

    [BoxGroup("Settings"), LabelWidth(140)]
    [ShowIf("launcherType", LauncherType.AutoCast)]
    [MinValue(0.1f)] public float cooldown = 1f;

    [BoxGroup("Settings"), LabelWidth(140)]
    [ShowIf("launcherType", LauncherType.KeyBind)]
    public InputActionReference inputAction;

    [BoxGroup("Settings"), LabelWidth(140)]
    [ShowIf("launcherType", LauncherType.OnEvent)]
    public GameEventType eventType;

    [BoxGroup("Settings"), LabelWidth(140)]
    [Range(0.5f, 3f)] public float bonusMultiplier = 1f;
}
