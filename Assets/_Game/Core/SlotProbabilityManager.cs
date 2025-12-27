using UnityEngine;

public enum SlotMode
{
    Normal,
    SevenBoost,   // 例：7の確率アップ用（あとで使う）
    KeyBonus      // 例：🔑の確率アップ用（あとで使う）
}

public class SlotProbabilityManager : MonoBehaviour
{
    [Header("シンボルごとの重み設定")]
    public SymbolConfig[] configs;

    private int totalWeight;
    public SlotMode currentMode = SlotMode.Normal;

    void Awake()
    {
        ResetAllWeightsToBase();
    }

    /// <summary>
    /// 全シンボルを baseWeight にリセット
    /// </summary>
    public void ResetAllWeightsToBase()
    {
        totalWeight = 0;
        foreach (var c in configs)
        {
            c.currentWeight = c.baseWeight;
            totalWeight += c.currentWeight;
        }
    }

    /// <summary>
    /// 特定シンボルだけ倍率をかける（他はbaseに戻す簡易版）
    /// 例：MultiplySymbol(SymbolId.Seven, 2.0f);
    /// </summary>
    public void MultiplySymbol(SymbolId id, float factor)
    {
        totalWeight = 0;
        foreach (var c in configs)
        {
            if (c.id == id)
            {
                c.currentWeight = Mathf.Max(0, Mathf.RoundToInt(c.baseWeight * factor));
            }
            else
            {
                c.currentWeight = c.baseWeight;
            }

            totalWeight += c.currentWeight;
        }
    }

    /// <summary>
    /// モードに応じて重みを再計算（あとで拡張しやすいように用意）
    /// </summary>
    public void ApplyMode(SlotMode mode)
    {
        currentMode = mode;

        // まずベースに戻す
        ResetAllWeightsToBase();

        switch (mode)
        {
            case SlotMode.Normal:
                // 何もしない（baseWeightのまま）
                break;

            case SlotMode.SevenBoost:
                // 例：7だけ2倍にする
                MultiplySymbol(SymbolId.Seven, 2.0f);
                break;

            case SlotMode.KeyBonus:
                // 例：🔑だけ3倍にする
                MultiplySymbol(SymbolId.Key, 3.0f);
                break;
        }
    }

    /// <summary>
    /// 重み付きランダムで1シンボルを選ぶ
    /// </summary>
public SymbolId GetRandomSymbolId()
    {
        if (totalWeight <= 0)
        {
            Debug.LogWarning("totalWeight が 0 以下です。configの設定を確認してください。");
            return configs[0].id;
        }

        int r = Random.Range(0, totalWeight);

        foreach (var c in configs)
        {
            if (r < c.currentWeight)
            {
                return c.id;
            }
            r -= c.currentWeight;
        }

        // フォールバック（理論上ここには来ない）
        return configs[configs.Length - 1].id;
    }

	public void ApplyItemMultipliers(System.Func<SymbolId, float> getMultiplier)
{
    totalWeight = 0;

    foreach (var c in configs)
    {
        float m = getMultiplier != null ? getMultiplier(c.id) : 1f;
        float v = c.baseWeight * m;
        c.currentWeight = Mathf.Max(0, Mathf.RoundToInt(v));
        totalWeight += c.currentWeight;
    }
}


}
