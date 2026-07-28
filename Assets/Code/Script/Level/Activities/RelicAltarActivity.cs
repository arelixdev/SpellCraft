using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

/// <summary>
/// Activité autel : le joueur entre dans le trigger, appuie sur Espace,
/// et une relique aléatoire lui est donnée (gratuit, contrairement au coffre).
/// </summary>
public class RelicAltarActivity : ActivityBase
{
    [BoxGroup("Loot"), LabelText("Relic Pool")]
    public RelicPoolSO RelicPool;

    [BoxGroup("Visuals"), LabelText("Label nom (optionnel)")]
    [Tooltip("Texte affiché au-dessus de l'autel quand le joueur entre à portée")]
    public TMP_Text RelicNameLabel;

    [BoxGroup("Visuals"), LabelText("Animator (optionnel)")]
    [Tooltip("Doit avoir un trigger 'Activate' pour l'animation de récupération")]
    public Animator AltarAnimator;

    protected override void Awake()
    {
        base.Awake();
        OnPlayerNearby += ShowLabel;
        OnPlayerLeft   += HideLabel;
        HideLabel();
    }

    private void ShowLabel(SpellCaster caster)
    {
        if (RelicNameLabel == null) return;
        RelicNameLabel.text = "Espace : prendre la relique";
        RelicNameLabel.gameObject.SetActive(true);
    }

    private void HideLabel()
    {
        if (RelicNameLabel == null) return;
        RelicNameLabel.gameObject.SetActive(false);
    }

    protected override void OnInteract(SpellCaster caster)
    {
        var relic = RelicPool?.DrawOne();
        if (relic == null)
        {
            Debug.LogWarning("[RelicAltarActivity] Aucune relique disponible (RelicPool vide ou non assignée).");
            return;
        }

        if (AltarAnimator != null) AltarAnimator.SetTrigger("Activate");

        caster.GetComponent<RelicManager>()?.CollectRelic(relic);
        Debug.Log($"[RelicAltarActivity] Relique donnée : {relic.DisplayName}");

        Complete();
    }

    private void OnDrawGizmosSelected()
    {
        var col = GetComponent<Collider>();
        if (col == null) return;
        Gizmos.color = new Color(0.6f, 0.2f, 1f, 0.3f);
        Gizmos.DrawWireCube(transform.position, col.bounds.size);
    }
}
