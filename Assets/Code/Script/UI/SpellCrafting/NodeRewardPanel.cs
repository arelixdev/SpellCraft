using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// Panel de choix de node : met le jeu en pause et propose 3 cartes.
/// Déclenché par la touche P en attendant que fin-de-vague/coffres soient branchés.
public class NodeRewardPanel : MonoBehaviour
{
    [SerializeField] private GameObject _panelRoot;      // désactivé par défaut
    [SerializeField] private Transform  _cardContainer;  // 3 slots, HorizontalLayoutGroup
    [SerializeField] private GameObject _cardPrefab;     // NodeCardRewardView.prefab
    [SerializeField] private Button     _skipButton;     // optionnel ("Passer")
    [SerializeField] private LootPoolSO _testLootPool;   // déclencheur manuel (touche P)

    private readonly List<GameObject> _spawnedCards = new();

    private void Awake()
    {
        if (_panelRoot != null) _panelRoot.SetActive(false);
        _skipButton?.onClick.AddListener(Hide);
    }

    private void Update()
    {
        if (Keyboard.current?.pKey.wasPressedThisFrame != true) return;

        if (_panelRoot != null && _panelRoot.activeSelf)
            Hide();
        else if (_testLootPool != null)
            ShowReward(_testLootPool.DrawThree());
    }

    public void ShowReward(List<SpellNodeSO> choices)
    {
        foreach (var go in _spawnedCards) Destroy(go);
        _spawnedCards.Clear();

        foreach (var node in choices)
        {
            if (node == null || _cardPrefab == null || _cardContainer == null) continue;

            var go = Instantiate(_cardPrefab, _cardContainer);
            go.SetActive(true);
            go.GetComponent<NodeCardRewardView>()?.Init(node, OnNodeChosen);
            _spawnedCards.Add(go);
        }

        Time.timeScale = 0f;
        if (_panelRoot != null) _panelRoot.SetActive(true);
    }

    private void OnNodeChosen(SpellNodeSO node)
    {
        var caster = GameObject.FindWithTag("Player")?.GetComponent<SpellCaster>();
        caster?.CollectNode(node);
        Hide();
    }

    private void Hide()
    {
        Time.timeScale = 1f;
        if (_panelRoot != null) _panelRoot.SetActive(false);
    }
}
