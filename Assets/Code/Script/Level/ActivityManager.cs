using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Au démarrage, tire N activités aléatoires du pool et les instancie
/// aux points de spawn définis dans la scène.
/// </summary>
public class ActivityManager : MonoBehaviour
{
    // ── Configuration ────────────────────────────────────────────────────────────
    [Title("Pool d'activités")]
    [LabelText("Activités disponibles")]
    [Tooltip("Toutes les ActivitySO que le jeu peut tirer pour cette zone")]
    public List<ActivitySO> ActivityPool = new();

    [Title("Placement")]
    [LabelText("Points de spawn dans la zone")]
    [Tooltip("Nombre de points = nombre max d'activités simultanées")]
    public Transform[] SpawnPoints;

    [LabelText("Nombre d'activités à placer")]
    [MinValue(1)]
    public int ActivityCount = 5;

    // ── État ─────────────────────────────────────────────────────────────────────
    [FoldoutGroup("État"), ShowInInspector, ReadOnly, LabelText("Activités complétées")]
    public int CompletedCount { get; private set; }

    [FoldoutGroup("État"), ShowInInspector, ReadOnly, LabelText("Activités placées")]
    public int PlacedCount { get; private set; }

    public event Action<ActivityBase> OnActivityCompleted;
    public event Action               OnAllActivitiesCompleted;

    // ── Unity ────────────────────────────────────────────────────────────────────
    private void Start() => PlaceActivities();

    // ── Placement ────────────────────────────────────────────────────────────────
    private void PlaceActivities()
    {
        if (ActivityPool.Count == 0 || SpawnPoints.Length == 0)
        {
            Debug.LogWarning("[ActivityManager] Pool ou SpawnPoints vide — aucune activité placée.");
            return;
        }

        int count = Mathf.Min(ActivityCount, SpawnPoints.Length, ActivityPool.Count);

        // Tirage sans remise + ordre aléatoire des points
        var selectedDefs   = ActivityPool.OrderBy(_ => UnityEngine.Random.value).Take(count).ToList();
        var shuffledPoints = SpawnPoints.OrderBy(_ => UnityEngine.Random.value).Take(count).ToArray();

        for (int i = 0; i < count; i++)
        {
            ActivitySO def = selectedDefs[i];
            if (def?.Prefab == null) continue;

            var go = Instantiate(def.Prefab, shuffledPoints[i].position, shuffledPoints[i].rotation);
            var activity = go.GetComponent<ActivityBase>();
            if (activity == null)
            {
                Debug.LogWarning($"[ActivityManager] Le prefab '{def.Prefab.name}' n'a pas de composant ActivityBase.");
                continue;
            }

            activity.Definition = def;
            activity.OnCompleted += () => HandleCompleted(activity);
            PlacedCount++;
        }

        Debug.Log($"[ActivityManager] {PlacedCount} activités placées.");
    }

    private void HandleCompleted(ActivityBase activity)
    {
        CompletedCount++;
        OnActivityCompleted?.Invoke(activity);
        Debug.Log($"[ActivityManager] {CompletedCount}/{PlacedCount} activités complétées.");

        if (CompletedCount >= PlacedCount)
            OnAllActivitiesCompleted?.Invoke();
    }
}
