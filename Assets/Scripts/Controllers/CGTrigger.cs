using UnityEngine;

public class CGTrigger : MonoBehaviour
{
    [Header("設定區")]
    public int itemID;
    private SpriteRenderer myRenderer;

    void Awake()
    {
        myRenderer = GetComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
        // 1. 醒來的第一件事：先檢查一次現有狀態
        CheckAndRefreshImage();

        // 2. 訂閱廣播頻道：只要 CGManager 有廣播，就去執行 OnStateChanged 函式
        if (CGManager.Instance != null)
        {
            CGManager.Instance.OnCGStateChanged += OnStateChanged;
        }
    }

    void OnDisable()
    {
        // ★ 好習慣：物件關閉時要退訂廣播，避免報錯
        if (CGManager.Instance != null)
        {
            CGManager.Instance.OnCGStateChanged -= OnStateChanged;
        }
    }

    // 當收到廣播時執行的動作
    private void OnStateChanged(int triggeredID)
    {
        // 檢查廣播的 ID 是不是自己負責的 ID
        if (triggeredID == itemID)
        {
            CheckAndRefreshImage();
        }
    }

    public void CheckAndRefreshImage()
    {
        if (CGManager.Instance == null) return;

        if (CGManager.Instance.IsItemTriggered(itemID))
        {
            Sprite newSprite = CGManager.Instance.GetTriggeredSprite(itemID);

            if (newSprite != null && myRenderer != null)
            {
                myRenderer.sprite = newSprite;
            }
        }
    }
}