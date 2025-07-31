using System.Collections.Generic;
using UnityEngine;

public class itemmanager : MonoBehaviour
{
    public static itemmanager Instance;

    public List<InventoryItem> inventory = new List<InventoryItem>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject); // 跨場景保留
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddItem(ItemData itemData)
    {
        InventoryItem existingItem = inventory.Find(item => item.itemData == itemData);
        if (existingItem != null)
        {
            existingItem.quantity += itemData.amount;
        }
        else
        {
            inventory.Add(new InventoryItem(itemData, itemData.amount));
        }

        Debug.Log("已獲得物品：" + itemData.itemName);
    }

    public void RemoveItem(ItemData itemData, int amount)
    {
        InventoryItem existingItem = inventory.Find(item => item.itemData == itemData);
        if (existingItem != null)
        {
            existingItem.quantity -= amount;
            if (existingItem.quantity <= 0)
            {
                inventory.Remove(existingItem);
            }
        }
    }

    public int GetItemQuantity(ItemData itemData)
    {
        InventoryItem existingItem = inventory.Find(item => item.itemData == itemData);
        return existingItem != null ? existingItem.quantity : 0;
    }
}
