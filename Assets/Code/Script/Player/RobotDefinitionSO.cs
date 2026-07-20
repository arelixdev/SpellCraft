using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Localization;

/// <summary>
/// Décrit un robot jouable (apparence + stats de départ). Choisi sur l'écran de
/// sélection (RobotSelection.Chosen) et appliqué au Player par RobotLoader au spawn.
/// </summary>
[CreateAssetMenu(fileName = "NewRobot", menuName = "SpellCraft/Robot Definition")]
public class RobotDefinitionSO : ScriptableObject
{
    [LabelText("Nom affiché")]
    public string DisplayName = "Robot";

    [LabelText("Description")]
    public LocalizedString Description;

    [LabelText("Prefab visuel")]
    [Required] [AssetsOnly]
    public GameObject VisualPrefab;

    [Title("Stats de départ")]
    [LabelText("PV max")]
    public float BaseMaxHealth = 100f;

    [LabelText("Vitesse de déplacement")]
    public float BaseMoveSpeed = 5f;

    [LabelText("Chance de critique")]
    [Range(0f, 1f)]
    public float BaseCritChance = 0.1f;

    [LabelText("Multiplicateur de critique")]
    [Range(1f, 5f)]
    public float BaseCritMultiplier = 1.5f;

    [LabelText("Or de départ")]
    public int StartingGold = 0;

    [Title("Slots de sort")]
    [LabelText("Slots (tirés par l'archétype)")]
    public SpellSlot[] SpellSlots = System.Array.Empty<SpellSlot>();
}
