using System.Text;
using System.Collections.Generic;
using UnityEngine;

public class SlotMachineCore
{
    public const int Rows = 4;
    public const int Cols = 7;

    public struct WinLine
    {
        public Vector2Int start; // (col, row)
        public Vector2Int dir;   // (dx, dy)
        public int length;
    }

    public struct EvalResult
    {
        public bool[,] winMask;
        public bool keyHit;
        public int totalWin;
        public string message;

        public List<WinLine> winLines;

        public bool isJackpot;
    }

    private static int CalcJackpotPayout(SymbolId symbol)
    {
        if (symbol == SymbolId.Seven) return 5000;
        if (symbol == SymbolId.Key)   return 3500;

        if (symbol == SymbolId.A || symbol == SymbolId.Diamond) return 2000;
        if (symbol == SymbolId.Bar || symbol == SymbolId.Coin || symbol == SymbolId.Watermelon) return 1200;
        return 700;
    }

    /// <summary>
    /// 最終出目（finalGrid）を確定する。確率は呼び出し側で適用済み（アイテム込み）を想定。
    /// </summary>
    public SymbolId[,] GenerateFinalGrid(SlotProbabilityManager probabilityManager)
    {
        var grid = new SymbolId[Rows, Cols];
        for (int y = 0; y < Rows; y++)
        {
            for (int x = 0; x < Cols; x++)
            {
                grid[y, x] = probabilityManager.GetRandomSymbolId();
            }
        }
        return grid;
    }

    /// <summary>
    /// 縦・横・斜め（右下／右上）で、同一シンボルが3個以上連続したら当たり。
    /// </summary>
    public EvalResult Evaluate(SymbolId[,] finalGrid)
    {
        // 念のためサイズチェック（ここで落として原因を明確にする）
        if (finalGrid == null || finalGrid.GetLength(0) != Rows || finalGrid.GetLength(1) != Cols)
        {
            return new EvalResult
            {
                winMask = new bool[Rows, Cols],
                keyHit = false,
                totalWin = 0,
                message = "エラー: finalGrid のサイズが不正です。",
                winLines = new List<WinLine>()
            };
        }

        // 0) ジャックポット（全マス同一）を最優先で扱う
        if (TryGetJackpot(finalGrid, out var jpSymbol))
        {
            var r = new EvalResult
            {
                winMask = new bool[Rows, Cols],
                keyHit = (jpSymbol == SymbolId.Key),
                totalWin = CalcJackpotPayout(jpSymbol),
                message = $"JACKPOT!! 役: {SymbolText(jpSymbol)} x{Rows * Cols}\n配当: {CalcJackpotPayout(jpSymbol)}",
                winLines = new List<WinLine>(),
                isJackpot = true
            };

            // 全マスを勝ち扱い
            for (int y = 0; y < Rows; y++)
                for (int x = 0; x < Cols; x++)
                    r.winMask[y, x] = true;

            // ジャックポットは「1本のライン」として扱うならここで追加してもよいが、
            // 今回は全マス当たりで十分なので追加しない（線を出したければ追加してOK）
            return r;
        }

        var result = new EvalResult
        {
            winMask = new bool[Rows, Cols],
            keyHit = false,
            totalWin = 0,
            message = "",
            winLines = new List<WinLine>(),
            isJackpot = false
        };

        var sb = new StringBuilder();

        // (dy, dx, label)
        (int dy, int dx, string label)[] dirs = new (int, int, string)[]
        {
            (0, 1,  "横"),
            (1, 0,  "縦"),
            (1, 1,  "斜め"),
            (-1, 1, "斜め")
        };

        int lineNo = 1;

        for (int di = 0; di < dirs.Length; di++)
        {
            int dy = dirs[di].dy;
            int dx = dirs[di].dx;
            string label = dirs[di].label;

            for (int y = 0; y < Rows; y++)
            {
                for (int x = 0; x < Cols; x++)
                {
                    SymbolId symbol = finalGrid[y, x];

                    // 逆方向に同じシンボルがあるなら、このセルからは開始しない（二重カウント防止）
                    int py = y - dy;
                    int px = x - dx;
                    if (py >= 0 && py < Rows && px >= 0 && px < Cols)
                    {
                        if (finalGrid[py, px] == symbol) continue;
                    }

                    // 連続カウント
                    int count = 1;
                    int cy = y + dy;
                    int cx = x + dx;
                    while (cy >= 0 && cy < Rows && cx >= 0 && cx < Cols && finalGrid[cy, cx] == symbol)
                    {
                        count++;
                        cy += dy;
                        cx += dx;
                    }

                    if (count < 3) continue;

                    int lineWin = CalcPayout(symbol, count);
                    if (lineWin <= 0) continue;

                    // ---- ここから「有効ライン」として確定 ----
                    result.totalWin += lineWin;

                    // マスク
                    cy = y; cx = x;
                    for (int i = 0; i < count; i++)
                    {
                        result.winMask[cy, cx] = true;
                        cy += dy;
                        cx += dx;
                    }

                    // 🔑成立
                    if (symbol == SymbolId.Key) result.keyHit = true;

                    // ログ文
                    sb.AppendLine(
                        $"ライン{lineNo}: {label} 開始(行{y + 1}, 列{x + 1}) に {SymbolText(symbol)} が{count}個連続（配当: {lineWin}）"
                    );
                    lineNo++;

                    // ★ WinLine は「有効ライン」のときだけ追加（ここが重要）
                    result.winLines.Add(new WinLine
                    {
                        start = new Vector2Int(x, y),    // x=col, y=row
                        dir   = new Vector2Int(dx, dy),  // x=dx,  y=dy
                        length = count
                    });
                }
            }
        }

        if (result.totalWin == 0)
        {
            result.message = "ハズレ（有効ラインなし）";
        }
        else
        {
            sb.AppendLine($"合計配当: {result.totalWin}");
            result.message = sb.ToString();
        }

        return result;
    }

    private static string SymbolText(SymbolId id)
    {
        switch (id)
        {
            case SymbolId.Seven:      return "7";
            case SymbolId.Key:        return "K";
            case SymbolId.A:          return "A";
            case SymbolId.Diamond:    return "D";
            case SymbolId.Bar:        return "BAR";
            case SymbolId.Coin:       return "C";
            case SymbolId.Watermelon: return "W";
            case SymbolId.Bell:       return "BELL";
            case SymbolId.Cherry:     return "CH";
            case SymbolId.Clover:     return "CL";
            default:                  return "?";
        }
    }

    public bool TryGetJackpot(SymbolId[,] finalGrid, out SymbolId symbol)
    {
        symbol = finalGrid[0, 0];
        for (int y = 0; y < Rows; y++)
        {
            for (int x = 0; x < Cols; x++)
            {
                if (finalGrid[y, x] != symbol) return false;
            }
        }
        return true;
    }

    private static int CalcPayout(SymbolId symbol, int count)
    {
        if (count < 3) return 0;
        if (count > 7) count = 7;

        // TOP: 7 / Key
        if (symbol == SymbolId.Seven || symbol == SymbolId.Key)
        {
            switch (count)
            {
                case 3: return 40;
                case 4: return 100;
                case 5: return 240;
                case 6: return 400;
                case 7: return 700;
            }
        }

        // HIGH: A / Diamond
        if (symbol == SymbolId.A || symbol == SymbolId.Diamond)
        {
            switch (count)
            {
                case 3: return 30;
                case 4: return 80;
                case 5: return 180;
                case 6: return 320;
                case 7: return 550;
            }
        }

        // MID: BAR / Coin / Watermelon
        if (symbol == SymbolId.Bar || symbol == SymbolId.Coin || symbol == SymbolId.Watermelon)
        {
            switch (count)
            {
                case 3: return 20;
                case 4: return 60;
                case 5: return 120;
                case 6: return 220;
                case 7: return 400;
            }
        }

        // LOW: Bell / Cherry / Clover
        if (symbol == SymbolId.Bell || symbol == SymbolId.Cherry || symbol == SymbolId.Clover)
        {
            switch (count)
            {
                case 3: return 10;
                case 4: return 30;
                case 5: return 60;
                case 6: return 120;
                case 7: return 220;
            }
        }

        return 0;
    }
}
