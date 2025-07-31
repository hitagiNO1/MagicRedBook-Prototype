using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    public Transform contentPanel; // UI 清單容器
    public GameObject itemSlotPrefab; // UI 項目 prefab
    public GameObject confirmPanel; // 是否使用道具的提示面板
    public TextMeshProUGUI confirmText;

    private ItemData selectedItem; // 當前選取的道具

    public void RefreshUI()
    {
        // 清空舊的欄位
        foreach (Transform child in contentPanel)
        {
            Destroy(child.gameObject);
        }
        // 產生新的 UI 欄位
        foreach (ItemData item in InventoryManager.Instance.items)
        {
            if (item.amount >= 1)
            {
                GameObject slot = Instantiate(itemSlotPrefab, contentPanel);
                slot.GetComponent<ItemSlot>().Setup(item, this);
            }
        }
    }

    // 顯示是否使用道具的確認面板
    public void PromptUse(ItemData item)
    {
        selectedItem = item;
        confirmText.text = $"使用 {item.itemName}？";
        confirmPanel.SetActive(true);
    }

    // 點擊使用道具
    public void ConfirmUse()
    {
        Debug.Log($"使用了 {selectedItem.itemName}，效果：{selectedItem.description}");
        selectedItem.amount -= 1;
        confirmPanel.SetActive(false);
        RefreshUI();

        // 此處可進一步處理效果
    }

    // 取消使用
    public void CancelUse()
    {
        confirmPanel.SetActive(false);
    }
}
