using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

/// <summary>
/// Activité coffre : le joueur entre dans le trigger, appuie sur Espace,
/// et une node aléatoire lui est donnée.
/// </summary>
public class ChestActivity : ActivityBase
{
    // ── Loot ────────────────────────────────────────────────────────────────────
    [BoxGroup("Loot"), LabelText("Loot Pool")]
    public LootPoolSO LootPool;

    // ── Prix ─────────────────────────────────────────────────────────────────────
    [BoxGroup("Prix"), LabelText("Prix de base (or)")]
    public int BaseGoldPrice = 20;

    [BoxGroup("Prix"), LabelText("Croissance du prix par coffre")]
    [Tooltip("Prix = BaseGoldPrice * croissance^(coffres déjà ouverts dans le run)")]
    public float PriceGrowthPerChest = 1.3f;

    [FoldoutGroup("État"), ShowInInspector, ReadOnly, LabelText("Prix actuel")]
    public int CurrentPrice => Mathf.RoundToInt(BaseGoldPrice *
        Mathf.Pow(PriceGrowthPerChest, RunProgress.ChestsOpenedThisRun));

    // ── Visuals ──────────────────────────────────────────────────────────────────
    [BoxGroup("Visuals"), LabelText("Animator (optionnel)")]
    [Tooltip("Doit avoir un trigger 'Open' pour l'animation d'ouverture")]
    public Animator ChestAnimator;

    [BoxGroup("Visuals"), LabelText("Label prix (optionnel)")]
    [Tooltip("Texte affiché au-dessus du coffre quand le joueur entre à portée, indiquant le prix à payer")]
    public TMP_Text PriceLabel;

    protected override void Awake()
    {
        base.Awake();
        OnPlayerNearby += ShowPriceLabel;
        OnPlayerLeft   += HidePriceLabel;
        HidePriceLabel();
    }

    private void ShowPriceLabel(SpellCaster caster)
    {
        if (PriceLabel == null) return;
        PriceLabel.text = $"{CurrentPrice} or";
        PriceLabel.gameObject.SetActive(true);
    }

    private void HidePriceLabel()
    {
        if (PriceLabel == null) return;
        PriceLabel.gameObject.SetActive(false);
    }

    // ── Interaction ───────────────────────────────────────────────────────────────
    protected override void OnInteract(SpellCaster caster)
    {
        int price = CurrentPrice;
        var wallet = caster.GetComponent<PlayerWallet>();
        if (wallet == null || !wallet.TrySpend(price))
        {
            Debug.Log($"[ChestActivity] Or insuffisant pour ouvrir ce coffre ({price} requis).");
            return; // pas de Complete() : le joueur peut retenter une fois qu'il a assez d'or
        }

        if (ChestAnimator != null) ChestAnimator.SetTrigger("Open");

        SpellNodeSO node = LootPool?.DrawOneWithBias(caster.craftingGraph);
        if (node != null)
        {
            caster.CollectNode(node);
            Debug.Log($"[ChestActivity] Node donnée : {node.nodeName}");
        }
        else
        {
            Debug.LogWarning("[ChestActivity] Aucune node disponible (LootPool vide ou non assignée).");
        }

        RunController.Instance?.RegisterChestOpened();
        Complete();
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
