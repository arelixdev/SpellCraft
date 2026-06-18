using System.Collections;
using UnityEngine;

public class UIController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject _gameOverPanel;

    private PlayerHealth _playerHealth;

    private void Awake()
    {
        if (_gameOverPanel != null)
            _gameOverPanel.SetActive(false);
    }

    private void Start()
    {
        StartCoroutine(WaitForPlayer());
    }

    private void OnDisable()
    {
        if (_playerHealth != null)
            _playerHealth.OnDied -= ShowGameOver;
    }

    private IEnumerator WaitForPlayer()
    {
        while (_playerHealth == null)
        {
            var playerGO = GameObject.FindWithTag("Player");
            if (playerGO != null)
                _playerHealth = playerGO.GetComponent<PlayerHealth>();

            yield return null;
        }

        _playerHealth.OnDied += ShowGameOver;
    }

    private void ShowGameOver()
    {
        if (_gameOverPanel != null)
            _gameOverPanel.SetActive(true);
    }
}
