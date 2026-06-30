using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Activité coffre : le joueur entre dans le trigger, une node aléatoire lui est donnée.
/// Hérite de ActivityBase — appeler Complete() clôture l'activité.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ChestActivity : ActivityBase
{
    // ── Loot ────────────────────────────────────────────────────────────────────
    [BoxGroup("Loot"), LabelText("Catalogue global")]
    [Tooltip("Utilisé si Pool de nodes est vide")]
    public SpellNodeCatalogSO Catalog;

    [BoxGroup("Loot"), LabelText("Pool de nodes spécifiques")]
    [Tooltip("Si rempli, le coffre tire uniquement parmi ces nodes (ignore le catalogue)")]
    public List<SpellNodeSO> NodePool = new();

    // ── Visuals ──────────────────────────────────────────────────────────────────
    [BoxGroup("Visuals"), LabelText("Animator (optionnel)")]
    [Tooltip("Doit avoir un trigger 'Open' pour l'animation d'ouverture")]
    public Animator ChestAnimator;

    // ── État ─────────────────────────────────────────────────────────────────────
    private bool _opened;

    // ── Trigger ──────────────────────────────────────────────────────────────────
    private void Awake()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_opened) return;

        var caster = other.GetComponent<SpellCaster>()
                  ?? other.GetComponentInParent<SpellCaster>();
        if (caster == null) return;

        Open(caster);
    }

    // ── Ouverture ─────────────────────────────────────────────────────────────────
    private void Open(SpellCaster caster)
    {
        _opened = true;
        ChestAnimator?.SetTrigger("Open");

        SpellNodeSO node = PickNode();
        if (node != null)
        {
            caster.CollectNode(node);
            Debug.Log($"[ChestActivity] Node donnée : {node.nodeName}");
        }
        else
        {
            Debug.LogWarning("[ChestActivity] Aucune node disponible dans le pool ni le catalogue.");
        }

        Complete();
    }

    private SpellNodeSO PickNode()
    {
        var pool = NodePool.Count > 0 ? NodePool : Catalog?.allNodes;
        if (pool == null || pool.Count == 0) return null;
        return pool[Random.Range(0, pool.Count)];
    }

    // ── Gizmos ───────────────────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        var col = GetComponent<Collider>();
        if (col == null) return;
        Gizmos.color = new Color(1f, 0.85f, 0f, 0.3f);
        Gizmos.DrawWireCube(transform.position, col.bounds.size);
    }
}
