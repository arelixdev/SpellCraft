using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Écran titre : Play/Continuer chargent CharacterSelect (pas de distinction pour l'instant,
/// aucun système de sauvegarde n'existe encore — les deux démarrent une run neuve), Options
/// ouvre le panneau partagé (OptionsPanelController), Quitter ferme le jeu.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [SerializeField] private string _gameplayEntrySceneName = "CharacterSelect";
    [SerializeField] private OptionsPanelController _optionsPanel;

    [SerializeField] private Button _playButton;
    [SerializeField] private Button _continueButton;
    [SerializeField] private Button _optionsButton;
    [SerializeField] private Button _quitButton;

    private void Awake()
    {
        if (_playButton != null) _playButton.onClick.AddListener(PlayOrContinue);
        if (_continueButton != null) _continueButton.onClick.AddListener(PlayOrContinue);
        if (_optionsButton != null) _optionsButton.onClick.AddListener(OpenOptions);
        if (_quitButton != null) _quitButton.onClick.AddListener(Quit);
    }

    private void PlayOrContinue()
    {
        ScreenFader.LoadScene(_gameplayEntrySceneName);
    }

    private void OpenOptions()
    {
        if (_optionsPanel != null) _optionsPanel.Open();
    }

    private void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
