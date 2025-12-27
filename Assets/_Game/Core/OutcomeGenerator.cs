using System;
using System.Collections.Generic;
using UnityEngine;

public class OutcomeGenerator
{
    private const int Rows = SlotMachineCore.Rows; // 4
    private const int Cols = SlotMachineCore.Cols; // 7

    // ライン方向（Evaluateに合わせる）
    private static readonly (int dy, int dx)[] Directions = new (int, int)[]
    {
        (0, 1),   // 横
        (1, 0),   // 縦
        (1, 1),   // 右下
        (-1, 1)   // 右上
    };

    // ---- 調整パラメータ（まずは固定。後でInspector化推奨） ----
    public float winChance = 0.35f; // 今回当たりを作る確率

    // 何ライン当てるか（例：1が多い）
    private readonly int[] lineCountOptions = { 1, 2, 3 };
    private readonly int[] lineCountWeights = { 80, 18, 2 };

    // 連続数（3が多い）
    private readonly int[] streakOptions = { 3, 4, 5 };
    private readonly int[] streakWeights = { 80, 18, 2 };

    // 当たりラインに載せる役の重み（ここが肝：7/Keyを上げられる）
    private readonly List<SymbolId> winSymbolOptions = new List<SymbolId>
    {
        SymbolId.Seven, SymbolId.Key,
        SymbolId.A, SymbolId.Diamond,
        SymbolId.Bar, SymbolId.Coin, SymbolId.Watermelon,
        SymbolId.Bell, SymbolId.Cherry, SymbolId.Clover
    };

    // 例：7/Keyを「当たりに限って」体感出るようにする
    private readonly List<int> winSymbolWeights = new List<int>
    {
        6, 6,     // Seven, Key  ← ここを上げれば当たり時に出やすい
        18, 18,   // A, Diamond
        22, 22, 22, // BAR, Coin, Watermelon
        30, 30, 30  // Bell, Cherry, Clover
    };

    /// <summary>
    /// 新方式で finalGrid を生成する
    /// </summary>
    public SymbolId[,] GenerateFinalGrid(SlotProbabilityManager pm)
    {
        var grid = new SymbolId[Rows, Cols];

        // 1) まず通常抽選で全マスを埋める（ベース）
        for (int y = 0; y < Rows; y++)
            for (int x = 0; x < Cols; x++)
                grid[y, x] = pm.GetRandomSymbolId();

        // 2) 当たりを作るか？
        if (UnityEngine.Random.value >= winChance)
            return grid;

        int lineCount = WeightedChoice.Choose(lineCountOptions, lineCountWeights);
        var occupied = new bool[Rows, Cols]; // 当たりで使用済みセル（交差を避ける）

        for (int i = 0; i < lineCount; i++)
        {
            int streak = WeightedChoice.Choose(streakOptions, streakWeights);
            SymbolId sym = WeightedChoice.Choose(winSymbolOptions, winSymbolWeights);

            if (!TryPlaceLine(grid, occupied, streak, sym))
            {
                // 置けなかったら諦め（またはリトライ回数を増やす）
                continue;
            }
        }

        return grid;
    }

    private bool TryPlaceLine(SymbolId[,] grid, bool[,] occupied, int streak, SymbolId sym)
    {
        // 何回か試行して置けるラインを探す
        for (int attempt = 0; attempt < 50; attempt++)
        {
            var dir = Directions[UnityEngine.Random.Range(0, Directions.Length)];
            int dy = dir.dy;
            int dx = dir.dx;

            // 開始点の範囲を決める（streakが盤面内に収まるように）
            // xは右方向に伸びるので 0..Cols-streak
            int xMin = 0;
            int xMax = Cols - streak;

            // yはdyによって範囲が変わる
            int yMin, yMax;
            if (dy == 0) { yMin = 0; yMax = Rows - 1; }
            else if (dy == 1) { yMin = 0; yMax = Rows - streak; }
            else { // dy == -1
                yMin = streak - 1;
                yMax = Rows - 1;
            }

            if (xMax < xMin || yMax < yMin) continue;

            int sx = UnityEngine.Random.Range(xMin, xMax + 1);
            int sy = UnityEngine.Random.Range(yMin, yMax + 1);

            // 交差チェック（occupiedを避ける。交差許可したいならこの条件を緩める）
            bool ok = true;
            int y = sy, x = sx;
            for (int k = 0; k < streak; k++)
            {
                if (occupied[y, x]) { ok = false; break; }
                y += dy; x += dx;
            }
            if (!ok) continue;

            // 置く
            y = sy; x = sx;
            for (int k = 0; k < streak; k++)
            {
                grid[y, x] = sym;
                occupied[y, x] = true;
                y += dy; x += dx;
            }
            return true;
        }

        return false;
    }
}
