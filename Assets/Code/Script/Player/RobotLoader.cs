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

        ApplyStats(robot);
        ApplyVisual(robot);
    }

    private void ApplyStats(RobotDefinitionSO robot)
    {
        GetComponent<PlayerHealth>()?.SetMaxHealth(robot.BaseMaxHealth);
        GetComponent<PlayerController>()?.SetBaseMoveSpeed(robot.BaseMoveSpeed);
        GetComponent<PlayerWallet>()?.SetStartingGold(robot.StartingGold);

        var spellCaster = GetComponent<SpellCaster>();
        if (spellCaster != null)
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

        if (robot.VisualPrefab != null)
        {
            var visual = Instantiate(robot.VisualPrefab, transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
        }
    }
}
