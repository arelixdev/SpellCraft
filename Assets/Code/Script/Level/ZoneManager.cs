using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class ZoneManager : MonoBehaviour
{
    // ── Enums ───────────────────────────────────────────────────────────────────
    public enum ZoneType
    {
        Vague,
        Elite,
        ZoneCorrompue,
        AtelierReparation,
        MarcheNoir,
        SignalInconnu,
    }

    public enum ZoneState { Idle, Active, Terminee }

    // ── Type de zone ────────────────────────────────────────────────────────────
    [Title("Type de Zone")]
    [EnumToggleButtons, HideLabel]
    public ZoneType Type = ZoneType.Vague;

    [InfoBox("Survivez à toutes les vagues et éliminez chaque ennemi.",       InfoMessageType.Info, "@Type == ZoneType.Vague")]
    [InfoBox("Éliminez l'ennemi élite pour terminer la zone.",                InfoMessageType.Info, "@Type == ZoneType.Elite")]
    [InfoBox("Survivez pendant le temps imparti.",                            InfoMessageType.Info, "@Type == ZoneType.ZoneCorrompue")]
    [InfoBox("Zone de repos — terminez l'interaction pour repartir.",         InfoMessageType.Info, "@Type == ZoneType.AtelierReparation")]
    [InfoBox("Achetez ce dont vous avez besoin, puis quittez la zone.",       InfoMessageType.Info, "@Type == ZoneType.MarcheNoir")]
    [InfoBox("Signal mystérieux — explorez et décidez quand partir.",         InfoMessageType.Info, "@Type == ZoneType.SignalInconnu")]
    [HideInInspector] public bool _infoBoxHack; // force les InfoBox à s'afficher

    // ── Paramètres combat communs ───────────────────────────────────────────────
    [BoxGroup("Combat"), ShowIf("@IsCombatZone()")]
    [Required, LabelText("Prefab ennemi")]
    public GameObject EnemyPrefab;

    [BoxGroup("Combat"), ShowIf("@IsCombatZone()")]
    [MinValue(1f), LabelText("Rayon de spawn")]
    public float SpawnRadius = 15f;

    [BoxGroup("Combat"), ShowIf("@IsCombatZone()")]
    [MinValue(0.1f), LabelText("Tolérance NavMesh")]
    public float NavMeshSampleDistance = 2f;

    // ── Vague ───────────────────────────────────────────────────────────────────
    [BoxGroup("Vague"), ShowIf("@Type == ZoneType.Vague")]
    [MinValue(1), LabelText("Nombre de vagues")]
    public int TotalWaves = 3;

    [BoxGroup("Vague"), ShowIf("@Type == ZoneType.Vague")]
    [MinValue(1), LabelText("Ennemis par vague")]
    public int EnemiesPerWave = 5;

    [BoxGroup("Vague"), ShowIf("@Type == ZoneType.Vague")]
    [MinValue(0f), LabelText("Délai entre vagues (s)")]
    public float TimeBetweenWaves = 3f;

    // ── Élite ───────────────────────────────────────────────────────────────────
    [BoxGroup("Élite"), ShowIf("@Type == ZoneType.Elite")]
    [LabelText("Prefab élite"), Tooltip("Si vide, utilise le Prefab ennemi de base")]
    public GameObject ElitePrefab;

    [BoxGroup("Élite"), ShowIf("@Type == ZoneType.Elite")]
    [LabelText("Point de spawn élite"), Tooltip("Si vide, spawn hors caméra autour du joueur")]
    public Transform EliteSpawnPoint;

    // ── Zone Corrompue ──────────────────────────────────────────────────────────
    [BoxGroup("Zone Corrompue"), ShowIf("@Type == ZoneType.ZoneCorrompue")]
    [MinValue(5f), LabelText("Durée de survie (s)")]
    public float SurvivalDuration = 60f;

    [BoxGroup("Zone Corrompue"), ShowIf("@Type == ZoneType.ZoneCorrompue")]
    [LabelText("Spawner continu"), Tooltip("EnemySpawner (mode Illimité) à activer pendant la zone")]
    public EnemySpawner ContinuousSpawner;

    // ── Events ──────────────────────────────────────────────────────────────────
    [FoldoutGroup("Events")]
    [LabelText("Zone démarrée")]
    public UnityEvent OnZoneStarted;

    [FoldoutGroup("Events")]
    [LabelText("Zone terminée")]
    public UnityEvent OnZoneCompleted;

    [FoldoutGroup("Events"), ShowIf("@Type == ZoneType.Vague")]
    [LabelText("Vague commencée (index, total)")]
    public UnityEvent<int, int> OnWaveStarted;

    [FoldoutGroup("Events"), ShowIf("@Type == ZoneType.Vague")]
    [LabelText("Vague nettoyée (index, total)")]
    public UnityEvent<int, int> OnWaveCleared;

    [FoldoutGroup("Events"), ShowIf("@Type == ZoneType.ZoneCorrompue")]
    [LabelText("Temps restant mis à jour")]
    public UnityEvent<float> OnSurvivalTimerTick;

    // ── État ────────────────────────────────────────────────────────────────────
    [FoldoutGroup("État"), ShowInInspector, ReadOnly, LabelText("État")]
    public ZoneState State { get; private set; } = ZoneState.Idle;

    [FoldoutGroup("État"), ShowInInspector, ReadOnly, LabelText("Vague actuelle")]
    [ShowIf("@Type == ZoneType.Vague")]
    private int CurrentWave => _currentWave;

    [FoldoutGroup("État"), ShowInInspector, ReadOnly, LabelText("Ennemis trackés vivants")]
    [ShowIf("@IsCombatWithTracking()")]
    private int TrackedAlive => _trackedEnemies.Count;

    [FoldoutGroup("État"), ReadOnly, LabelText("Temps restant (s)")]
    [ShowIf("@Type == ZoneType.ZoneCorrompue")]
    public float SurvivalTimeLeft;

    // ── Privé ───────────────────────────────────────────────────────────────────
    private Transform _player;
    private Camera _camera;
    private int _currentWave;
    private float _survivalTimer;
    private readonly List<EnemyHealth> _trackedEnemies = new();
    private Transform _enemyParent;

    // ── Unity ───────────────────────────────────────────────────────────────────
    private void Awake()
    {
        var playerGO = GameObject.FindWithTag("Player");
        if (playerGO != null) _player = playerGO.transform;
        _camera = Camera.main;
        _enemyParent = new GameObject($"[{Type} Enemies]").transform;
    }

    private void OnDestroy()
    {
        if (_enemyParent != null)
            Destroy(_enemyParent.gameObject);
    }

    private void Update()
    {
        if (State != ZoneState.Active || Type != ZoneType.ZoneCorrompue) return;

        _survivalTimer += Time.deltaTime;
        SurvivalTimeLeft = Mathf.Max(0f, SurvivalDuration - _survivalTimer);
        OnSurvivalTimerTick?.Invoke(SurvivalTimeLeft);

        if (_survivalTimer >= SurvivalDuration)
            CompleteZone();
    }

    // ── API publique ────────────────────────────────────────────────────────────
    [FoldoutGroup("État"), Button("▶  Démarrer la zone")]
    public void StartZone()
    {
        if (State == ZoneState.Active) return;
        State = ZoneState.Active;
        OnZoneStarted?.Invoke();
        BeginZone();
    }

    /// <summary>
    /// Appeler depuis un bouton UI pour compléter les zones passives
    /// (AtelierReparation, MarcheNoir, SignalInconnu).
    /// </summary>
    [FoldoutGroup("État"), Button("✓  Terminer la zone")]
    public void CompleteZone()
    {
        if (State == ZoneState.Terminee) return;
        State = ZoneState.Terminee;

        StopAllCoroutines();

        if (ContinuousSpawner != null)
            ContinuousSpawner.StopSpawning();

        foreach (var health in FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
            if (!health.IsInvincible) Destroy(health.gameObject);
        _trackedEnemies.Clear();

        var playerGO = GameObject.FindWithTag("Player");
        if (playerGO != null)
            playerGO.GetComponent<SpellCaster>()?.DisableCasting();

        OnZoneCompleted?.Invoke();
        Debug.Log($"[ZoneManager] Zone '{Type}' terminée.");
    }

    // ── Logique par type ────────────────────────────────────────────────────────
    private void BeginZone()
    {
        switch (Type)
        {
            case ZoneType.Vague:
                _currentWave = 0;
                StartCoroutine(LaunchNextWave(0f));
                break;

            case ZoneType.Elite:
                SpawnElite();
                break;

            case ZoneType.ZoneCorrompue:
                _survivalTimer = 0f;
                if (ContinuousSpawner != null)
                    ContinuousSpawner.StartSpawning();
                break;

            case ZoneType.AtelierReparation:
            case ZoneType.MarcheNoir:
            case ZoneType.SignalInconnu:
                Debug.Log($"[ZoneManager] Zone passive '{Type}' active — en attente de CompleteZone().");
                break;
        }
    }

    // ── Vague ───────────────────────────────────────────────────────────────────
    private IEnumerator LaunchNextWave(float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        _currentWave++;
        Debug.Log($"[ZoneManager] Vague {_currentWave}/{TotalWaves}");
        OnWaveStarted?.Invoke(_currentWave, TotalWaves);

        for (int i = 0; i < EnemiesPerWave; i++)
            SpawnTrackedEnemy(EnemyPrefab);
    }

    private void HandleTrackedEnemyDied(EnemyHealth enemy)
    {
        _trackedEnemies.Remove(enemy);

        switch (Type)
        {
            case ZoneType.Vague:
                if (_trackedEnemies.Count > 0) return;

                OnWaveCleared?.Invoke(_currentWave, TotalWaves);
                Debug.Log($"[ZoneManager] Vague {_currentWave}/{TotalWaves} nettoyée.");

                if (_currentWave >= TotalWaves)
                    CompleteZone();
                else
                    StartCoroutine(LaunchNextWave(TimeBetweenWaves));
                break;

            case ZoneType.Elite:
                if (_trackedEnemies.Count == 0)
                    CompleteZone();
                break;
        }
    }

    // ── Élite ───────────────────────────────────────────────────────────────────
    private void SpawnElite()
    {
        var prefab = ElitePrefab != null ? ElitePrefab : EnemyPrefab;
        if (prefab == null)
        {
            Debug.LogWarning("[ZoneManager] Aucun prefab élite/ennemi assigné.");
            return;
        }

        if (EliteSpawnPoint != null)
            SpawnTrackedEnemy(prefab, EliteSpawnPoint.position);
        else if (TryGetSpawnPosition(out Vector3 pos))
            SpawnTrackedEnemy(prefab, pos);
        else
            Debug.LogWarning("[ZoneManager] Impossible de trouver une position de spawn pour l'élite.");
    }

    // ── Spawn helper ────────────────────────────────────────────────────────────
    private void SpawnTrackedEnemy(GameObject prefab, Vector3? overridePos = null)
    {
        Vector3 pos;
        if (overridePos.HasValue)
            pos = overridePos.Value;
        else if (!TryGetSpawnPosition(out pos))
        {
            Debug.LogWarning("[ZoneManager] Position de spawn introuvable, spawn ignoré.");
            return;
        }

        var go = Instantiate(prefab, pos, Quaternion.identity, _enemyParent);
        var health = go.GetComponent<EnemyHealth>();
        if (health == null) return;

        _trackedEnemies.Add(health);
        health.OnDied += () => HandleTrackedEnemyDied(health);
    }

    private bool TryGetSpawnPosition(out Vector3 result)
    {
        const int maxAttempts = 30;
        Vector3 origin = _player != null ? _player.position : transform.position;

        for (int i = 0; i < maxAttempts; i++)
        {
            float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            var dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            Vector3 candidate = origin + dir * SpawnRadius;

            if (_camera != null && !IsOutsideCamera(candidate)) continue;

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, NavMeshSampleDistance, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
        }

        result = Vector3.zero;
        return false;
    }

    private bool IsOutsideCamera(Vector3 worldPos)
    {
        Vector3 vp = _camera.WorldToViewportPoint(worldPos);
        return vp.z < 0f || vp.x < 0f || vp.x > 1f || vp.y < 0f || vp.y > 1f;
    }

    // ── ShowIf helpers ──────────────────────────────────────────────────────────
    private bool IsCombatZone() =>
        Type == ZoneType.Vague || Type == ZoneType.Elite || Type == ZoneType.ZoneCorrompue;

    private bool IsCombatWithTracking() =>
        Type == ZoneType.Vague || Type == ZoneType.Elite;

    // ── Gizmos ──────────────────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        if (!IsCombatZone()) return;
        Transform origin = Application.isPlaying && _player != null ? _player : transform;
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.25f);
        Gizmos.DrawWireSphere(origin.position, SpawnRadius);

        if (EliteSpawnPoint != null && Type == ZoneType.Elite)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(EliteSpawnPoint.position, 1f);
        }
    }
}
