using UnityEngine;


public enum ItemId
{
    // 例：後で増やす
    LuckyCharm,
    DoubleSeven,
    KeyMagnet,
}

public abstract class ItemDefinition : ScriptableObject
{
    [Header("Identity")]
    public ItemId id;

    [Header("UI")]
    public string displayName;
    [TextArea(2, 6)] public string description;
    public Sprite icon;

    [Header("Rarity")]
    public ItemRarity rarity = ItemRarity.Common;

    /// <summary>アイテムの効果ロジック（具象クラスで実装）</summary>
    public abstract IItemEffect CreateEffect();

    // 個別調整用（0以下なら rarity ベース）
    [Min(0f)]
    public float dropWeight = 0f;

    public float GetEffectiveDropWeight()
    {
        // 個別指定があればそれを優先
        if (dropWeight > 0f) return dropWeight;

        // rarity ベースのデフォルト重み
        switch (rarity)
        {
            case ItemRarity.Common:     return 100f;
            case ItemRarity.Rare:       return 30f;
            case ItemRarity.Epic:       return 8f;
            case ItemRarity.Legendary:  return 1f;
            default:                    return 10f;
        }


    }

}

