using System; // ★ 必須引入 System 才能使用 Action
using System.Collections;
using UnityEngine;

public class CGManager : MonoBehaviour
{
    public static CGManager Instance;

    // ==========================================
    // ★ 廣播頻道：當有 CG 被觸發時，透過這個頻道通知所有 CGTrigger
    // ==========================================
    public event Action<int> OnCGStateChanged;

    [System.Serializable]
    public class ItemData
    {
        public GameObject CG;
        public bool isTriggered;
        public Sprite triggeredSprite;

        [Header("可選：觸發完成後要播放的劇情")]
        public StoryScene itemStory;

        [Tooltip("觸發後延遲幾秒才開始播劇情")]
        public float delayTime = 2f;
    }

    public ItemData[] allItems;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 呼叫這個函式來標記物品已觸發 (通常由小遊戲過關時呼叫)
    /// </summary>
    public void SetItemTriggered(int id)
    {
        if (id >= 0 && id < allItems.Length)
        {
            // 1. 更新資料庫裡的狀態
            allItems[id].isTriggered = true;
            Debug.Log($"[CGManager] 物品 ID: {id} 已標記為觸發完成！");

            // 2. ★ 發出廣播：通知場景中所有正在收聽的 CGTrigger 立刻換圖！
            // (使用 ?.Invoke 代表如果沒有人收聽，就不會報錯，非常安全)
            OnCGStateChanged?.Invoke(id);

            // 3. 如果這個物品有設定劇情，就呼叫「協程」來延遲播放
            if (allItems[id].itemStory != null)
            {
                Debug.Log($"[CGManager] 準備在 {allItems[id].delayTime} 秒後播放劇情...");
                StartCoroutine(PlayStoryWithDelay(allItems[id].itemStory, allItems[id].delayTime));
            }
        }
        else
        {
            Debug.LogError($"[CGManager] 錯誤的 ID: {id}，請檢查陣列範圍。");
        }
    }

    // --- 專門用來倒數並播放劇情的協程 ---
    private IEnumerator PlayStoryWithDelay(StoryScene storyToPlay, float delay)
    {
        // 1. 等待指定的時間 (例如 2 秒)
        yield return new WaitForSeconds(delay);

        // 2. 時間到了，檢查 GameController 是否存在並播放
        if (GameController.Instance != null)
        {
            GameController.Instance.PlayScene(storyToPlay);
        }
        else
        {
            Debug.LogWarning("[CGManager] 找不到 GameController.Instance，無法播放劇情！");
        }
    }

    // --- 給其他腳本 (如 CGTrigger) 獲取狀態用的函式 ---

    public bool IsItemTriggered(int id)
    {
        if (id >= 0 && id < allItems.Length)
        {
            return allItems[id].isTriggered;
        }
        return false;
    }

    public Sprite GetTriggeredSprite(int id)
    {
        if (id >= 0 && id < allItems.Length)
        {
            return allItems[id].triggeredSprite;
        }
        return null;
    }
}