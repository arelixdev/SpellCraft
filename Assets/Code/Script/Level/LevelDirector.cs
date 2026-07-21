using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Décide, au chargement de la scène Gameplay, quel niveau (décor + POI) est actif selon
/// RunProgress.Level : pioche un LevelDefinitionSO sans remise dans le pool tournant pour
/// les stages réguliers (niveau 1..LevelsBeforeBigBoss), puis bascule sur BigBossLevel /
/// BigBossPrefab / BigBossEscortMobs une fois ce nombre de niveaux dépassé.
/// Charge la scène de décor du niveau (EnvironmentSceneName) en additif par-dessus Gameplay.
/// Le rechargement de Gameplay en mode Single à chaque changement de niveau (BossPortal)
/// décharge automatiquement l'ancien décor — pas besoin de le décharger explicitement ici.
/// Doit tourner avant ActivityManager.Start() (Awake — garanti antérieur à tout Start()
/// de la scène) pour que le pool d'activités soit en place avant le placement des POI,
/// et le décor (NavMesh compris) déjà chargé pour le placement sur le NavMesh.
/// </summary>
public class LevelDirector : MonoBehaviour
{
    [BoxGroup("Niveaux réguliers"), LabelText("Pool de niveaux")]
    public LevelPoolSO RegularLevels;

    [BoxGroup("Niveaux réguliers"), MinValue(1), LabelText("Nombre de niveaux avant le Big Boss")]
    public int LevelsBeforeBigBoss = 3;

    [BoxGroup("Big Boss"), LabelText("Niveau du Big Boss")]
    [Tooltip("Décor + POI du stage final. Laisser vide pour garder les POI déjà en place dans la scène.")]
    public LevelDefinitionSO BigBossLevel;

    [BoxGroup("Big Boss"), LabelText("Prefab du Big Boss")]
    [Tooltip("Laisser vide pour garder le boss déjà assigné sur le Boss Portal en attendant d'avoir ce contenu.")]
    public GameObject BigBossPrefab;

    [BoxGroup("Big Boss"), LabelText("Mobs d'escorte du Big Boss")]
    public System.Collections.Generic.List<MobSpawnEntry> BigBossEscortMobs = new();

    [BoxGroup("Références"), Required, LabelText("Activity Manager")]
    public ActivityManager ActivityManager;

    [BoxGroup("Références"), Required, LabelText("Boss Portal")]
    public BossPortal BossPortal;

    [FoldoutGroup("État"), ShowInInspector, ReadOnly, LabelText("Niveau actif")]
    public string CurrentLevelName { get; private set; } = "(scène telle quelle)";

    private void Awake()
    {
        if (RunProgress.Level > LevelsBeforeBigBoss)
            SetupBigBoss();
        else
            SetupRegularLevel();
    }

    private void SetupRegularLevel()
    {
        if (BossPortal != null)
        {
            BossPortal.gameObject.SetActive(true);
            BossPortal.IsFinalBoss = false;
            BossPortal.EscortMobs.Clear();
        }

        var level = PickUnusedLevel();
        if (level == null)
        {
            Debug.LogWarning("[LevelDirector] Aucun niveau disponible dans le pool régulier — POI de la scène conservés tels quels.");
            return;
        }

        RunProgress.UsedLevels.Add(level);
        ApplyLevel(level);
    }

    // Pas de portail pour le Big Boss : il spawne directement au centre du terrain
    // (position de l'ActivityManager) dès le chargement du niveau, sans interaction du joueur.
    private void SetupBigBoss()
    {
        if (BigBossLevel != null)
            ApplyLevel(BigBossLevel);

        if (BossPortal != null)
        {
            if (BigBossPrefab != null) BossPortal.BossPrefab = BigBossPrefab;
            BossPortal.EscortMobs = BigBossEscortMobs;
            BossPortal.IsFinalBoss = true;

            Vector3 arenaCenter = ActivityManager != null ? ActivityManager.transform.position : transform.position;
            BossPortal.ForceSpawnBossAt(arenaCenter);
            BossPortal.gameObject.SetActive(false);
        }
    }

    // Pioche sans remise dans le pool ; si tous les niveaux du pool ont déjà été vus cette
    // run (pool plus petit que LevelsBeforeBigBoss), retombe sur le pool complet plutôt que
    // de ne rien tirer.
    private LevelDefinitionSO PickUnusedLevel()
    {
        if (RegularLevels == null || RegularLevels.Levels.Count == 0) return null;

        var available = RegularLevels.Levels
            .Where(l => l != null && !RunProgress.UsedLevels.Contains(l))
            .ToList();

        if (available.Count == 0)
            available = RegularLevels.Levels.Where(l => l != null).ToList();

        return available.Count > 0 ? available[Random.Range(0, available.Count)] : null;
    }

    private void ApplyLevel(LevelDefinitionSO level)
    {
        CurrentLevelName = level.DisplayName;

        if (ActivityManager != null)
            ActivityManager.ActivityPool = level.ActivityPool;

        if (!string.IsNullOrEmpty(level.EnvironmentSceneName))
            SceneManager.LoadScene(level.EnvironmentSceneName, LoadSceneMode.Additive);
        else
            Debug.LogWarning($"[LevelDirector] '{level.DisplayName}' n'a pas de scène de décor assignée — le niveau se chargera sans terrain/NavMesh.");

        Debug.Log($"[LevelDirector] Niveau '{level.DisplayName}' appliqué (stage {RunProgress.Level}).");
    }
}
