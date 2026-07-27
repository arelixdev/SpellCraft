using System.Collections;
using UnityEngine;

/// <summary>
/// Compétence "Zone au sol" : coup de marteau ancré près du boss, dans la direction du
/// joueur au moment du cast — pas téléporté sur la position du joueur (BossChargeSkill
/// s'occupe déjà de rattraper un joueur loin). Télégraphe un cercle, puis inflige des
/// dégâts si le joueur y est encore au moment de l'impact.
/// Le déclenchement (choix aléatoire, exclusivité, délai entre skills) est géré par BossController.
/// </summary>
public class BossSlamSkill : MonoBehaviour, IBossSkill
{
    [Header("Portée de déclenchement (courte distance : rattraper est le rôle de Charge)")]
    [SerializeField] private float _maxRange = 10f;

    [Header("Zone au sol")]
    [SerializeField] private float _telegraphDuration = 0.6f;
    [SerializeField] private float _slamRadius        = 3f;
    [SerializeField] private float _damage            = 25f;

    private Transform      _player;
    private PlayerHealth   _playerHealth;
    private EnemyAI        _ai;
    private CapsuleCollider _collider;

    private void Awake()
    {
        _ai       = GetComponent<EnemyAI>();
        _collider = GetComponent<CapsuleCollider>();
    }

    // transform.position est au pivot du boss (centre du CapsuleCollider, pas les pieds —
    // même convention que le CharacterController du Player, voir RobotLoader.cs), donc tout
    // effet ancré au sol doit recaler sa hauteur sur le bas du collider plutôt que le pivot.
    private float GroundY() => _collider != null ? _collider.bounds.min.y : transform.position.y;

    private void Start()
    {
        var playerGO = GameObject.FindWithTag("Player");
        if (playerGO == null) return;

        _player       = playerGO.transform;
        _playerHealth = playerGO.GetComponent<PlayerHealth>();
    }

    public bool IsInRange(Transform player) => Vector3.Distance(transform.position, player.position) <= _maxRange;

    public IEnumerator Execute()
    {
        _ai.CanMove = false;

        Vector3 dirToPlayer = _player.position - transform.position;
        dirToPlayer.y = 0f;
        if (dirToPlayer.sqrMagnitude < 0.0001f) dirToPlayer = transform.forward;
        dirToPlayer.Normalize();

        // Ancré à _slamRadius du boss (pas sur le joueur) : le marteau frappe le sol
        // juste devant/autour du boss, dans la direction visée au cast.
        Vector3    targetPos = transform.position + dirToPlayer * _slamRadius;
        targetPos.y = GroundY();
        GameObject marker    = CreateTelegraphMarker(targetPos);

        yield return new WaitForSeconds(_telegraphDuration);

        Destroy(marker);

        if (Vector3.Distance(_player.position, targetPos) <= _slamRadius)
            _playerHealth.TakeDamage(_damage);

        _ai.CanMove = true;
    }

    private GameObject CreateTelegraphMarker(Vector3 position)
    {
        var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = "BossSlamTelegraph";
        Destroy(marker.GetComponent<Collider>());

        marker.GetComponent<MeshRenderer>().material.color = new Color(1f, 0f, 0f, 0.5f);

        marker.transform.position   = position + Vector3.up * 0.05f;
        marker.transform.localScale = new Vector3(_slamRadius * 2f, 0.05f, _slamRadius * 2f);

        return marker;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _maxRange);
    }
}
