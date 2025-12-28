using UnityEngine;
using UnityEngine.UI;

public class ItemChoiceOptionView : MonoBehaviour
{
    public Button button;
    public Image iconImage;
    public Text nameText;

    private ItemDefinition _def;
    private System.Action<ItemDefinition> _onClicked;

    public void Bind(ItemDefinition def, System.Action<ItemDefinition> onClicked)
    {
        _def = def;
        _onClicked = onClicked;

        if (iconImage != null) iconImage.sprite = def != null ? def.icon : null;
        if (nameText != null) nameText.text = def != null ? def.displayName : "-";

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                if (_def != null) _onClicked?.Invoke(_def);
            });

            button.interactable = (def != null);
        }
    }
}
