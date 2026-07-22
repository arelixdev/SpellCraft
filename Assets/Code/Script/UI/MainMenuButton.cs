using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuButton : MonoBehaviour
{
    [SerializeField] private string _mainMenuSceneName = "MainMenu";

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(_mainMenuSceneName);
    }
}
