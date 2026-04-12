using System.Collections; // ★ 確保有引入這個命名空間來使用協程
using UnityEngine;

public class CGManager : MonoBehaviour
{
    public static CGManager Instance;

    [System.Serializable]
    public class ItemData
    {
        public GameObject CG;
        public bool isTriggered;
        public Sprite triggeredSprite;

        [Header("可選：觸發完成後要播放的劇情")]
        public StoryScene itemStory;

        // ★ 新增：延遲播放的時間，預設為 2 秒
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

    public void SetItemTriggered(int id)
    {
        if (id >= 0 && id < allItems.Length)
        {
            allItems[id].isTriggered = true;
            Debug.Log($"[ItemManager] 物品 ID: {id} 已標記為觸發完成！");

            // 如果這個物品有設定劇情，就呼叫「協程」來延遲播放
            if (allItems[id].itemStory != null)
            {
                Debug.Log($"[ItemManager] 準備在 {allItems[id].delayTime} 秒後播放劇情...");

                // ★ 啟動延遲播放的協程
                StartCoroutine(PlayStoryWithDelay(allItems[id].itemStory, allItems[id].delayTime));
            }
        }
        else
        {
            Debug.LogError($"[ItemManager] 錯誤的 ID: {id}，請檢查陣列範圍。");
        }
    }

    // ★ 新增功能：專門用來倒數並播放劇情的協程
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
            Debug.LogWarning("[ItemManager] 找不到 GameController.Instance，無法播放劇情！");
        }
    }

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