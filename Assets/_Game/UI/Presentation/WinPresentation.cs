using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class WinPresentation : MonoBehaviour
{
    [Header("Result Text")]
    public Text resultText;

    [Header("Typewriter")]
    public float typewriterCharsPerSecond = 60f;

    [Header("Blink")]
    public int blinkCount = 4;           // 4回点滅（ON/OFFで8回切替）
    public float blinkInterval = 0.12f;

    private Coroutine typing;

    public void StopAllEffects()
    {
        if (typing != null) StopCoroutine(typing);
        typing = null;
    }

    public void SetResultInstant(string msg)
    {
        StopAllEffects();
        if (resultText != null) resultText.text = msg;
    }

    public IEnumerator PlayResultTypewriter(string msg)
    {
        if (resultText == null) yield break;

        StopAllEffects();
        typing = StartCoroutine(TypeRoutine(msg));
        yield return typing;
    }

    private IEnumerator TypeRoutine(string msg)
    {
        resultText.text = "";
        float secondsPerChar = 1f / Mathf.Max(1f, typewriterCharsPerSecond);

        for (int i = 0; i < msg.Length; i++)
        {
            resultText.text += msg[i];
            yield return new WaitForSeconds(secondsPerChar);
        }
        typing = null;
    }

    /// <summary>
    /// highlightFn(true) で当たり色、highlightFn(false) で通常色、の切り替えを外部に任せる
    /// </summary>
    public IEnumerator PlayWinBlink(System.Action<bool> highlightFn)
    {
        if (highlightFn == null) yield break;

        // ONから開始
        bool on = true;
        for (int i = 0; i < blinkCount * 2; i++)
        {
            highlightFn(on);
            on = !on;
            yield return new WaitForSeconds(blinkInterval);
        }

        // 最後はONで終える
        highlightFn(true);
    }
}
