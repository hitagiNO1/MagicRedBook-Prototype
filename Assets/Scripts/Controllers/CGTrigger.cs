using UnityEngine;

public class CGTrigger : MonoBehaviour
{
    [Header("設定區")]
    [Tooltip("請對應 ItemManager 裡的 ID (0-5)")]
    public int itemID;

    private SpriteRenderer myRenderer;

    void Awake()
    {
        myRenderer = GetComponent<SpriteRenderer>();
    }

    // --- 關鍵邏輯：從 CG 場景回來時執行 ---
    // 當主場景重新被設為 Active，或物件被開啟時，這行會瞬間執行
    void OnEnable()
    {
        // 檢查是否需要換圖
        CheckAndRefreshImage();
    }
    // 檢查狀態並執行換圖
    void CheckAndRefreshImage()
    {
        // 防呆：確保 Manager 活著
        if (CGManager.Instance == null) return;

        // 如果這個 ID 在 ItemManager 裡被標記為 "已觸發" (由 CGText 設定)
        if (CGManager.Instance.IsItemTriggered(itemID))
        {
            Sprite newSprite = CGManager.Instance.GetTriggeredSprite(itemID);

            // 確保有新圖片且 Renderer 存在
            if (newSprite != null && myRenderer != null)
            {
                myRenderer.sprite = newSprite;
            }
        }
    }
}