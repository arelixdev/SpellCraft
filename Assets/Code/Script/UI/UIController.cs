using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject _gameOverPanel;
    [SerializeField] private GameObject _victoryPanel;

    [Header("Options")]
    [SerializeField] private Button _optionsButton;
    [SerializeField] private OptionsPanelController _optionsPanel;

    [Header("Health HUD")]
    [SerializeField] private Image   _healthBarFill;
    [SerializeField] private TMP_Text _healthLabel;

    [Header("Gold HUD")]
    [SerializeField] private TMP_Text _goldLabel;

    private PlayerHealth _playerHealth;
    private PlayerWallet _playerWallet;

    private void Awake()
    {
        if (_gameOverPanel != null)
            _gameOverPanel.SetActive(false);

        if (_victoryPanel != null)
            _victoryPanel.SetActive(false);

        if (_optionsButton != null && _optionsPanel != null)
            _optionsButton.onClick.AddListener(_optionsPanel.Open);
    }

    private void Start()
    {
        StartCoroutine(WaitForPlayer());

        if (RunController.Instance != null)
            RunController.Instance.OnVictory.AddListener(ShowVictory);
    }

    private void OnDisable()
    {
        if (_playerHealth != null)
        {
            _playerHealth.OnDied          -= ShowGameOver;
            _playerHealth.OnHealthChanged -= UpdateHealthHUD;
        }

        if (_playerWallet != null)
            _playerWallet.OnGoldChanged -= UpdateGoldHUD;

        if (RunController.Instance != null)
            RunController.Instance.OnVictory.RemoveListener(ShowVictory);
    }

    private IEnumerator WaitForPlayer()
    {
        while (_playerHealth == null)
        {
            var playerGO = GameObject.FindWithTag("Player");
            if (playerGO != null)
            {
                _playerHealth = playerGO.GetComponent<PlayerHealth>();
                _playerWallet = playerGO.GetComponent<PlayerWallet>();
            }

            yield return null;
        }

        _playerHealth.OnDied          += ShowGameOver;
        _playerHealth.OnHealthChanged += UpdateHealthHUD;

        UpdateHealthHUD(_playerHealth.CurrentHealth, _playerHealth.MaxHealth);

        if (_playerWallet != null)
        {
            _playerWallet.OnGoldChanged += UpdateGoldHUD;
            UpdateGoldHUD(_playerWallet.Gold);
        }
    }

    private void UpdateGoldHUD(int gold)
    {
        if (_goldLabel != null)
            _goldLabel.text = $"{gold}";
    }

    private enum HealthLabelMode { Hidden, CurrentMax, Percent }
    private HealthLabelMode _healthLabelMode = HealthLabelMode.Hidden;

    public void CycleHealthLabel()
    {
        _healthLabelMode = (HealthLabelMode)(((int)_healthLabelMode + 1) % 3);
        RefreshHealthLabel();
    }

    private void UpdateHealthHUD(float current, float max)
    {
        float ratio = max > 0f ? current / max : 0f;

        if (_healthBarFill != null)
            _healthBarFill.fillAmount = ratio;

        RefreshHealthLabel();
    }

    private void RefreshHealthLabel()
    {
        if (_healthLabel == null || _playerHealth == null) return;

        _healthLabel.text = _healthLabelMode switch
        {
            HealthLabelMode.Hidden     => "",
            HealthLabelMode.CurrentMax => $"{Mathf.RoundToInt(_playerHealth.CurrentHealth)}\n<size=60%>   /{Mathf.RoundToInt(_playerHealth.MaxHealth)}</size>",
            HealthLabelMode.Percent    => $"{Mathf.RoundToInt(_playerHealth.HealthRatio * 100f)}%",
            _                          => "",
        };
    }

    private void ShowGameOver()
    {
        if (_gameOverPanel != null)
            _gameOverPanel.SetActive(true);
    }

    private void ShowVictory()
    {
        if (_victoryPanel != null)
            _victoryPanel.SetActive(true);
    }
}
