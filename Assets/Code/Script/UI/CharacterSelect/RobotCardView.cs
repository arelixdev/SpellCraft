using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

/// <summary>
/// Une carte de l'écran de sélection : affiche un RobotDefinitionSO et notifie
/// CharacterSelectController quand le joueur clique "Choisir". Les libellés (stats,
/// slots) sont composés en code plutôt que posés dans l'Inspector, donc ils passent
/// par la table "Robot Card" au lieu d'un LocalizeStringEvent.
/// </summary>
public class RobotCardView : MonoBehaviour
{
    private const string TableName = "Robot Card";

    [SerializeField] private TMP_Text _nameLabel;
    [SerializeField] private TMP_Text _descriptionLabel;
    [SerializeField] private TMP_Text _statsLabel;
    [SerializeField] private TMP_Text _slotsLabel;
    [SerializeField] private Button   _chooseButton;

    private RobotDefinitionSO _robot;
    private Action<RobotDefinitionSO> _onChosen;

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    public void Setup(RobotDefinitionSO robot, Action<RobotDefinitionSO> onChosen)
    {
        _robot    = robot;
        _onChosen = onChosen;

        Refresh();

        if (_chooseButton != null)
        {
            _chooseButton.onClick.RemoveAllListeners();
            _chooseButton.onClick.AddListener(() => _onChosen?.Invoke(_robot));
        }
    }

    // Le texte est composé une fois dans Setup() puis rejoué à l'identique ici : contrairement
    // au reste de l'UI (LocalizeStringEvent posé dans l'Inspector), rien ne se met à jour tout
    // seul quand la langue change, donc on doit reconstruire le texte à la main.
    private void OnLocaleChanged(Locale _)
    {
        if (_robot != null) Refresh();
    }

    private void Refresh()
    {
        if (_nameLabel != null) _nameLabel.text = _robot.DisplayName;
        if (_descriptionLabel != null) _descriptionLabel.text = ResolveDescription(_robot);
        if (_statsLabel != null) _statsLabel.text = BuildStatsText(_robot);
        if (_slotsLabel != null) _slotsLabel.text = BuildSlotsText(_robot.SpellSlots);
    }

    private static string ResolveDescription(RobotDefinitionSO robot)
        => robot.Description.IsEmpty ? string.Empty : robot.Description.GetLocalizedString();

    private static string BuildStatsText(RobotDefinitionSO robot)
    {
        var sb = new StringBuilder();
        sb.Append(Localize("stats.pvMax", robot.BaseMaxHealth));
        sb.Append('\n').Append(Localize("stats.vitesse", robot.BaseMoveSpeed));
        sb.Append('\n').Append(Localize("stats.crit", robot.BaseCritChance * 100f, robot.BaseCritMultiplier));
        sb.Append('\n').Append(Localize("stats.or", robot.StartingGold));
        return sb.ToString();
    }

    // Affiche exactement le loadout déjà tiré par RobotArchetypeSO.Roll() (slots,
    // launcher, sort de départ) — aucun recalcul ici, ce qui est montré est ce qui
    // sera appliqué tel quel par RobotLoader au démarrage du gameplay.
    private static string BuildSlotsText(SpellSlot[] slots)
    {
        if (slots == null || slots.Length == 0) return Localize("slots.none");

        var sb = new StringBuilder();
        sb.Append(Localize("slots.header", slots.Length)).Append('\n');

        // <line-height> resserre l'espace vide sous l'en-tête puis l'élargit entre les sorts —
        // un m_lineSpacing global sur le TMP_Text ne peut pas viser ces deux écarts séparément.
        sb.Append("<line-height=50%>\n</line-height><line-height=135%>");

        for (int i = 0; i < slots.Length; i++)
        {
            var slot        = slots[i];
            string launcher = slot?.launcherConfig != null ? slot.launcherConfig.launcherName : Localize("slots.unknownLauncher");
            string spell    = slot?.connectedSpell != null ? slot.connectedSpell.name : Localize("slots.emptySpell");

            if (i > 0) sb.Append('\n');
            sb.Append(' ').Append(Localize("slots.entry", i + 1, launcher, spell));
        }

        return sb.ToString();
    }

    private static string Localize(string key, params object[] arguments)
        => LocalizationSettings.StringDatabase.GetLocalizedString(TableName, key, arguments);
}
