using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Décrit un type d'ennemi (stats de base, scaling par niveau, comportement).
/// Instancié par EnemyGenericController.Initialize() sur un prefab générique au spawn.
/// </summary>
[CreateAssetMenu(fileName = "NewEnemy", menuName = "SpellCraft/Enemy Definition")]
public class EnemyDefinitionSO : ScriptableObject
{
    public enum BehaviorType { Chaser, RangedCaster }

    [LabelText("Nom affiché")]
    public string DisplayName = "Ennemi";

    [LabelText("Prefab à instancier")]
    [Required] [AssetsOnly]
    public GameObject Prefab;

    [LabelText("Comportement")]
    public BehaviorType Behavior = BehaviorType.Chaser;

    [Title("Stats de base")]
    [LabelText("PV de base")]
    public float BaseHealth = 25f;

    [LabelText("Scaling PV par niveau")]
    [Tooltip("Multiplicateur exponentiel appliqué par niveau : PV = BaseHealth * scale^(niveau-1). N'a d'effet qu'avec un niveau > 1.")]
    public float HealthScalePerLevel = 1f;

    [LabelText("Dégâts de base")]
    public float BaseDamage = 10f;

    [LabelText("Scaling dégâts par niveau")]
    [Tooltip("Multiplicateur exponentiel appliqué par niveau : Dégâts = BaseDamage * scale^(niveau-1). N'a d'effet qu'avec un niveau > 1.")]
    public float DamageScalePerLevel = 1f;

    [LabelText("Vitesse de déplacement")]
    public float BaseMoveSpeed = 3.5f;

    [Title("Comportement")]
    [LabelText("Portée de détection")]
    public float DetectionRange = 15f;

    [LabelText("Portée d'attaque")]
    public float AttackRange = 1.8f;

    [LabelText("Cadence d'attaque (par seconde)")]
    public float AttackRate = 1f;

    [LabelText("Distance d'arrêt"), ShowIf("Behavior", BehaviorType.Chaser)]
    public float StopDistance = 1.5f;

    [LabelText("Distance préférée"), ShowIf("Behavior", BehaviorType.RangedCaster)]
    [Tooltip("Distance que l'ennemi essaie de maintenir avec le joueur (kiting)")]
    public float PreferredRange = 8f;

    [LabelText("Prefab projectile"), ShowIf("Behavior", BehaviorType.RangedCaster)]
    [AssetsOnly]
    public GameObject ProjectilePrefab;

    [LabelText("Vitesse projectile"), ShowIf("Behavior", BehaviorType.RangedCaster)]
    public float ProjectileSpeed = 12f;
}
