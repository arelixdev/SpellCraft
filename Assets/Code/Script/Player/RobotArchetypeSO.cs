using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Modèle d'un robot (Tank, Vif, Sniper...) : chaque stat est un intervalle plutôt qu'une
/// valeur fixe. Roll() tire une valeur aléatoire dans chaque intervalle et un nom aléatoire
/// pour produire un RobotDefinitionSO éphémère, prêt à être proposé au joueur.
/// </summary>
[CreateAssetMenu(fileName = "NewRobotArchetype", menuName = "SpellCraft/Robot Archetype")]
public class RobotArchetypeSO : ScriptableObject
{
    [LabelText("Nom de l'archétype")]
    public string ArchetypeName = "Archétype";

    [LabelText("Description"), TextArea(2, 4)]
    public string Description = "";

    [LabelText("Prefab visuel")]
    [Required] [AssetsOnly]
    public GameObject VisualPrefab;

    [Title("Intervalles de stats")]
    [LabelText("PV max"), MinMaxSlider(1f, 300f, true)]
    public Vector2 MaxHealthRange = new(90f, 110f);

    [LabelText("Vitesse de déplacement"), MinMaxSlider(0f, 15f, true)]
    public Vector2 MoveSpeedRange = new(4.5f, 5.5f);

    [LabelText("Chance de critique"), MinMaxSlider(0f, 1f, true)]
    public Vector2 CritChanceRange = new(0.08f, 0.12f);

    [LabelText("Multiplicateur de critique"), MinMaxSlider(1f, 5f, true)]
    public Vector2 CritMultiplierRange = new(1.4f, 1.6f);

    [LabelText("Or de départ"), MinMaxSlider(0f, 100f, true)]
    public Vector2 StartingGoldRange = new(0f, 0f);

    [Title("Slots de sort")]
    [LabelText("Réserve de LauncherConfig")]
    [Tooltip("Chaque robot tiré reçoit entre 1 et 4 slots, chacun assigné à une config piochée au hasard (sans doublon) dans cette réserve.")]
    public LauncherConfigPoolSO LauncherPool;

    public RobotDefinitionSO Roll(string rolledName)
    {
        var robot = ScriptableObject.CreateInstance<RobotDefinitionSO>();
        robot.DisplayName         = rolledName;
        robot.Description         = Description;
        robot.VisualPrefab        = VisualPrefab;
        robot.BaseMaxHealth       = Random.Range(MaxHealthRange.x, MaxHealthRange.y);
        robot.BaseMoveSpeed       = Random.Range(MoveSpeedRange.x, MoveSpeedRange.y);
        robot.BaseCritChance      = Random.Range(CritChanceRange.x, CritChanceRange.y);
        robot.BaseCritMultiplier  = Random.Range(CritMultiplierRange.x, CritMultiplierRange.y);
        robot.StartingGold        = Mathf.RoundToInt(Random.Range(StartingGoldRange.x, StartingGoldRange.y));
        robot.SpellSlots          = RollSlots();
        return robot;
    }

    private SpellSlot[] RollSlots()
    {
        if (LauncherPool == null) return System.Array.Empty<SpellSlot>();

        int slotCount = Random.Range(1, 5); // 1 à 4 inclus
        var configs = LauncherPool.DrawUnique(slotCount);

        var slots = new SpellSlot[configs.Count];
        for (int i = 0; i < configs.Count; i++)
            slots[i] = new SpellSlot { launcherConfig = configs[i] };

        return slots;
    }
}
