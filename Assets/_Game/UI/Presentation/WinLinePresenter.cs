using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using WinLine = SlotMachineCore.WinLine;


public class WinLinePresenter : MonoBehaviour
{
    [Header("Line Appearance")]
    public Color lineColor = Color.red;
    public float lineWidth = 8f;

    [Header("References")]
    public RectTransform reelRoot; // リール全体（マスクのRect）
    public Image[] reelImages;     // 28個（row-major）

    private readonly List<LineRenderer> activeLines = new();

    private const int Rows = SlotMachineCore.Rows;
    private const int Cols = SlotMachineCore.Cols;

    public void ClearLines()
    {
        foreach (var lr in activeLines)
        {
            if (lr != null) Destroy(lr.gameObject);
        }
        activeLines.Clear();
    }

    public void DrawLines(List<WinLine> lines)
    {
        if (lines == null || lines.Count == 0) return;

        foreach (var line in lines)
        {
            DrawSingleLine(line);
        }
    }

    private void DrawSingleLine(WinLine line)
    {
        int endRow = line.start.y + line.dir.y * (line.length - 1);
        int endCol = line.start.x + line.dir.x * (line.length - 1);

        if (!IsInBounds(line.start.y, line.start.x) || !IsInBounds(endRow, endCol))
        {
            Debug.LogError($"[WinLinePresenter] WinLine out of bounds: start=({line.start.y},{line.start.x}) dir=({line.dir.y},{line.dir.x}) len={line.length} end=({endRow},{endCol})");
            return;
        }

        // 開始セルと終了セルの座標を計算
        Vector3 startWorld = GetCellCenterWorld(line.start.y, line.start.x);
        Vector3 endWorld = GetCellCenterWorld(
            line.start.y + line.dir.y * (line.length - 1),
            line.start.x + line.dir.x * (line.length - 1)
        );

        GameObject go = new GameObject("WinLine");
        go.transform.SetParent(transform, false);

        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.SetPosition(0, startWorld);
        lr.SetPosition(1, endWorld);

        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;

        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lineColor;
        lr.endColor = lineColor;

        lr.useWorldSpace = true;
        lr.sortingOrder = 10; // リールより前

        activeLines.Add(lr);
    }

    private bool IsInBounds(int r, int c)
    {
        return r >= 0 && r < SlotMachineCore.Rows && c >= 0 && c < SlotMachineCore.Cols;
    }

    private Vector3 GetCellCenterWorld(int row, int col)
    {
        // 範囲チェック
        if (row < 0 || row >= SlotMachineCore.Rows || col < 0 || col >= SlotMachineCore.Cols)
        {
            Debug.LogError($"[WinLinePresenter] Cell out of range: row={row}, col={col}");
            return transform.position;
        }

        int index = row * SlotMachineCore.Cols + col;

        if (reelImages == null)
        {
            Debug.LogError("[WinLinePresenter] reelImages is null");
            return transform.position;
        }

        if (index < 0 || index >= reelImages.Length)
        {
            Debug.LogError($"[WinLinePresenter] reelImages index out of range: index={index}, len={reelImages.Length} (row={row}, col={col})");
        return transform.position;
        }

        if (reelImages[index] == null)
        {
            Debug.LogError($"[WinLinePresenter] reelImages[{index}] is null");
            return transform.position;
        }

        RectTransform rt = reelImages[index].rectTransform;
        return rt.TransformPoint(rt.rect.center);
    }



}
