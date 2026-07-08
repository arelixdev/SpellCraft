using System.Collections;
using UnityEngine;

/// <summary>
/// Compétence "Zone au sol" : mémorise la position du joueur au cast, télégraphe un cercle
/// à cet endroit, puis inflige des dégâts si le joueur y est encore au moment de l'impact.
/// Le déclenchement (choix aléatoire, exclusivité, délai entre skills) est géré par BossController.
/// </summary>
public class BossSlamSkill : MonoBehaviour, IBossSkill
{
    [Header("Portée de déclenchement")]
    [SerializeField] private float _maxRange = 20f;

    [Header("Zone au sol")]
    [SerializeField] private float _telegraphDuration = 0.6f;
    [SerializeField] private float _slamRadius        = 3f;
    [SerializeField] private float _damage            = 25f;

    private Transform    _player;
    private PlayerHealth _playerHealth;
    private EnemyAI      _ai;

    private void Awake()
    {
        _ai = GetComponent<EnemyAI>();
    }

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

        Vector3    targetPos = _player.position;
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
