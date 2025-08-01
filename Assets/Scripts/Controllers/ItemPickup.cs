using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public ItemData itemData; // 此道具對應的資料物件
    private void OnTriggerEnter(Collider other)
    {
        // 如果玩家碰到此道具物件
        if (other.CompareTag("Player"))
        {
            // 呼叫道具管理器將此道具加入清單
            InventoryManager.Instance.AddItem(itemData,1);
            Destroy(gameObject); // 刪除場上道具物件
        }
    }
}
