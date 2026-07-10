using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Une carte de l'écran de sélection : affiche un RobotDefinitionSO et notifie
/// CharacterSelectController quand le joueur clique "Choisir".
/// </summary>
public class RobotCardView : MonoBehaviour
{
    [SerializeField] private TMP_Text _nameLabel;
    [SerializeField] private TMP_Text _descriptionLabel;
    [SerializeField] private TMP_Text _statsLabel;
    [SerializeField] private TMP_Text _slotsLabel;
    [SerializeField] private Button   _chooseButton;

    private RobotDefinitionSO _robot;
    private Action<RobotDefinitionSO> _onChosen;

    public void Setup(RobotDefinitionSO robot, Action<RobotDefinitionSO> onChosen)
    {
        _robot    = robot;
        _onChosen = onChosen;

        if (_nameLabel != null) _nameLabel.text = robot.DisplayName;
        if (_descriptionLabel != null) _descriptionLabel.text = robot.Description;

        if (_statsLabel != null)
        {
            _statsLabel.text =
                $"PV max : {robot.BaseMaxHealth:0}\n" +
                $"Vitesse : {robot.BaseMoveSpeed:0.#}\n" +
                $"Crit : {robot.BaseCritChance * 100f:0}% x{robot.BaseCritMultiplier:0.#}\n" +
                $"Or de départ : {robot.StartingGold}";
        }

        if (_slotsLabel != null) _slotsLabel.text = BuildSlotsText(robot.SpellSlots);

        if (_chooseButton != null)
        {
            _chooseButton.onClick.RemoveAllListeners();
            _chooseButton.onClick.AddListener(() => _onChosen?.Invoke(_robot));
        }
    }

    // Affiche exactement le loadout déjà tiré par RobotArchetypeSO.Roll() (slots,
    // launcher, sort de départ) — aucun recalcul ici, ce qui est montré est ce qui
    // sera appliqué tel quel par RobotLoader au démarrage du gameplay.
    private static string BuildSlotsText(SpellSlot[] slots)
    {
        if (slots == null || slots.Length == 0) return "Slots : aucun";

        var sb = new StringBuilder();
        sb.Append($"Slots ({slots.Length}) :");

        for (int i = 0; i < slots.Length; i++)
        {
            var slot        = slots[i];
            string launcher = slot?.launcherConfig != null
                ? $"{slot.launcherConfig.launcherName} ({slot.launcherConfig.launcherType})"
                : "?";
            string spell = slot?.connectedSpell != null ? slot.connectedSpell.name : "vide";

            sb.Append($"\n {i + 1}. {launcher} — {spell}");
        }

        return sb.ToString();
    }
}
