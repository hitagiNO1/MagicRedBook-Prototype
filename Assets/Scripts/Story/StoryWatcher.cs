using UnityEngine;

public class StoryWatcher : MonoBehaviour
{
    [Header("要檢查的故事")]
    public GameScene targetScene;   // 你要偵測的故事是哪個

    [Header("要啟用的物件")]
    public SmoothFollow RedBookFollow;
    public FollowAI LylaFollow;

    private bool alreadyEnabled = false;

    void Update()
    {
        if (alreadyEnabled) return; // 已經啟用過就不用再跑

        // 確保 GameController 有實例
        if (GameController.Instance == null) return;

        // 查詢 GameController 裡紀錄的故事播放狀態
        bool played;
        if (GameController.Instance.ScenePlayedState.TryGetValue(targetScene, out played))
        {
            if (played) // 如果故事已經播完
            {
                RedBookFollow.SetFollow(false);
                LylaFollow.enabled = true; // 啟用 FollowAI
                alreadyEnabled = true; // 防止重複觸發
            }
        }
    }
}
