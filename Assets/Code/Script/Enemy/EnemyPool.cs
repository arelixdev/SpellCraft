using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Pool d'ennemis global. Remplace Instantiate/Destroy par Get/Return.
/// Ajouter un GameObject "EnemyPool" dans la scène avec ce composant.
/// </summary>
public class EnemyPool : MonoBehaviour
{
    public static EnemyPool Instance { get; private set; }

    // ── Debug ────────────────────────────────────────────────────────────────────
    [FoldoutGroup("Debug"), ShowInInspector, ReadOnly, LabelText("Ennemis en attente dans le pool")]
    private int PooledCount => CountPooled();

    // prefab → file d'instances inactives
    private readonly Dictionary<GameObject, Queue<GameObject>> _pools    = new();
    // instanceID → prefab source (pour savoir dans quelle file remettre l'ennemi)
    private readonly Dictionary<int, GameObject>               _prefabOf = new();

    // ── Unity ────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── API publique ─────────────────────────────────────────────────────────────

    /// <summary>Récupère un ennemi du pool (ou en crée un nouveau si le pool est vide).</summary>
    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (!_pools.TryGetValue(prefab, out var queue))
        {
            queue = new Queue<GameObject>();
            _pools[prefab] = queue;
        }

        // Purge les entrées nulles au cas où un GO aurait été Destroy() manuellement
        while (queue.Count > 0 && queue.Peek() == null)
            queue.Dequeue();

        GameObject go;
        if (queue.Count > 0)
        {
            go = queue.Dequeue();
            go.transform.SetPositionAndRotation(position, rotation);
            go.transform.SetParent(parent);
        }
        else
        {
            go = Instantiate(prefab, position, rotation, parent);
            _prefabOf[go.GetInstanceID()] = prefab;
        }

        go.SetActive(true);
        return go;
    }

    /// <summary>Renvoie un ennemi au pool. Appelé automatiquement par EnemyHealth à la mort.</summary>
    public void Return(GameObject go)
    {
        if (go == null) return;

        go.SetActive(false);
        go.transform.SetParent(transform);

        if (_prefabOf.TryGetValue(go.GetInstanceID(), out var prefab)
            && _pools.TryGetValue(prefab, out var queue))
        {
            queue.Enqueue(go);
        }
        else
        {
            // GO non référencé dans le pool (ex: spawné manuellement) → on détruit proprement
            Destroy(go);
        }
    }

    /// <summary>
    /// Pré-chauffe le pool pour éviter les pics d'instantiation en début de run.
    /// À appeler depuis RunController.StartRun() si besoin.
    /// </summary>
    public void PreWarm(GameObject prefab, int count)
    {
        var temp = new List<GameObject>(count);
        for (int i = 0; i < count; i++)
            temp.Add(Get(prefab, Vector3.zero, Quaternion.identity));
        foreach (var go in temp)
            Return(go);
        Debug.Log($"[EnemyPool] Pré-chauffe : {count}× '{prefab.name}' prêts.");
    }

    // ── Interne ──────────────────────────────────────────────────────────────────
    private int CountPooled()
    {
        int n = 0;
        foreach (var q in _pools.Values) n += q.Count;
        return n;
    }
}
