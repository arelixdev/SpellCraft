using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

/// <summary>
/// Variante de l'autel de relique : au lieu de donner une relique aléatoire directement
/// (RelicAltarActivity), tire 3 reliques et ouvre RelicRewardPanel pour laisser le joueur
/// choisir (met le jeu en pause le temps du choix).
/// </summary>
public class RelicChoiceAltarActivity : ActivityBase
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
        RelicNameLabel.text = "Espace : choisir une relique";
        RelicNameLabel.gameObject.SetActive(true);
    }

    private void HideLabel()
    {
        if (RelicNameLabel == null) return;
        RelicNameLabel.gameObject.SetActive(false);
    }

    protected override void OnInteract(SpellCaster caster)
    {
        var choices = RelicPool?.DrawThree();
        if (choices == null || choices.Count == 0)
        {
            Debug.LogWarning("[RelicChoiceAltarActivity] Aucune relique disponible (RelicPool vide ou non assignée).");
            return;
        }

        if (AltarAnimator != null) AltarAnimator.SetTrigger("Activate");

        RelicRewardPanel.Instance?.ShowReward(choices);
        Complete();
    }

    private void OnDrawGizmosSelected()
    {
        var col = GetComponent<Collider>();
        if (col == null) return;
        Gizmos.color = new Color(1f, 0.8f, 0.1f, 0.3f);
        Gizmos.DrawWireCube(transform.position, col.bounds.size);
    }
}
