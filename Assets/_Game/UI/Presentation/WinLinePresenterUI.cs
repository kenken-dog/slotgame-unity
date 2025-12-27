using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using WinLine = SlotMachineCore.WinLine;

public class WinLinePresenterUI : MonoBehaviour
{
    [Header("References")]
    public RectTransform reelRoot;     // リール領域（ReelRoot）
    public RectTransform lineLayer;    // WinLineLayer（通常はこのスクリプトのRectTransformでもOK）
    public Image[] reelImages;         // 28個（row-major）
    public Sprite lineSprite;          // 1x1白スプライト（Square推奨）

    [Header("Appearance")]
    public Color lineColor = Color.red;
    public float lineThickness = 10f;  // 太さ（UIなのでピクセル）
    public float linePadding = 0f;     // 始点/終点を少し伸ばしたい場合

    private readonly List<Image> activeLines = new List<Image>();

    private const int Cols = SlotMachineCore.Cols;

    public void ClearLines()
    {
        for (int i = 0; i < activeLines.Count; i++)
        {
            if (activeLines[i] != null) Destroy(activeLines[i].gameObject);
        }
        activeLines.Clear();
    }

    public void DrawLines(List<WinLine> lines)
    {
        if (lines == null || lines.Count == 0) return;

        for (int i = 0; i < lines.Count; i++)
        {
            DrawSingleLine(lines[i]);
        }
    }

    private void DrawSingleLine(WinLine line)
    {
        // ここは「Vector2Int の慣習」に合わせる：
        // start.x=col, start.y=row / dir.x=dx, dir.y=dy
        int startRow = line.start.y;
        int startCol = line.start.x;
        int endRow = startRow + line.dir.y * (line.length - 1);
        int endCol = startCol + line.dir.x * (line.length - 1);

        if (!IsInBounds(startRow, startCol) || !IsInBounds(endRow, endCol))
        {
            Debug.LogWarning($"[WinLinePresenterUI] out of bounds start=({startRow},{startCol}) end=({endRow},{endCol})");
            return;
        }

        // セル中心（ReelRootローカル座標）を取得
        Vector2 p0 = GetCellCenterLocal(startRow, startCol);
        Vector2 p1 = GetCellCenterLocal(endRow, endCol);

        // 線を作る（Image）
        Image img = CreateLineImage();
        RectTransform rt = img.rectTransform;

        // 親は lineLayer（ReelRoot配下推奨）
        rt.SetParent(lineLayer != null ? lineLayer : (RectTransform)transform, false);

        // 中点
        Vector2 mid = (p0 + p1) * 0.5f;
        rt.anchoredPosition = mid;

        // 長さ
        float length = Vector2.Distance(p0, p1) + linePadding * 2f;

        // 回転角（Unity UIはZ回転）
        Vector2 dir = (p1 - p0).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        rt.sizeDelta = new Vector2(length, lineThickness);
        rt.localRotation = Quaternion.Euler(0f, 0f, angle);

        // 表示順（最後に描けば基本前に出るが、念のため）
        rt.SetAsLastSibling();

        activeLines.Add(img);
    }

    private Image CreateLineImage()
    {
        GameObject go = new GameObject("WinLineUI");
        Image img = go.AddComponent<Image>();
        img.raycastTarget = false;
        img.sprite = lineSprite;
        img.type = Image.Type.Sliced; // SimpleでもOK。SquareならSimpleで良い
        img.color = lineColor;
        return img;
    }

    private Vector2 GetCellCenterLocal(int row, int col)
    {
        int index = row * Cols + col;
        RectTransform cell = reelImages[index].rectTransform;

        // ReelRoot のローカル座標へ変換（ReelRoot配下に lineLayer がある前提だとズレにくい）
        Vector3 world = cell.TransformPoint(cell.rect.center);
        Vector3 local = reelRoot.InverseTransformPoint(world);

        return new Vector2(local.x, local.y);
    }

    private bool IsInBounds(int r, int c)
    {
        return r >= 0 && r < SlotMachineCore.Rows && c >= 0 && c < SlotMachineCore.Cols;
    }

    public void SetVisible(bool visible)
    {
        for (int i = 0; i < activeLines.Count; i++)
        {
            if (activeLines[i] != null) activeLines[i].enabled = visible;
        }
    }

}
