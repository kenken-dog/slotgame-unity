using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StartItemChoicePopup : MonoBehaviour
{
    [Header("Root")]
    public GameObject root; // Panel全体（表示/非表示）

    [Header("UI")]
    public Text titleText;
    public Text noteText;
    public ItemChoiceOptionView[] optionViews; // 3枠
    public Button closeButton; // 任意（基本は選ぶまで閉じさせないなら不要）

    private Action<ItemDefinition> _onSelected;

    void Awake()
    {
        if (root != null) root.SetActive(false);

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() =>
            {
                // 仕様次第：閉じさせないなら interactable=false にする
                Hide();
            });
        }
    }

    public void Show(List<ItemDefinition> defs, Action<ItemDefinition> onSelected)
    {
        _onSelected = onSelected;

        if (titleText != null) titleText.text = "スタートアイテムを選んでください";
        if (noteText != null) noteText.text = "Common / Rare から1つ選択";

        if (optionViews != null)
        {
            for (int i = 0; i < optionViews.Length; i++)
            {
                var def = (defs != null && i < defs.Count) ? defs[i] : null;
                optionViews[i].Bind(def, HandleSelect);
            }
        }

        if (root != null) root.SetActive(true);
    }

    private void HandleSelect(ItemDefinition def)
    {
        _onSelected?.Invoke(def);
        Hide();
    }

    public void Hide()
    {
        if (root != null) root.SetActive(false);
    }

    public bool IsOpen => root != null && root.activeSelf;
}
