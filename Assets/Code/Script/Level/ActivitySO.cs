using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Définit un type d'activité disponible dans la zone.
/// Créer via : clic droit → SpellCraft/Activity Definition
/// </summary>
[CreateAssetMenu(fileName = "NewActivity", menuName = "SpellCraft/Activity Definition")]
public class ActivitySO : ScriptableObject
{
    [LabelText("Nom affiché")]
    public string DisplayName = "Activité";

    [LabelText("Icône")]
    [PreviewField(64)]
    public Sprite Icon;

    [LabelText("Prefab à instancier")]
    [Required]
    [AssetsOnly]
    public GameObject Prefab;
}
