using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Au démarrage, tire N activités aléatoires du pool et les place
/// aléatoirement sur le NavMesh en respectant le rayon d'exclusion de chaque activité.
/// Positionner ce GameObject au centre de la zone.
/// </summary>
public class ActivityManager : MonoBehaviour
{
    // ── Configuration ────────────────────────────────────────────────────────────
    [Title("Pool d'activités")]
    [LabelText("Activités disponibles")]
    public List<ActivitySO> ActivityPool = new();

    [LabelText("Nombre d'activités à placer")]
    [MinValue(1)]
    public int ActivityCount = 5;

    [Title("Placement NavMesh")]
    [LabelText("Rayon de recherche (m)")]
    [Tooltip("Distance max depuis le centre de la zone (position de ce GameObject)")]
    public float SearchRadius = 50f;

    [LabelText("Tolérance NavMesh (m)")]
    public float NavMeshSampleDistance = 3f;

    [LabelText("Tentatives max par activité")]
    public int MaxPlacementAttempts = 60;

    // ── État ─────────────────────────────────────────────────────────────────────
    [FoldoutGroup("État"), ShowInInspector, ReadOnly, LabelText("Activités placées")]
    public int PlacedCount { get; private set; }

    [FoldoutGroup("État"), ShowInInspector, ReadOnly, LabelText("Activités complétées")]
    public int CompletedCount { get; private set; }

    public event Action<ActivityBase> OnActivityCompleted;
    public event Action               OnAllActivitiesCompleted;

    // positions déjà occupées avec leur rayon d'exclusion respectif
    private readonly List<(Vector3 position, float exclusionRadius)> _placed = new();

    // ── Unity ────────────────────────────────────────────────────────────────────
    private void Start() => PlaceActivities();

    // ── Placement ────────────────────────────────────────────────────────────────
    private void PlaceActivities()
    {
        if (ActivityPool.Count == 0)
        {
            Debug.LogWarning("[ActivityManager] Pool d'activités vide.");
            return;
        }

        // Tirage AVEC remise : le même type peut apparaître plusieurs fois
        var selected = new List<ActivitySO>();
        for (int i = 0; i < ActivityCount; i++)
            selected.Add(ActivityPool[UnityEngine.Random.Range(0, ActivityPool.Count)]);

        foreach (var def in selected)
        {
            if (def?.Prefab == null) continue;

            if (!TryFindPosition(def, out Vector3 pos))
            {
                Debug.LogWarning($"[ActivityManager] '{def.DisplayName}' : aucune position NavMesh valide trouvée en {MaxPlacementAttempts} tentatives. Vérifie que le NavMesh est baked et que SearchRadius couvre la zone.");
                continue;
            }

            var go = Instantiate(def.Prefab, pos, Quaternion.identity);
            var activity = go.GetComponent<ActivityBase>();
            if (activity == null)
            {
                Debug.LogWarning($"[ActivityManager] '{def.Prefab.name}' n'a pas de composant ActivityBase.");
                Destroy(go);
                continue;
            }

            activity.Definition = def;
            activity.OnCompleted += () => HandleCompleted(activity);

            _placed.Add((pos, def.ExclusionRadius));
            PlacedCount++;
        }

        Debug.Log($"[ActivityManager] {PlacedCount}/{ActivityCount} activités placées.");
    }

    private bool TryFindPosition(ActivitySO def, out Vector3 result)
    {
        Vector3 center = transform.position;

        for (int i = 0; i < MaxPlacementAttempts; i++)
        {
            Vector2 rnd       = UnityEngine.Random.insideUnitCircle * SearchRadius;
            Vector3 candidate = center + new Vector3(rnd.x, 0f, rnd.y);

            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, NavMeshSampleDistance, NavMesh.AllAreas))
                continue;

            if (IsTooClose(hit.position, def.ExclusionRadius))
                continue;

            result = hit.position;
            return true;
        }

        result = Vector3.zero;
        return false;
    }

    // Une position est invalide si elle empiète sur l'exclusion de l'activité à placer
    // OU sur l'exclusion d'une activité déjà placée.
    private bool IsTooClose(Vector3 candidate, float candidateExclusion)
    {
        foreach (var (pos, radius) in _placed)
        {
            float minDist = Mathf.Max(candidateExclusion, radius);
            if (Vector3.Distance(candidate, pos) < minDist)
                return true;
        }
        return false;
    }

    // ── Complétion ───────────────────────────────────────────────────────────────
    private void HandleCompleted(ActivityBase activity)
    {
        CompletedCount++;
        OnActivityCompleted?.Invoke(activity);
        Debug.Log($"[ActivityManager] {CompletedCount}/{PlacedCount} activités complétées.");

        if (CompletedCount >= PlacedCount)
            OnAllActivitiesCompleted?.Invoke();
    }

    // ── Gizmos ───────────────────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, SearchRadius);
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        foreach (var (pos, radius) in _placed)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
            Gizmos.DrawWireSphere(pos, radius);
        }
    }
}
