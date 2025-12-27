using System.Collections.Generic;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    [Header("All Item Definitions (unique)")]
    public List<ItemDefinition> allItems = new List<ItemDefinition>();

    private readonly ItemInventory _inventory = new ItemInventory();

    // 実行時に使う「効果」のリスト
    private readonly List<IItemEffect> _effects = new List<IItemEffect>();

    // 参照用：id -> definition
    private Dictionary<ItemId, ItemDefinition> _defs;

    void Awake()
    {
        BuildDictionary();
    }

    private void BuildDictionary()
    {
        _defs = new Dictionary<ItemId, ItemDefinition>();
        foreach (var def in allItems)
        {
            if (def == null) continue;
            _defs[def.id] = def;
        }
    }

    public bool Has(ItemId id) => _inventory.Has(id);

    public ItemDefinition GetDefinition(ItemId id)
    {
        if (_defs != null && _defs.TryGetValue(id, out var def)) return def;
        return null;
    }

    /// <summary>
    /// 未所持の中からランダムに1つ付与（重複なし）
    /// </summary>
    public bool TryGiveRandomItem(out ItemId newItem)
    {
        newItem = default;

        // 未所持のみ抽出
        List<ItemDefinition> candidates = new List<ItemDefinition>();
        foreach (var def in allItems)
        {
            if (def == null) continue;
            if (!_inventory.Has(def.id)) candidates.Add(def);
        }

        if (candidates.Count == 0) return false;

        // 重み合計
        float total = 0f;
        for (int i = 0; i < candidates.Count; i++)
        {
            float w = Mathf.Max(0f, candidates[i].GetEffectiveDropWeight());
            total += w;
        }

        // 全部0なら均等抽選にフォールバック
        ItemDefinition picked;
        if (total <= 0f)
        {
            picked = candidates[Random.Range(0, candidates.Count)];
        }
        else
        {
            float r = Random.Range(0f, total);
            float acc = 0f;

            picked = candidates[candidates.Count - 1]; // 保険
            for (int i = 0; i < candidates.Count; i++)
            {
                acc += Mathf.Max(0f, candidates[i].GetEffectiveDropWeight());
                if (r <= acc)
                {
                    picked = candidates[i];
                    break;
                }
            }
        }

        if (!_inventory.TryAdd(picked.id)) return false;

        var effect = picked.CreateEffect();
        if (effect != null) _effects.Add(effect);

        newItem = picked.id;
        return true;
    }

    public bool TryGiveRandomItemByMinRarity(ItemRarity minRarity, out ItemId newItem)
    {
        newItem = default;

        // 未所持 + rarity条件で候補抽出
        List<ItemDefinition> candidates = new List<ItemDefinition>();
        foreach (var def in allItems)
        {
            if (def == null) continue;
            if (_inventory.Has(def.id)) continue;
            if (def.rarity < minRarity) continue;

            candidates.Add(def);
        }

        if (candidates.Count == 0) return false;

        // 重み付き抽選（GetEffectiveDropWeight を使う前提）
        float total = 0f;
        for (int i = 0; i < candidates.Count; i++)
            total += Mathf.Max(0f, candidates[i].GetEffectiveDropWeight());

        ItemDefinition picked;
        if (total <= 0f)
        {
            picked = candidates[Random.Range(0, candidates.Count)];
        }
        else
        {
            float r = Random.Range(0f, total);
            float acc = 0f;
            picked = candidates[candidates.Count - 1]; // 保険

            for (int i = 0; i < candidates.Count; i++)
            {
                acc += Mathf.Max(0f, candidates[i].GetEffectiveDropWeight());
                if (r <= acc)
                {
                    picked = candidates[i];
                    break;
                }
            }
        }

        if (!_inventory.TryAdd(picked.id)) return false;

        var effect = picked.CreateEffect();
        if (effect != null) _effects.Add(effect);

        newItem = picked.id;
        return true;
    }

    /// <summary>
    /// SlotProbabilityManager に渡す用：シンボルごとの倍率を合成して返す
    /// </summary>
    public float GetWeightMultiplier(SymbolId symbol)
    {
        float mul = 1f;
        for (int i = 0; i < _effects.Count; i++)
        {
            mul *= _effects[i].GetWeightMultiplier(symbol);
        }
        return mul;
    }

    /// <summary>UI表示用：所持アイテムの定義を列挙</summary>
    public IEnumerable<ItemDefinition> GetOwnedDefinitions()
    {
        foreach (var id in _inventory.GetAllOwned())
        {
            var def = GetDefinition(id);
            if (def != null) yield return def;
        }
    }
}
