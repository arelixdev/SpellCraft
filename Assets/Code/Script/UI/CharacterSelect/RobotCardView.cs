using System;
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

        if (_chooseButton != null)
        {
            _chooseButton.onClick.RemoveAllListeners();
            _chooseButton.onClick.AddListener(() => _onChosen?.Invoke(_robot));
        }
    }
}
