using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Portail boss : toujours accessible dans la zone.
/// Plus le joueur attend, plus le boss est puissant.
/// SetDangerLevel est appelé chaque seconde par RunController.
/// </summary>
[RequireComponent(typeof(Collider))]
public class BossPortal : MonoBehaviour
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

    [FoldoutGroup("État"), ShowInInspector, ReadOnly, LabelText("Boss spawné")]
    private bool _bossSpawned;

    [FoldoutGroup("État"), ShowInInspector, ReadOnly, LabelText("HP actuels du boss (×)")]
    private float CurrentHpMult => BossHpCurve.Evaluate(_currentDanger);

    [FoldoutGroup("État"), ShowInInspector, ReadOnly, LabelText("DPS actuels du boss (×)")]
    private float CurrentDmgMult => BossDamageCurve.Evaluate(_currentDanger);

    // ── API ──────────────────────────────────────────────────────────────────────
    public void SetDangerLevel(float dangerSeconds) => _currentDanger = dangerSeconds;

    // ── Unity ────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

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

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || _bossSpawned) return;
        SpawnBoss();
    }

    // ── Spawn ────────────────────────────────────────────────────────────────────
    private void SpawnBoss()
    {
        if (BossPrefab == null)
        {
            Debug.LogWarning("[BossPortal] Aucun prefab boss assigné.");
            return;
        }

        _bossSpawned = true;

        Vector3 spawnPos = BossSpawnPoint != null
            ? BossSpawnPoint.position
            : transform.position + transform.forward * 3f;

        var boss = Instantiate(BossPrefab, spawnPos, Quaternion.identity);

        float hpMult  = BossHpCurve.Evaluate(_currentDanger);
        float dmgMult = BossDamageCurve.Evaluate(_currentDanger);

        boss.GetComponent<EnemyHealth>()?.ApplyMultiplier(hpMult);
        boss.GetComponent<EnemyMeleeAttack>()?.ApplyMultiplier(dmgMult);

        var bossHealth = boss.GetComponent<EnemyHealth>();
        if (bossHealth != null)
            bossHealth.OnDied += () => SpawnBossLoot(boss.transform.position);

        Debug.Log($"[BossPortal] Boss spawné après {_currentDanger:F0}s — HP×{hpMult:F2} DPS×{dmgMult:F2}");
    }

    private void SpawnBossLoot(Vector3 position)
    {
        if (NodePickupPrefab == null) return;

        var pickup = Instantiate(NodePickupPrefab, position, Quaternion.identity);
        pickup.GetComponent<NodePickup>()?.Initialize(LootPool);
    }
}
