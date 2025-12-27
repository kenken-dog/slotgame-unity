using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class StepwiseReelAnimator : MonoBehaviour, IReelAnimator
{
    [Header("盤面Image（row-major：上段左→右7個→次段…で28個）")]
    public Image[] reelImageList; // 28個

    [Header("Sprites")]
    public SymbolSpriteSet spriteSet;

    [Header("回転の基本スピード（速→遅）")]
    public float startInterval = 0.03f;
    public float endInterval = 0.12f;

    [Header("列ごとの回転時間")]
    public float baseSpinTime = 0.8f;
    public float stopStagger = 0.15f;

    [Header("カタカタ停止演出")]
    public int settleTicks = 3;
    public float settleIntervalBase = 0.14f;
    public float settleIntervalStep = 0.06f;

    [Header("SFX")]
    public AudioSource audioSource;
    public AudioClip tickClip;
    public AudioClip stopClip;
    public float tickVolume = 0.4f;
    public float stopVolume = 0.8f;
    public int tickEveryN = 2;

    private Image[,] reelImages;       // [rows, cols]
    private SymbolId[,] displayGrid;   // 表示中
    private const int Rows = SlotMachineCore.Rows;
    private const int Cols = SlotMachineCore.Cols;

    void Awake()
    {
        BuildGridReferences();
        displayGrid = new SymbolId[Rows, Cols];
    }

    private void BuildGridReferences()
    {
        reelImages = new Image[Rows, Cols];
        int index = 0;
        for (int y = 0; y < Rows; y++)
        {
            for (int x = 0; x < Cols; x++)
            {
                if (index < reelImageList.Length)
                {
                    reelImages[y, x] = reelImageList[index];
                    index++;
                }
            }
        }
    }

    public IEnumerator Play(SymbolId[,] finalGrid, Func<SymbolId> getRollingSymbol)
    {
        if (spriteSet == null)
        {
            Debug.LogError("StepwiseReelAnimator: spriteSet が未設定です。");
            yield break;
        }

        // 列ごとの停止時刻
        float[] stopTimes = new float[Cols];
        float maxStop = 0f;
        for (int x = 0; x < Cols; x++)
        {
            stopTimes[x] = baseSpinTime + x * stopStagger;
            if (stopTimes[x] > maxStop) maxStop = stopTimes[x];
        }

        bool[] landed = new bool[Cols];
        bool[] settling = new bool[Cols];
        int[] remainingSettle = new int[Cols];
        float[] nextTick = new float[Cols];

        // 初期表示（rollingで埋める）
        for (int x = 0; x < Cols; x++)
        {
            for (int y = 0; y < Rows; y++)
            {
                displayGrid[y, x] = getRollingSymbol();
            }
            nextTick[x] = 0f;
            remainingSettle[x] = settleTicks;
        }
        RenderAll();

        float elapsed = 0f;
        float dt = 0.01f;
        WaitForSeconds wait = new WaitForSeconds(dt);

        while (elapsed < maxStop + 2.0f)
        {
            bool allDone = true;

            for (int x = 0; x < Cols; x++)
            {
                if (landed[x]) continue;
                allDone = false;

                if (!settling[x] && elapsed >= stopTimes[x])
                {
                    settling[x] = true;
                    remainingSettle[x] = Mathf.Max(0, settleTicks);
                    nextTick[x] = elapsed;
                }

                if (elapsed < nextTick[x]) continue;

                if (!settling[x])
                {
                    float t = Mathf.Clamp01(elapsed / stopTimes[x]);
                    float interval = Mathf.Lerp(startInterval, endInterval, EaseOutCubic(t));

                    SpinOneTick(x, getRollingSymbol);
                    nextTick[x] = elapsed + interval;
                }
                else
                {
                    if (remainingSettle[x] > 0)
                    {
                        SpinOneTick(x, getRollingSymbol);

                        int done = settleTicks - remainingSettle[x];
                        float interval = settleIntervalBase + settleIntervalStep * done;

                        remainingSettle[x]--;
                        nextTick[x] = elapsed + interval;
                    }
                    else
                    {
                        // 着地
                        for (int y = 0; y < Rows; y++) displayGrid[y, x] = finalGrid[y, x];
                        RenderColumn(x);

                        if (audioSource != null && stopClip != null)
                            audioSource.PlayOneShot(stopClip, stopVolume);

                        landed[x] = true;
                    }
                }
            }

            if (allDone) break;

            yield return wait;
            elapsed += dt;
        }

        // 念のため全列着地
        for (int x = 0; x < Cols; x++)
        {
            if (!landed[x])
            {
                for (int y = 0; y < Rows; y++) displayGrid[y, x] = finalGrid[y, x];
                RenderColumn(x);
            }
        }
    }

    private void SpinOneTick(int col, Func<SymbolId> getRollingSymbol)
    {
        for (int y = Rows - 1; y >= 1; y--)
            displayGrid[y, col] = displayGrid[y - 1, col];

        displayGrid[0, col] = getRollingSymbol();
        RenderColumn(col);

        if (audioSource != null && tickClip != null)
        {
            if (tickEveryN <= 1 || (Time.frameCount % tickEveryN == 0))
                audioSource.PlayOneShot(tickClip, tickVolume);
        }
    }

    private static float EaseOutCubic(float t)
    {
        float u = 1f - t;
        return 1f - u * u * u;
    }

    private void RenderAll()
    {
        for (int x = 0; x < Cols; x++) RenderColumn(x);
    }

    private void RenderColumn(int col)
    {
        for (int y = 0; y < Rows; y++)
        {
            var img = reelImages[y, col];
            if (img == null) continue;

            Sprite s = spriteSet.Get(displayGrid[y, col]);
            img.sprite = s;
            img.enabled = (s != null);
        }
    }

    public void ApplyHighlight(bool[,] winMask, Color winColor, Color defaultColor)
    {
        for (int y = 0; y < Rows; y++)
        {
            for (int x = 0; x < Cols; x++)
            {
                var img = reelImages[y, x];
                if (img == null) continue;
                img.color = winMask[y, x] ? winColor : defaultColor;
            }
        }
    }

    public void ResetColors(Color defaultColor)
    {
        for (int y = 0; y < Rows; y++)
        {
            for (int x = 0; x < Cols; x++)
            {
                var img = reelImages[y, x];
                if (img == null) continue;
                img.color = defaultColor;
            }
        }
    }
}
