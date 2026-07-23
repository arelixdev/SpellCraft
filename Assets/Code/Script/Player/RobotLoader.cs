using UnityEngine;

/// <summary>
/// Applique le RobotDefinitionSO choisi (RobotSelection.Chosen, ou un robot tiré de
/// _defaultArchetype en fallback pour tester la scène Gameplay directement) aux stats
/// de départ du Player et instancie son apparence. DefaultExecutionOrder(-100) garantit
/// que ça tourne avant les Awake() de PlayerHealth/PlayerController/PlayerWallet, qui
/// capturent leurs valeurs de départ (_currentHealth, _gold) dans leur propre Awake().
/// </summary>
[DefaultExecutionOrder(-100)]
public class RobotLoader : MonoBehaviour
{
    [SerializeField] private RobotArchetypeSO _defaultArchetype;
    [SerializeField] private RobotNamePoolSO  _defaultNamePool;

    private GameObject _visualInstance;

    private void Awake()
    {
        var robot = RobotSelection.Chosen;
        if (robot == null && _defaultArchetype != null)
        {
            var rolledName = _defaultNamePool != null ? _defaultNamePool.GetRandom() : _defaultArchetype.ArchetypeName;
            robot = _defaultArchetype.Roll(rolledName);
        }

        if (robot == null)
        {
            Debug.LogWarning("[RobotLoader] Aucun robot choisi et aucun archétype par défaut assigné.");
            return;
        }

        ApplyStats(robot, liveReset: false);
        ApplyVisual(robot);
    }

    // Le Player survit au rechargement de scène (PlayerPersistence) : cet Awake() ne se
    // relance donc pas quand on choisit un nouveau robot pour un nouveau run — sans cet
    // appel explicite (CharacterSelectController), le Player garderait les sorts/stats de
    // l'archétype précédent. liveReset=true route les sorts via SpellCaster.ResetForNewRun
    // au lieu du simple SetSlots (pensé pour tourner avant le tout premier Awake).
    public void ApplyRobotForNewRun(RobotDefinitionSO robot)
    {
        if (robot == null) return;

        ApplyStats(robot, liveReset: true);
        ApplyVisual(robot);
    }

    private void ApplyStats(RobotDefinitionSO robot, bool liveReset)
    {
        GetComponent<PlayerHealth>()?.SetMaxHealth(robot.BaseMaxHealth);
        GetComponent<PlayerController>()?.SetBaseMoveSpeed(robot.BaseMoveSpeed);
        GetComponent<PlayerWallet>()?.SetStartingGold(robot.StartingGold);

        var spellCaster = GetComponent<SpellCaster>();
        if (spellCaster == null) return;

        if (liveReset)
        {
            spellCaster.ResetForNewRun(robot.SpellSlots, robot.BaseCritChance, robot.BaseCritMultiplier);
        }
        else
        {
            spellCaster.baseCritChance     = robot.BaseCritChance;
            spellCaster.baseCritMultiplier = robot.BaseCritMultiplier;
            spellCaster.SetSlots(robot.SpellSlots);
        }
    }

    private void ApplyVisual(RobotDefinitionSO robot)
    {
        var placeholderRenderer = GetComponent<MeshRenderer>();
        if (placeholderRenderer != null)
            placeholderRenderer.enabled = false;

        if (_visualInstance != null)
        {
            Destroy(_visualInstance);
            _visualInstance = null;
        }

        _visualInstance = RobotVisualGenerator.BuildVisual(robot.VisualRecipe);
        if (_visualInstance != null)
        {
            _visualInstance.transform.SetParent(transform, false);
            // Le CharacterController a un centre (0,0,0) et une hauteur de 2 → ses pieds
            // sont à Y=-1 relatif au Player. Le mesh (pivot à la hanche) doit être abaissé
            // d'autant pour que les pieds touchent le sol au lieu de flotter.
            _visualInstance.transform.localPosition = new Vector3(0f, -1f, 0f);
            _visualInstance.transform.localRotation = Quaternion.identity;
        }
    }
}
