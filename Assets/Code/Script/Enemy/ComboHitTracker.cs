using System.Collections.Generic;
using UnityEngine;

public class ComboHitTracker : MonoBehaviour
{
    private readonly Dictionary<int, int> _counters = new();

    // Returns true when the hit count for this threshold is reached (and resets).
    public bool RegisterHit(int threshold)
    {
        _counters.TryGetValue(threshold, out int count);
        count++;
        if (count >= threshold)
        {
            _counters[threshold] = 0;
            return true;
        }
        _counters[threshold] = count;
        return false;
    }

    public void ResetAll() => _counters.Clear();
}
