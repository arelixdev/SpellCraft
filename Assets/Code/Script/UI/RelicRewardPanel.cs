using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// Panel de choix de relique : met le jeu en pause et propose 3 cartes, sur le modèle de
/// NodeRewardPanel. Instance résolue par les activités (RelicChoiceAltarActivity), qui
/// sont instanciées dynamiquement par ActivityManager et ne peuvent donc pas avoir de
/// référence câblée dans l'Inspector — même pattern que RunController.Instance.
public class RelicRewardPanel : MonoBehaviour
{
    public static RelicRewardPanel Instance { get; private set; }

    [SerializeField] private GameObject _panelRoot;      // désactivé par défaut
    [SerializeField] private Transform  _cardContainer;  // 3 slots, HorizontalLayoutGroup
    [SerializeField] private GameObject _cardPrefab;     // RelicCardRewardView.prefab
    [SerializeField] private Button     _skipButton;     // optionnel ("Passer")

    private readonly List<GameObject> _spawnedCards = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (_panelRoot != null) _panelRoot.SetActive(false);
        _skipButton?.onClick.AddListener(Hide);
    }

    public void ShowReward(List<RelicSO> choices)
    {
        foreach (var go in _spawnedCards) Destroy(go);
        _spawnedCards.Clear();

        foreach (var relic in choices)
        {
            if (relic == null || _cardPrefab == null || _cardContainer == null) continue;

            var go = Instantiate(_cardPrefab, _cardContainer);
            go.SetActive(true);
            go.GetComponent<RelicCardRewardView>()?.Init(relic, OnRelicChosen);
            _spawnedCards.Add(go);
        }

        Time.timeScale = 0f;
        if (_panelRoot != null) _panelRoot.SetActive(true);
    }

    private void OnRelicChosen(RelicSO relic)
    {
        var relicManager = GameObject.FindWithTag("Player")?.GetComponent<RelicManager>();
        relicManager?.CollectRelic(relic);
        Hide();
    }

    private void Hide()
    {
        Time.timeScale = 1f;
        if (_panelRoot != null) _panelRoot.SetActive(false);
    }
}
