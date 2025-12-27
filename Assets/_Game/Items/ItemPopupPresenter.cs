using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ItemPopupPresenter : MonoBehaviour
{
    [Header("UI")]
    public GameObject panelRoot; // ItemPopupPanel
    public Text popupText;       // ItemPopupText

    [Header("Animation")]
    public float showSeconds = 0.9f;
    public float fadeInSeconds = 0.12f;
    public float fadeOutSeconds = 0.18f;
    public float startScale = 0.92f;
    public float endScale = 1.00f;

    private CanvasGroup canvasGroup;
    private Coroutine running;

    void Awake()
    {
        if (panelRoot != null)
        {
            canvasGroup = panelRoot.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = panelRoot.AddComponent<CanvasGroup>();
            panelRoot.SetActive(false);
        }
    }

    public void Show(string message)
    {
        if (panelRoot == null || popupText == null) return;

        if (running != null) StopCoroutine(running);
        running = StartCoroutine(ShowRoutine(message));
    }

    private IEnumerator ShowRoutine(string message)
    {
        panelRoot.SetActive(true);
        popupText.text = message;

        // init
        canvasGroup.alpha = 0f;
        panelRoot.transform.localScale = Vector3.one * startScale;

        // fade in
        float t = 0f;
        while (t < fadeInSeconds)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / fadeInSeconds);
            canvasGroup.alpha = p;
            panelRoot.transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, p);
            yield return null;
        }
        canvasGroup.alpha = 1f;
        panelRoot.transform.localScale = Vector3.one * endScale;

        // hold
        yield return new WaitForSeconds(showSeconds);

        // fade out
        t = 0f;
        while (t < fadeOutSeconds)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / fadeOutSeconds);
            canvasGroup.alpha = 1f - p;
            yield return null;
        }

        canvasGroup.alpha = 0f;
        panelRoot.SetActive(false);
        running = null;
    }
}
