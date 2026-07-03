using System;
using UnityEngine;

public class PlayerWallet : MonoBehaviour
{
    [SerializeField] private int _startingGold = 0;

    private int _gold;

    public int Gold => _gold;

    public event Action<int> OnGoldChanged;

    private void Awake() => _gold = _startingGold;

    public void AddGold(int amount)
    {
        if (amount <= 0) return;

        _gold += amount;
        Debug.Log($"[PlayerWallet] +{amount} or | Total: {_gold}");
        OnGoldChanged?.Invoke(_gold);
    }

    public bool TrySpend(int amount)
    {
        if (amount <= 0) return true;
        if (_gold < amount) return false;

        _gold -= amount;
        Debug.Log($"[PlayerWallet] -{amount} or | Total: {_gold}");
        OnGoldChanged?.Invoke(_gold);
        return true;
    }
}
