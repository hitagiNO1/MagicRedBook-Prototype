using UnityEngine;

public class CGManager : MonoBehaviour
{
    // 單例實體，讓任何腳本 (CGText, ItemTrigger) 都能隨時呼叫它
    public static CGManager Instance;

    [System.Serializable] // 加上這個才能在 Inspector 裡展開編輯
    public class ItemData
    {
        public GameObject CG;        // 物品名稱 (方便你在 Inspector 辨識，例如 "書桌")
        public bool isTriggered;       // 系統記錄：是否已經觸發過
        public Sprite triggeredSprite; // 這裡放觸發後要變成的圖片 (例如：已閱讀的書)
    }

    public ItemData[] allItems;

    void Awake()
    {
        // 確保場景中只有一個管理器
        if (Instance == null)
        {
            Instance = this;
            // 如果你的主場景一直存在 (不會被 Unload)，下面這行可以註解掉
            // DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // --- 給 CGText 呼叫用：劇情播完時存檔 ---
    public void SetItemTriggered(int id)
    {
        if (id >= 0 && id < allItems.Length)
        {
            allItems[id].isTriggered = true;
            Debug.Log($"[ItemManager] 物品 ID: {id} 已標記為觸發完成！");
        }
        else
        {
            Debug.LogError($"[ItemManager] 錯誤的 ID: {id}，請檢查陣列範圍。");
        }
    }

    // --- 給 ItemTrigger 呼叫用：物件醒來時檢查狀態 ---
    public bool IsItemTriggered(int id)
    {
        if (id >= 0 && id < allItems.Length)
        {
            return allItems[id].isTriggered;
        }
        return false;
    }

    // --- 給 ItemTrigger 呼叫用：物件醒來時拿新圖片 ---
    public Sprite GetTriggeredSprite(int id)
    {
        if (id >= 0 && id < allItems.Length)
        {
            return allItems[id].triggeredSprite;
        }
        return null; // 如果 ID 錯誤就回傳空值
    }

    // (選用) 測試用：按 R 鍵重置所有狀態 (方便開發測試)
    /*
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            foreach (var item in allItems) item.isTriggered = false;
            Debug.Log("所有物品狀態已重置");
        }
    }
    */
}