using UnityEngine;

[System.Serializable]
public class InventoryItem
{
    public ItemData itemData;    // ScriptableObject 物品資料
    public int quantity;         // 持有數量

    public InventoryItem(ItemData data, int amount)
    {
        itemData = data;
        quantity = amount;
    }
}

