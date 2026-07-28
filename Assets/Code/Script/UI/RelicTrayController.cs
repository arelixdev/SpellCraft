using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Bandeau HUD listant les reliques ramassées ce run-ci : une icône PAR TYPE de relique
// (pas par exemplaire) avec un compteur "xN" quand plusieurs copies ont été ramassées,
// pour ne pas alourdir l'UI. Le Player et son RelicManager survivent au rechargement de
// scène (PlayerPersistence) mais cette UI, elle, est recréée à chaque (re)chargement de
// Gameplay — WaitForPlayer() resynchronise donc depuis RelicManager.CollectedRelics au
// lieu de compter uniquement sur l'event, sous peine d'un bandeau vide après un
// changement de niveau en cours de run.
public class RelicTrayController : MonoBehaviour
{
    [SerializeField] private Transform      _iconContainer;
    [SerializeField] private RelicIconView  _iconPrefab;

    private RelicManager _relicManager;

    private readonly Dictionary<RelicSO, RelicIconView> _views  = new();
    private readonly Dictionary<RelicSO, int>            _counts = new();

    private void OnEnable() => StartCoroutine(WaitForPlayer());

    private void OnDisable()
    {
        if (_relicManager != null)
            _relicManager.OnRelicCollected -= AddRelic;
    }

    private IEnumerator WaitForPlayer()
    {
        while (_relicManager == null)
        {
            var playerGO = GameObject.FindWithTag("Player");
            if (playerGO != null)
                _relicManager = playerGO.GetComponent<RelicManager>();

            yield return null;
        }

        _relicManager.OnRelicCollected += AddRelic;

        foreach (var relic in _relicManager.CollectedRelics)
            AddRelic(relic);
    }

    private void AddRelic(RelicSO relic)
    {
        if (_iconContainer == null || _iconPrefab == null || relic == null) return;

        int count = _counts.GetValueOrDefault(relic) + 1;
        _counts[relic] = count;

        if (!_views.TryGetValue(relic, out var view))
        {
            view = Instantiate(_iconPrefab, _iconContainer);
            if (view.Icon != null) view.Icon.sprite = relic.Icon;
            view.gameObject.name = $"Relic_{relic.DisplayName}";
            view.gameObject.SetActive(true);
            _views[relic] = view;
        }

        if (view.CountLabel != null)
            view.CountLabel.text = count > 1 ? $"x{count}" : "";
    }
}
