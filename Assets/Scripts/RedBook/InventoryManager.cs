using System.Collections.Generic;
using UnityEngine;
using System.Linq; // 引用 LINQ 提供 FirstOrDefault 用法

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    public List<ItemData> items = new List<ItemData>(); // 道具清單

    public GameObject inventoryUI; // UI 管理腳本（包含 RefreshUI 方法）

    private void Awake()
    {
        // 設定 Singleton 實例
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void AddItem(ItemData newItem)
    {
        // 嘗試找到已擁有相同名稱的道具
        ItemData existingItem = items.FirstOrDefault(i => i.itemName == newItem.itemName);

        if (existingItem != null)
        {
            // 如果道具已存在，數量 +1
            existingItem.amount += 1;
        }
        else
        {
            items.Add(newItem);
            newItem.amount = 1;
        }
        // 更新 UI
        inventoryUI.GetComponent<InventoryUI>().RefreshUI();
    }

}
