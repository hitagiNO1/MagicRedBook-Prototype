using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemSlot : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI nameText;
    public Button useButton;
    public TextMeshProUGUI Description;
    public TextMeshProUGUI amount;
   
    private ItemData data;
    private InventoryUI uiRef;

    // 初始化欄位內容
    public void Setup(ItemData item, InventoryUI ui)
    {
        data = item;
        uiRef = ui;
        amount.text = item.amount.ToString();
        icon.sprite = item.icon;
        nameText.text = item.itemName;
        Description.text = item.description;
        useButton.onClick.AddListener(() => uiRef.PromptUse(data));
    }
}
