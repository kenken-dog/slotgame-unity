using UnityEngine;

public enum SymbolId
{
    Seven,      // 7
    Key,        // 🔑
    A,          // A
    Diamond,    // 💎
    Bar,        // BAR
    Coin,       // 💰
    Watermelon, // 🍉
    Bell,       // 🔔
    Cherry,     // 🍒
    Clover      // 🍀
}

[System.Serializable]
public class SymbolConfig
{
    public SymbolId id;

    [Tooltip("通常時の基準重み（大きいほど出やすい）")]
    public int baseWeight;

    [HideInInspector]
    public int currentWeight;
}
