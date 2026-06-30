using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Gère le LOD de l'ennemi :
///   - Bucket de distance (0=proche / 1=moyen / 2=loin)
///   - Stagger des updates NavMesh via GetInstanceID (zéro configuration)
///   - Sommeil du NavMeshAgent au-delà de SleepRange (coût quasi zéro)
///
/// Ajouter ce composant sur chaque prefab ennemi.
/// EnemyAI et EnemyMeleeAttack l'interrogent avant d'agir.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyLOD : MonoBehaviour
{
    // ── Seuils ───────────────────────────────────────────────────────────────────
    [BoxGroup("Seuils"), LabelText("Proche → Moyen (m)")]
    public float CloseRange  = 12f;

    [BoxGroup("Seuils"), LabelText("Moyen → Loin (m)")]
    public float MediumRange = 30f;

    [BoxGroup("Seuils"), LabelText("Rayon Sommeil (m)")]
    [Tooltip("Au-delà, le NavMeshAgent est désactivé")]
    public float SleepRange  = 50f;

    [BoxGroup("Seuils"), LabelText("Rayon Réveil (m)")]
    [Tooltip("Hysteresis : inférieur à SleepRange pour éviter le yo-yo")]
    public float WakeRange   = 40f;

    // ── Intervalles de mise à jour en frames ─────────────────────────────────────
    //                               [ Proche, Moyen,       Loin ]
    private static readonly int[] s_pathInterval   = {  1,     6,         20 };
    private static readonly int[] s_attackInterval = {  1,     3, int.MaxValue }; // Loin = jamais

    // ── État ─────────────────────────────────────────────────────────────────────
    [FoldoutGroup("État"), ShowInInspector, ReadOnly, LabelText("Bucket (0=proche 2=loin)")]
    public int Bucket { get; private set; }

    [FoldoutGroup("État"), ShowInInspector, ReadOnly, LabelText("Endormi")]
    public bool IsSleeping { get; private set; }

    private Transform    _player;
    private NavMeshAgent _agent;
    private int          _staggerId; // dérivé de l'instance ID, stable et unique

    // ── Unity ────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        _agent     = GetComponent<NavMeshAgent>();
        _staggerId = Mathf.Abs(GetInstanceID());
    }

    private void Start()
    {
        var go = GameObject.FindWithTag("Player");
        if (go != null) _player = go.transform;
    }

    private void OnEnable()
    {
        // Réveille le NavMeshAgent si le pool réutilise cet ennemi
        IsSleeping = false;
        if (_agent != null) _agent.enabled = true;
    }

    private void Update()
    {
        if (_player == null) return;

        // Utiliser sqrMagnitude : évite une sqrt par frame par ennemi
        float sqDist = (_player.position - transform.position).sqrMagnitude;

        // ── Sommeil / Réveil ──
        if (!IsSleeping && sqDist > SleepRange * SleepRange)
        {
            IsSleeping     = true;
            _agent.enabled = false;
            return;
        }

        if (IsSleeping)
        {
            if (sqDist > WakeRange * WakeRange) return;
            IsSleeping     = false;
            _agent.enabled = true;
        }

        // ── Bucket ──
        if      (sqDist < CloseRange  * CloseRange)  Bucket = 0;
        else if (sqDist < MediumRange * MediumRange) Bucket = 1;
        else                                          Bucket = 2;
    }

    // ── API interrogée par EnemyAI / EnemyMeleeAttack ───────────────────────────

    /// <summary>
    /// Retourne true si cet ennemi doit recalculer son chemin ce frame.
    /// Faux si endormi ou si son slot de stagger n'est pas actif ce frame.
    /// </summary>
    public bool ShouldUpdatePath()
    {
        if (IsSleeping) return false;
        int interval = s_pathInterval[Bucket];
        return Time.frameCount % interval == _staggerId % interval;
    }

    /// <summary>
    /// Retourne true si cet ennemi peut tenter une attaque ce frame.
    /// Les ennemis du bucket Loin n'attaquent jamais.
    /// </summary>
    public bool ShouldUpdateAttack()
    {
        if (IsSleeping || Bucket == 2) return false;
        int interval = s_attackInterval[Bucket];
        return Time.frameCount % interval == _staggerId % interval;
    }

    // ── Gizmos ───────────────────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        Color[] colors = { Color.green, Color.yellow, Color.red };
        float[] ranges = { CloseRange, MediumRange, SleepRange };

        for (int i = 0; i < 3; i++)
        {
            Gizmos.color = colors[i] * new Color(1, 1, 1, 0.3f);
            Gizmos.DrawWireSphere(transform.position, ranges[i]);
        }
    }
}
