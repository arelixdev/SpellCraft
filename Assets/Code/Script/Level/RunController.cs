using Sirenix.OdinInspector;
using UnityEngine;
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

    // ── Events ──────────────────────────────────────────────────────────────────
    [FoldoutGroup("Events"), LabelText("Danger mis à jour (secondes écoulées)")]
    public UnityEvent<float> OnDangerChanged;

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
        var playerHealth = GameObject.FindWithTag("Player")?.GetComponent<PlayerHealth>();
        if (playerHealth != null)
            playerHealth.OnDied += StopRun;

        StartRun();
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
}
