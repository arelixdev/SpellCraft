using UnityEngine;

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

        for (int i = 0; i < _archetypes.Length; i++)
        {
            var archetype = _archetypes[i];
            if (archetype == null) continue;

            var rolledName = _namePool != null ? _namePool.GetRandom() : archetype.ArchetypeName;
            var robot      = archetype.Roll(rolledName);

            var card = Instantiate(_cardPrefab, _cardContainer);
            card.Setup(robot, ChooseRobot, i);
        }
    }

    private void ChooseRobot(RobotDefinitionSO robot)
    {
        RobotSelection.Chosen = robot;
        RunProgress.ResetRun();

        // Le Player est en DontDestroyOnLoad (PlayerPersistence), donc son Awake() ne se
        // relance pas au choix d'un nouveau robot : il faut réappliquer explicitement les
        // stats/sorts du robot choisi, puis réanimer le Player s'il était mort lors du run
        // précédent (ordre important : ResetForNewRun se base sur le maxHealth à jour).
        var playerGO = GameObject.FindWithTag("Player");
        if (playerGO != null)
        {
            playerGO.GetComponent<RobotLoader>()?.ApplyRobotForNewRun(robot);
            playerGO.GetComponent<PlayerHealth>()?.ResetForNewRun();
        }

        ScreenFader.LoadScene(_gameplaySceneName);
    }
}
