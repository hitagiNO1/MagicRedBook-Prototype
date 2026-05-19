using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;
    public List<ItemData> items = new List<ItemData>(); // 道具清單

    private void Awake()
    {
        // 設定 Singleton 實例
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void AddItem(ItemData newItem, int Itemamount)
    {
        // 嘗試找到已擁有相同名稱的道具
        ItemData existingItem = items.FirstOrDefault(i => i.itemName == newItem.itemName);

        if (existingItem != null)
        {
            existingItem.amount += Itemamount;
        }
        else
        {
            items.Add(newItem);
            newItem.amount += Itemamount;
        }

        // === ★ 核心修復：同步數量給 Itemuse 內部的字典 ===
        Itemuse itemUse = FindObjectOfType<Itemuse>();
        if (itemUse != null)
        {
            // 將 itemName 轉換成 Itemuse 字典在用的英文 Key
            string itemKey = GetItemuseKey(newItem.itemName);

            // 呼叫 Itemuse 裡面的 AddItem 方法來更新字典
            itemUse.AddItem(itemKey, Itemamount);
        }
    }

    // === ★ 新增：將中文道具名稱轉成 Itemuse 字典用的 Key ===
    private string GetItemuseKey(string itemName)
    {
        switch (itemName)
        {
            case "神聖罐罐！":
            case "Can":
                return "Can";

            case "鮪魚肉泥":
            case "Tuna":
                return "Tuna";

            case "貓草":
            case "Grass":
                return "Grass";

            case "凍乾":
            case "Drying":
                return "Drying";

            default:
                return itemName; // 如果都不是，就傳原名
        }
    }
}