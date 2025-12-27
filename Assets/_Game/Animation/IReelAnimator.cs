using System;
using System.Collections;

public interface IReelAnimator
{
    /// <summary>
    /// finalGrid に向かって表示を回転させて着地させる（A方式はコマ送りシフト）。
    /// getRollingSymbol は回転中の見せかけ用に使う（案A：確率抽選を使う）。
    /// </summary>
    IEnumerator Play(SymbolId[,] finalGrid, Func<SymbolId> getRollingSymbol);
}
