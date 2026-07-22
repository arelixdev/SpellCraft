using UnityEngine;

public class MainMenuButton : MonoBehaviour
{
    [SerializeField] private string _mainMenuSceneName = "MainMenu";

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        ScreenFader.LoadScene(_mainMenuSceneName);
    }
}
