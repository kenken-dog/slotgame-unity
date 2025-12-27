public interface IItemEffect
{
    /// <summary>
    /// 次のスピンに適用する「重み倍率」を返す。
    /// 対象のシンボル以外は 1.0 を返す。
    /// </summary>
    float GetWeightMultiplier(SymbolId symbol);

    /// <summary>UI表示用（必要なら）</summary>
    string DebugName { get; }
}
