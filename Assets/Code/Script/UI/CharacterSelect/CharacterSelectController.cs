using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Écran de sélection du robot en début de run : tire un robot aléatoire (stats + nom)
/// par RobotArchetypeSO via Roll(), instancie une carte par robot tiré, et charge la
/// scène Gameplay avec RobotSelection.Chosen renseigné quand le joueur en choisit un.
/// </summary>
public class CharacterSelectController : MonoBehaviour
{
    [SerializeField] private RobotArchetypeSO[] _archetypes;
    [SerializeField] private RobotNamePoolSO    _namePool;
    [SerializeField] private RobotCardView      _cardPrefab;
    [SerializeField] private Transform          _cardContainer;
    [SerializeField] private string             _gameplaySceneName = "Gameplay";

    private void Start()
    {
        if (_cardPrefab == null || _cardContainer == null) return;

        foreach (var archetype in _archetypes)
        {
            if (archetype == null) continue;

            var rolledName = _namePool != null ? _namePool.GetRandom() : archetype.ArchetypeName;
            var robot      = archetype.Roll(rolledName);

            var card = Instantiate(_cardPrefab, _cardContainer);
            card.Setup(robot, ChooseRobot);
        }
    }

    private void ChooseRobot(RobotDefinitionSO robot)
    {
        RobotSelection.Chosen = robot;
        SceneManager.LoadScene(_gameplaySceneName);
    }
}
