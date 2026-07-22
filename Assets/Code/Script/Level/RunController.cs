using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

/// <summary>
/// Cerveau de la session : incrémente le DangerLevel au fil du temps,
/// pilote l'EnemySpawner et informe le BossPortal.
/// La tension monte naturellement — plus le joueur attend, plus c'est dur.
/// </summary>
public class RunController : MonoBehaviour
{
    public static RunController Instance { get; private set; }

    // ── Références ──────────────────────────────────────────────────────────────
    [BoxGroup("Références"), LabelText("Activer les ennemis")]
    public bool SpawnEnemies = true;

    [BoxGroup("Références"), ShowIf("SpawnEnemies"), LabelText("Spawner d'ennemis")]
    public EnemySpawner EnemySpawner;

    [BoxGroup("Références"), LabelText("Boss Portal")]
    public BossPortal BossPortal;

    [BoxGroup("Références"), MinValue(1), LabelText("Numéro de niveau (scène)")]
    [Tooltip("Utilisé par EnemySpawner pour scaler les stats de base des EnemyDefinitionSO (healthScalePerLevel / damageScalePerLevel)")]
    public int LevelNumber = 1;

    [BoxGroup("Références"), MinValue(1f), LabelText("Rayon de spawn joueur (m)")]
    [Tooltip("Distance max depuis le centre de la zone (ActivityManager) pour repositionner le joueur sur le NavMesh à chaque chargement")]
    public float PlayerSpawnRadius = 25f;

    // ── Events ──────────────────────────────────────────────────────────────────
    [FoldoutGroup("Events"), LabelText("Danger mis à jour (secondes écoulées)")]
    public UnityEvent<float> OnDangerChanged;

    [FoldoutGroup("Events"), LabelText("Victoire (Big Boss vaincu)")]
    public UnityEvent OnVictory;

    // ── État ─────────────────────────────────────────────────────────────────────
    [FoldoutGroup("État"), ShowInInspector, ReadOnly, LabelText("Temps écoulé (s)")]
    public float TimeElapsed { get; private set; }

    [FoldoutGroup("État"), ShowInInspector, ReadOnly, LabelText("Run actif")]
    public bool IsRunning { get; private set; }

    [FoldoutGroup("État"), ShowInInspector, ReadOnly, LabelText("Coffres ouverts")]
    public int ChestsOpened { get; private set; }

    private float _lastEventSecond = -1f;

    // ── Unity ───────────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        LevelNumber = RunProgress.Level;
    }

    private void Start()
    {
        RepositionPlayerOnNavMesh();

        var playerHealth = GameObject.FindWithTag("Player")?.GetComponent<PlayerHealth>();
        if (playerHealth != null)
            playerHealth.OnDied += StopRun;

        StartRun();
    }

    // Le Player survit au rechargement de scène (PlayerPersistence) : il garde sa position
    // (et sa vélocité de chute) du niveau précédent, qui ne correspond à rien dans le
    // nouveau décor (chargé en additif, layout différent) — d'où la chute dans le vide au
    // niveau suivant / au redémarrage d'un run. On le repose sur un point aléatoire du
    // NavMesh du niveau qui vient de charger, à chaque fois, pour garantir qu'il est bien
    // posé sur le terrain (même logique que BossPortal.PlaceOnNavMesh / EnemySpawner).
    private void RepositionPlayerOnNavMesh()
    {
        var playerGO = GameObject.FindWithTag("Player");
        if (playerGO == null) return;

        var activityManager = FindFirstObjectByType<ActivityManager>();
        Vector3 center = activityManager != null ? activityManager.transform.position : transform.position;

        for (int i = 0; i < 30; i++)
        {
            Vector2 rnd = Random.insideUnitCircle * PlayerSpawnRadius;
            Vector3 candidate = center + new Vector3(rnd.x, 0f, rnd.y);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            {
                playerGO.GetComponent<PlayerController>()?.Teleport(hit.position);
                return;
            }
        }

        Debug.LogWarning("[RunController] Aucune position NavMesh valide trouvée pour le spawn du joueur — position inchangée.");
    }

    private void Update()
    {
        if (!IsRunning) return;

        TimeElapsed += Time.deltaTime;

        if (SpawnEnemies) EnemySpawner?.SetDangerLevel(TimeElapsed);
        BossPortal?.SetDangerLevel(TimeElapsed);

        // Fire event once per second (pas besoin chaque frame pour l'UI)
        int currentSecond = Mathf.FloorToInt(TimeElapsed);
        if (currentSecond != _lastEventSecond)
        {
            _lastEventSecond = currentSecond;
            OnDangerChanged?.Invoke(TimeElapsed);
        }
    }

    // ── API ──────────────────────────────────────────────────────────────────────
    [Button("▶ Démarrer le run")]
    public void StartRun()
    {
        if (IsRunning) return;
        IsRunning    = true;
        TimeElapsed  = 0f;
        ChestsOpened = 0;
        if (SpawnEnemies) EnemySpawner?.StartSpawning();
        Debug.Log("[RunController] Run démarré.");
    }

    public void RegisterChestOpened() => ChestsOpened++;

    [Button("■ Arrêter le run")]
    public void StopRun()
    {
        if (!IsRunning) return;
        IsRunning = false;
        if (SpawnEnemies) EnemySpawner?.StopSpawning();
        Debug.Log("[RunController] Run arrêté.");
    }

    public void WinRun()
    {
        if (!IsRunning) return;
        IsRunning = false;
        if (SpawnEnemies) EnemySpawner?.StopSpawning();
        OnVictory?.Invoke();
        Debug.Log("[RunController] Run gagné !");
    }
}
