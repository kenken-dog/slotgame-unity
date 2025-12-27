using System;
using System.Collections.Generic;
using UnityEngine;

public static class WeightedChoice
{
    public static int ChooseIndex(IReadOnlyList<int> weights)
    {
        int total = 0;
        for (int i = 0; i < weights.Count; i++) total += Mathf.Max(0, weights[i]);
        if (total <= 0) return 0;

        int r = UnityEngine.Random.Range(0, total);
        int acc = 0;
        for (int i = 0; i < weights.Count; i++)
        {
            acc += Mathf.Max(0, weights[i]);
            if (r < acc) return i;
        }
        return weights.Count - 1;
    }

    public static T Choose<T>(IReadOnlyList<T> items, IReadOnlyList<int> weights)
    {
        if (items == null || weights == null || items.Count != weights.Count || items.Count == 0)
            throw new ArgumentException("items/weights invalid");

        return items[ChooseIndex(weights)];
    }
}
