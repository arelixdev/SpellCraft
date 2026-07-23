using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Localization;

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

    [LabelText("Description")]
    public LocalizedString Description;

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
    [LabelText("Pondération du nombre de slots")]
    [Tooltip("Nombre de slots dans la barre (1 à 4), tiré selon ces poids.")]
    public WeightedCount0to4 SlotCountWeights = new();

    [LabelText("Pondération du nombre de slots remplis")]
    [Tooltip("Parmi les slots tirés, combien reçoivent d'office un sort de départ (le reste reste vide, à équiper via le crafting). Weight0 > 0 autorise \"aucun slot rempli\". Plafonné au nombre de slots réel.")]
    public WeightedCount0to4 FilledSlotWeights = new();

    [LabelText("Réserve de LauncherConfig")]
    [Tooltip("Chaque slot est assigné à une config piochée au hasard (sans doublon) dans cette réserve.")]
    public LauncherConfigPoolSO LauncherPool;

    [LabelText("Réserve de sorts de départ")]
    [Tooltip("Sort de départ (connectedSpell) pioché par palier pondéré dans cette réserve, pour les slots \"remplis\".")]
    public SpellGraphPoolSO SpellGraphPool;

    public RobotDefinitionSO Roll(string rolledName)
    {
        var robot = ScriptableObject.CreateInstance<RobotDefinitionSO>();
        robot.DisplayName         = rolledName;
        robot.Description         = Description;
        robot.VisualRecipe        = RobotVisualGenerator.RollRandomRecipe();
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

        int slotCount = SlotCountWeights.Roll();
        var configs = LauncherPool.DrawForSlotCount(slotCount);

        var slots = new SpellSlot[configs.Count];
        for (int i = 0; i < configs.Count; i++)
            slots[i] = new SpellSlot { launcherConfig = configs[i] };

        // Parmi les slots tirés, seule une partie (pondérée) reçoit d'office un sort —
        // toujours en partant du premier slot (préfixe 0, puis 0+1, puis 0+1+2...),
        // jamais un slot au hasard au milieu : même principe que l'ordre des touches.
        int filledCount = Mathf.Min(FilledSlotWeights.Roll(), slots.Length);
        for (int i = 0; i < filledCount; i++)
            slots[i].connectedSpell = SpellGraphPool != null ? SpellGraphPool.DrawOne() : null;

        return slots;
    }
}
