using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class UIButtonSound : MonoBehaviour
{
    [Header("Wiring")]
    public AudioManager audioManager;

    [Header("Behavior")]
    public bool playOnlyWhenInteractable = true; // 無効ボタンで鳴らさない
    public bool useUiClick = true;               // AudioManager.PlayUiClick を使う
    public AudioClip overrideClip;               // ボタン個別に上書きしたい時
    [Range(0f, 1f)] public float overrideVolume = 0.6f;

    private Button _button;

    void Awake()
    {
        _button = GetComponent<Button>();

        // 参照漏れ対策：未設定ならシーン内から探す（規模が小さい間は便利）
        if (audioManager == null)
            audioManager = FindAnyObjectByType<AudioManager>();


        _button.onClick.AddListener(OnClicked);
    }

    private void OnClicked()
    {
        // ★ オプション画面が開いていたら鳴らさない
        if (UIStateManager.Instance != null &&
            UIStateManager.Instance.IsOptionOpen)
            return;

        if (playOnlyWhenInteractable && _button != null && !_button.interactable)
            return;

        if (audioManager == null) return;

        if (overrideClip != null)
        {
            audioManager.PlaySe(overrideClip, overrideVolume);
            return;
        }

        audioManager.PlayUiClick();
        }
    }
