using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic; 

public class SlotGameController : MonoBehaviour
{
    [Header("UI")]
    public Button spinButton;

    [Header("Score UI")]
    public Text scoreText;            // NEW: 現在スコア表示
    public Text goalText;             // NEW: ゴール表示（任意）
    public Text statusText;           // NEW: クリア表示など（任意）※WinPresentationがあるなら不要でもOK

    [Header("Goal")]
    public int goalScore = 1000;      // NEW: とりあえず1000点
    public bool stopOnGoal = true;    // NEW: 到達で停止

    [Header("Presentation")]
    public WinPresentation winPresentation;
    public ItemPopupPresenter itemPopupPresenter;

    [Header("Systems")]
    public SlotProbabilityManager probabilityManager;
    public ItemManager itemManager;

    [Header("Animator (A方式)")]
    public StepwiseReelAnimator stepwiseAnimator;

    [Header("Win Line")]
    public WinLinePresenterUI winLinePresenter;

    [Header("Highlight Colors")]
    public Color defaultColor = Color.white;
    public Color winColor = Color.yellow;

    [Header("Audio")]
    public AudioManager audioManager;

    [Header("Win Line Sequence")]
    public float perLineHoldSeconds = 0.7f;   // 1ライン表示の保持時間
    public float perLineGapSeconds = 0.1f;    // ライン間の間
    public bool blinkEachLine = true;         // 点滅させるか

    [Header("Auto Spin")]
    public Button autoSpinButton;
    public Text autoSpinButtonText;

    [Header("Start Item Choice")]
    public StartItemChoicePopup startItemChoicePopup;
    public bool requireStartItemChoice = true;

    private bool startChoiceDone = false;

    private bool isAutoSpin = false;

    private OutcomeGenerator outcomeGenerator;

    private bool[,] BuildMaskForSingleLine(SlotMachineCore.WinLine line)
    {
        bool[,] mask = new bool[SlotMachineCore.Rows, SlotMachineCore.Cols];

        int row = line.start.y;
        int col = line.start.x;
        int dy = line.dir.y;
        int dx = line.dir.x;

        for (int i = 0; i < line.length; i++)
        {
            if (row >= 0 && row < SlotMachineCore.Rows && col >= 0 && col < SlotMachineCore.Cols)
            {
                mask[row, col] = true;
            }
            row += dy;
            col += dx;
        }

        return mask;
    }

    private IEnumerator PlayWinLinesSequential(SlotMachineCore.EvalResult eval)
    {
        if (stepwiseAnimator == null) yield break;

        // 演出前に一旦クリア
        stepwiseAnimator.ResetColors(defaultColor);
        winLinePresenter?.ClearLines();

        // winLines が無い場合は従来通り全体マスクを一回出す
        if (eval.winLines == null || eval.winLines.Count == 0)
        {
            stepwiseAnimator.ApplyHighlight(eval.winMask, winColor, defaultColor);
            yield return new WaitForSeconds(perLineHoldSeconds);
            yield break;
        }

        for (int i = 0; i < eval.winLines.Count; i++)
        {
            var line = eval.winLines[i];

            // 1本分のマスクを生成
            bool[,] mask = BuildMaskForSingleLine(line);

            // 線を1本だけ表示
            audioManager.PlayLineWin();
            winLinePresenter?.ClearLines();
            winLinePresenter?.DrawLines(new List<SlotMachineCore.WinLine> { line });

            if (blinkEachLine && winPresentation != null)
            {
                // 既存の点滅演出を流用（ハイライトと線を同期）
                yield return StartCoroutine(
                    winPresentation.PlayWinBlink((on) =>
                    {
                        stepwiseAnimator.ApplyHighlight(mask, on ? winColor : defaultColor, defaultColor);

                        // 線も点滅（WinLinePresenterUI に SetVisible を追加してあるとベスト）
                        // 無ければ Clear/Draw で代用（ちらつくが動く）
                        if (winLinePresenter != null)
                        {
                            // WinLinePresenterUI に SetVisible(bool) を追加してある前提
                            winLinePresenter.SetVisible(on);
                        }
                    })
                );
            }
            else
            {
                // 点滅しない場合：一定時間表示
                stepwiseAnimator.ApplyHighlight(mask, winColor, defaultColor);
                yield return new WaitForSeconds(perLineHoldSeconds);
            }

            // 1本終わったら解除
            stepwiseAnimator.ResetColors(defaultColor);
            winLinePresenter?.ClearLines();

            if (perLineGapSeconds > 0f)
                yield return new WaitForSeconds(perLineGapSeconds);
        }

        // 最後に「全体マスク」を一度出して締める（好み）
        // stepwiseAnimator.ApplyHighlight(eval.winMask, winColor, defaultColor);
        // winLinePresenter?.DrawLines(eval.winLines);
    }

    private void ToggleAutoSpin()
    {
        if (isGameCleared) return;

        isAutoSpin = !isAutoSpin;
        UpdateAutoSpinUI();

        // ONにした瞬間、スピン可能なら即スタート
        if (isAutoSpin && !isSpinning)
        {
            StartCoroutine(SpinFlow());
        }
    }

    private void UpdateAutoSpinUI()
    {
        if (autoSpinButtonText != null)
        {
            autoSpinButtonText.text = isAutoSpin ? "AUTO\nON" : "AUTO\nOFF";
        }
    }

    // ===== 状態 =====
    private bool isSpinning = false;
    private bool isGameCleared = false;
    private int totalScore = 0;

    private SlotMachineCore core;

    void Awake()
    {
        core = new SlotMachineCore();
        outcomeGenerator = new OutcomeGenerator();
    }

    void Start()
    {
        UpdateScoreUI();
        UpdateAutoSpinUI();

        if (spinButton != null)
        {
            spinButton.onClick.AddListener(() =>
            {
                if (!startChoiceDone) return; 

                if (!isSpinning && !isGameCleared)
                {
                    audioManager?.PlaySpinButton();
                    StartCoroutine(SpinFlow());
                }
            });
        }

        if (autoSpinButton != null)
        {
            autoSpinButton.onClick.AddListener(ToggleAutoSpin);
        }

        if (stepwiseAnimator != null)
        {
            stepwiseAnimator.ResetColors(defaultColor);
        }

        if (goalText != null)
        {
            goalText.text = $"GOAL: {goalScore}";
        }

        if (statusText != null)
        {
            statusText.text = "";
        }

        // ★ ゲーム開始時のアイテム選択
        if (requireStartItemChoice)
        {
            BeginStartItemChoice();
        }
        else
        {
            startChoiceDone = true;
        }

    }

    private IEnumerator SpinFlow()
    {
        audioManager?.PlaySpinButton();
        winLinePresenter?.ClearLines();

        if (winLinePresenter != null)
        {
            winLinePresenter.ClearLines();
        }

        isSpinning = true;
        if (spinButton != null) spinButton.interactable = false;

        if (winPresentation != null) winPresentation.SetResultInstant("スピン中...");

        // 1) 確率リフレッシュ（ベース→アイテム適用）
        probabilityManager.ResetAllWeightsToBase();
        if (itemManager != null)
        {
            probabilityManager.ApplyItemMultipliers(itemManager.GetWeightMultiplier);
        }

        // 2) 最終出目確定（新方式）
        SymbolId[,] finalGrid = outcomeGenerator.GenerateFinalGrid(probabilityManager);

        // 3) 表示：スピン演出
        if (stepwiseAnimator != null)
        {
            stepwiseAnimator.ResetColors(defaultColor);
            Func<SymbolId> rolling = () => probabilityManager.GetRandomSymbolId();
            yield return StartCoroutine(stepwiseAnimator.Play(finalGrid, rolling));
        }

        // 4) 判定
        var eval = core.Evaluate(finalGrid);

        // ジャックポットは専用SE（優先）
        if (eval.isJackpot)
        {
            audioManager?.PlayJackpot();
        }
        else if (eval.totalWin > 0)
        {
            audioManager?.PlayLineWin();
        }

        // 5) スコア加算（ここがゴール判定の基点）
        if (eval.totalWin > 0)
        {
            totalScore += eval.totalWin;
            UpdateScoreUI();
        }

        // 6) 当たり演出（1ラインずつ）
        if (eval.totalWin > 0)
        {
            yield return StartCoroutine(PlayWinLinesSequential(eval));
        }
        else
        {
            // ハズレ：ハイライト解除/線消去
            stepwiseAnimator?.ResetColors(defaultColor);
            winLinePresenter?.ClearLines();
        }

        // 7) 🔑でアイテム
        string message = eval.message;

        if (eval.keyHit && itemManager != null)
        {
            if (itemManager.TryGiveRandomItemByMinRarity(ItemRarity.Epic, out ItemId newItem))
            {
                var def = itemManager.GetDefinition(newItem);
                if (def != null)
                {
                    message += $"\n🔑役成立！新しいアイテムを獲得: {def.displayName} [{def.rarity}]";
                    if (itemPopupPresenter != null)
                        itemPopupPresenter.Show($"ITEM GET!\n{def.displayName}\n<{def.rarity}>");
                }
                else
                {
                    message += $"\n🔑役成立！新しいアイテムを獲得: {newItem}";
                    if (itemPopupPresenter != null)
                        itemPopupPresenter.Show($"ITEM GET!\n{newItem}");
                }
            }
            else
            {
                message += "\n🔑役成立！ただし Epic以上の未取得アイテムがありません。";
            }
        }

        // 8) ゴール判定
        if (!isGameCleared && totalScore >= goalScore)
        {
            isGameCleared = true;

            isAutoSpin = false; 
            UpdateAutoSpinUI(); 

            string clearMsg = $"\n\nGOAL達成！({totalScore} / {goalScore})\nGAME CLEAR!";
            message += clearMsg;

            if (statusText != null)
                statusText.text = "GAME CLEAR!";

            if (stopOnGoal && spinButton != null)
                spinButton.interactable = false;
        }

        // 9) 結果テキスト
        //if (winPresentation != null)
        //{
        //    yield return StartCoroutine(winPresentation.PlayResultTypewriter(message));
        //}

        // クリアしていない場合のみ次スピンを許可
        if (!isGameCleared && spinButton != null)
            spinButton.interactable = true;

        isSpinning = false;

        // ===== オートスピン継続判定 =====
        if (isAutoSpin && !isGameCleared)
        {
            // 少し間を空けて次スピン（演出を邪魔しない）
            yield return new WaitForSeconds(0.2f);

            if (!isSpinning)
            {
                StartCoroutine(SpinFlow());
            }
        }

    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"SCORE: {totalScore}";
        }
        if (goalText != null)
        {
            goalText.text = $"GOAL: {goalScore}";
        }
    }

    // 任意：リスタートしたい場合のAPI（ボタンに紐付けてもOK）
    public void ResetGame()
    {
        isGameCleared = false;
        totalScore = 0;
        UpdateScoreUI();

        if (statusText != null) statusText.text = "";
        if (stepwiseAnimator != null) stepwiseAnimator.ResetColors(defaultColor);

        if (spinButton != null) spinButton.interactable = true;
        if (winPresentation != null) winPresentation.SetResultInstant("リセットしました。");
    }
    private void BeginStartItemChoice()
    {
        startChoiceDone = false;

        // スピンを封じる（オートも止める）
        if (spinButton != null) spinButton.interactable = false;

        // 候補生成
        var defs = itemManager != null ? itemManager.GetStartChoiceCandidates(3) : null;

        // 候補が無い/足りない場合のフォールバック
        if (defs == null || defs.Count == 0)
        {
            startChoiceDone = true;
            if (spinButton != null) spinButton.interactable = true;
            return;
        }

        if (startItemChoicePopup != null)
        {
            startItemChoicePopup.Show(defs, (picked) =>
            {
                if (picked != null && itemManager != null)
                {
                    itemManager.TryGiveItem(picked.id);

                    // 取得通知（任意）
                    if (itemPopupPresenter != null)
                        itemPopupPresenter.Show($"START ITEM!\n{picked.displayName}");
                }

                startChoiceDone = true;
                if (!isGameCleared && spinButton != null) spinButton.interactable = true;
            });
        }
        else
        {
            // UIが無い場合：先頭を自動取得（保険）
            itemManager.TryGiveItem(defs[0].id);
            startChoiceDone = true;
            if (!isGameCleared && spinButton != null) spinButton.interactable = true;
        }
    }

}
