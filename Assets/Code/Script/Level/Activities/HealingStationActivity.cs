using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Activité station de recharge : appuyer sur Espace dans la zone restaure
/// un pourcentage des HP max du joueur, puis l'activité est consommée.
/// </summary>
public class HealingStationActivity : ActivityBase
{
    [BoxGroup("Soin"), LabelText("Pourcentage des HP max restaurés")]
    [Range(0f, 1f)]
    public float HealPercent = 0.25f;

    protected override void OnInteract(SpellCaster caster)
    {
        var health = caster.GetComponent<PlayerHealth>();
        if (health == null) return;

        float amount = health.MaxHealth * HealPercent;
        health.Heal(amount);

        Complete();
    }

    private void OnDrawGizmosSelected()
    {
        var col = GetComponent<Collider>();
        if (col == null) return;
        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.3f);
        Gizmos.DrawWireCube(transform.position, col.bounds.size);
    }
}
