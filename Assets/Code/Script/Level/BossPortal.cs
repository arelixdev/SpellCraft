using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>
/// Portail boss : le joueur entre dans le trigger, appuie sur Espace → le boss spawne.
/// Plus le joueur attend, plus le boss est puissant.
/// SetDangerLevel est appelé chaque seconde par RunController.
/// Une fois le boss vaincu, le portail reste utilisable : un second appui sur Espace
/// (le joueur choisit son moment, ex. après avoir fini de looter la zone) passe au
/// niveau suivant (rechargement de la scène avec un niveau incrémenté).
/// Au stage du Big Boss, LevelDirector désactive complètement ce portail (ForceSpawnBossAt
/// fait spawner le boss directement au centre du terrain, sans interaction du joueur) et
/// IsFinalBoss fait gagner la partie (RunController.WinRun()) dès que ce boss meurt.
/// </summary>
[System.Serializable]
public class MobSpawnEntry
{
    [LabelText("Définition"), HorizontalGroup("Row")]
    public EnemyDefinitionSO Definition;

    [LabelText("Nombre"), MinValue(1), HorizontalGroup("Row")]
    public int Count = 1;
}

public class BossPortal : ActivityBase
{
    // ── Boss ─────────────────────────────────────────────────────────────────────
    [BoxGroup("Boss"), Required, LabelText("Prefab Boss")]
    public GameObject BossPrefab;

    [BoxGroup("Boss"), LabelText("Point de spawn Boss")]
    [Tooltip("Si vide, le boss spawne devant le portail")]
    public Transform BossSpawnPoint;

    [BoxGroup("Boss"), LabelText("Prefab Node Pickup")]
    [Tooltip("Instancié à la mort du boss")]
    public GameObject NodePickupPrefab;

    [BoxGroup("Boss"), LabelText("Loot Pool")]
    public LootPoolSO LootPool;

    [BoxGroup("Boss"), LabelText("Boss de fin")]
    [Tooltip("Coché : vaincre ce boss termine la partie (RunController.WinRun()) au lieu de passer au niveau suivant. Reconfiguré par LevelDirector au stage du Big Boss.")]
    public bool IsFinalBoss;

    [BoxGroup("Boss"), LabelText("Mobs d'escorte")]
    [Tooltip("Spawnés une fois autour du boss dès son apparition (ex. les mobs du Big Boss)")]
    public List<MobSpawnEntry> EscortMobs = new();

    // ── Placement NavMesh ────────────────────────────────────────────────────────
    [BoxGroup("Placement"), LabelText("Rayon de recherche (m)")]
    [Tooltip("Distance max autour du centre de la zone pour chercher une position")]
    public float SearchRadius = 60f;

    [BoxGroup("Placement"), LabelText("Distance min des activités (m)")]
    [Tooltip("Le portail ne peut pas s'installer dans ce rayon autour d'une activité")]
    public float MinDistanceFromActivity = 12f;

    [BoxGroup("Placement"), LabelText("Tolérance NavMesh (m)")]
    public float NavMeshSampleDistance = 3f;

    [BoxGroup("Placement"), LabelText("Tentatives max")]
    public int MaxPlacementAttempts = 60;

    // ── Scaling ──────────────────────────────────────────────────────────────────
    [BoxGroup("Scaling"), LabelText("Multiplicateur HP (x=secondes, y=×HP)")]
    [Tooltip("Contrôle comment les HP du boss progressent dans le temps")]
    public AnimationCurve BossHpCurve = new AnimationCurve(
        new Keyframe(0f,   1f),
        new Keyframe(120f, 2f),
        new Keyframe(300f, 4f));

    [BoxGroup("Scaling"), LabelText("Multiplicateur DPS (x=secondes, y=×dégâts)")]
    public AnimationCurve BossDamageCurve = new AnimationCurve(
        new Keyframe(0f,   1f),
        new Keyframe(120f, 1.5f),
        new Keyframe(300f, 2.5f));

    // ── État ─────────────────────────────────────────────────────────────────────
    [FoldoutGroup("État"), ShowInInspector, ReadOnly, LabelText("Danger actuel (s)")]
    private float _currentDanger;

    [FoldoutGroup("État"), ShowInInspector, ReadOnly, LabelText("HP actuels du boss (×)")]
    private float CurrentHpMult => BossHpCurve.Evaluate(_currentDanger);

    [FoldoutGroup("État"), ShowInInspector, ReadOnly, LabelText("DPS actuels du boss (×)")]
    private float CurrentDmgMult => BossDamageCurve.Evaluate(_currentDanger);

    // ── API ──────────────────────────────────────────────────────────────────────
    public void SetDangerLevel(float dangerSeconds) => _currentDanger = dangerSeconds;

    // Spawn immédiat, sans passer par le trigger/l'interaction — utilisé par LevelDirector
    // pour faire apparaître le Big Boss au centre du terrain dès le chargement du niveau.
    public void ForceSpawnBossAt(Vector3 position)
    {
        if (_bossSpawned) return;
        _bossSpawned = true;
        SpawnBossAt(position);
    }

    private bool _bossSpawned;
    private bool _bossDefeated;

    // ── Unity ────────────────────────────────────────────────────────────────────
    private void Start()
    {
        // Attend un frame que ActivityManager.Start() ait placé les activités
        StartCoroutine(PlaceOnNavMeshNextFrame());
    }

    private IEnumerator PlaceOnNavMeshNextFrame()
    {
        yield return null;
        PlaceOnNavMesh();
    }

    private void PlaceOnNavMesh()
    {
        var activities = FindObjectsByType<ActivityBase>(FindObjectsSortMode.None);
        Vector3 zoneCenter = transform.position;

        for (int i = 0; i < MaxPlacementAttempts; i++)
        {
            Vector2 rnd = Random.insideUnitCircle * SearchRadius;
            Vector3 candidate = zoneCenter + new Vector3(rnd.x, 0f, rnd.y);

            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, NavMeshSampleDistance, NavMesh.AllAreas))
                continue;

            bool tooClose = false;
            foreach (var activity in activities)
            {
                if (activity == this) continue;
                if (Vector3.Distance(hit.position, activity.transform.position) < MinDistanceFromActivity)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose)
            {
                transform.position = hit.position;
                Debug.Log($"[BossPortal] Positionné à {hit.position} (tentative {i + 1}).");
                return;
            }
        }

        Debug.LogWarning("[BossPortal] Position valide introuvable — le portail reste à sa position initiale.");
    }

    protected override void OnInteract(SpellCaster caster)
    {
        if (_bossDefeated)
        {
            Complete();
            RunProgress.Level++;
            Debug.Log($"[BossPortal] Passage au niveau {RunProgress.Level}.");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return;
        }

        if (_bossSpawned) return;
        _bossSpawned = true;

        Vector3 spawnPos = BossSpawnPoint != null
            ? BossSpawnPoint.position
            : transform.position + transform.forward * 3f;
        SpawnBossAt(spawnPos);
    }

    // ── Spawn ────────────────────────────────────────────────────────────────────
    private void SpawnBossAt(Vector3 spawnPos)
    {
        if (BossPrefab == null)
        {
            Debug.LogWarning("[BossPortal] Aucun prefab boss assigné.");
            return;
        }

        var boss = Instantiate(BossPrefab, spawnPos, Quaternion.identity);

        float hpMult  = BossHpCurve.Evaluate(_currentDanger);
        float dmgMult = BossDamageCurve.Evaluate(_currentDanger);

        boss.GetComponent<EnemyHealth>()?.ApplyMultiplier(hpMult);
        boss.GetComponent<EnemyMeleeAttack>()?.ApplyMultiplier(dmgMult);

        var bossHealth = boss.GetComponent<EnemyHealth>();
        if (bossHealth != null)
            bossHealth.OnDied += () => OnBossDefeated(boss.transform.position);

        SpawnEscortMobs(spawnPos);

        Debug.Log($"[BossPortal] Boss spawné après {_currentDanger:F0}s — HP×{hpMult:F2} DPS×{dmgMult:F2}");
    }

    private void SpawnEscortMobs(Vector3 center)
    {
        int level = RunController.Instance != null ? RunController.Instance.LevelNumber : 1;

        foreach (var entry in EscortMobs)
        {
            if (entry?.Definition?.Prefab == null) continue;

            for (int i = 0; i < entry.Count; i++)
            {
                Vector2 rnd = Random.insideUnitCircle * 4f;
                Vector3 candidate = center + new Vector3(rnd.x, 0f, rnd.y);

                if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, NavMeshSampleDistance, NavMesh.AllAreas))
                    continue;

                var mob = EnemyPool.Instance != null
                    ? EnemyPool.Instance.Get(entry.Definition.Prefab, hit.position, Quaternion.identity)
                    : Instantiate(entry.Definition.Prefab, hit.position, Quaternion.identity);

                mob.GetComponent<EnemyGenericController>()?.Initialize(entry.Definition, level);
            }
        }
    }

    private void OnBossDefeated(Vector3 position)
    {
        SpawnBossLoot(position);
        _bossDefeated = true;

        if (IsFinalBoss)
        {
            Debug.Log("[BossPortal] Big Boss vaincu — fin de la partie.");
            RunController.Instance?.WinRun();
            return;
        }

        Debug.Log("[BossPortal] Boss vaincu — réinteragis avec le portail pour passer au niveau suivant.");
    }

    private void SpawnBossLoot(Vector3 position)
    {
        if (NodePickupPrefab == null) return;

        var pickup = Instantiate(NodePickupPrefab, position, Quaternion.identity);
        pickup.GetComponent<NodePickup>()?.Initialize(LootPool);
    }
}
