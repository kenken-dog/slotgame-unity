using UnityEngine;
using UnityEngine.UI;

public class OptionsMenuController : MonoBehaviour
{
    [Header("Wiring")]
    public AudioManager audioManager;

    [Header("UI")]
    public GameObject optionsPanel;
    public Button optionsButton;   // オプションを開く
    public Button closeButton;     // 閉じる
    public Toggle bgmToggle;       // BGM ON/OFF
    public Toggle seToggle;        // SE ON/OFF

    void Start()
    {
        if (optionsPanel != null) optionsPanel.SetActive(false);

        if (optionsButton != null)
            optionsButton.onClick.AddListener(Open);

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        // 初期状態を反映（イベント発火を避けてセット）
        RefreshTogglesFromAudioManager();

        if (bgmToggle != null)
            bgmToggle.onValueChanged.AddListener(OnBgmToggleChanged);

        if (seToggle != null)
            seToggle.onValueChanged.AddListener(OnSeToggleChanged);
    }

    private void RefreshTogglesFromAudioManager()
    {
        if (audioManager == null) return;

        if (bgmToggle != null) bgmToggle.SetIsOnWithoutNotify(audioManager.BgmOn);
        if (seToggle  != null) seToggle.SetIsOnWithoutNotify(audioManager.SeOn);
    }

    public void Open()
    {
        if (optionsPanel != null) optionsPanel.SetActive(true);
        UIStateManager.Instance.SetOptionOpen(true);
        RefreshTogglesFromAudioManager();
    }

    public void Close()
    {
        if (optionsPanel != null) optionsPanel.SetActive(false);
        UIStateManager.Instance.SetOptionOpen(false);
    }

    private void OnBgmToggleChanged(bool on)
    {
        audioManager?.SetBgmOn(on);
    }

    private void OnSeToggleChanged(bool on)
    {
        audioManager?.SetSeOn(on);
    }
}
