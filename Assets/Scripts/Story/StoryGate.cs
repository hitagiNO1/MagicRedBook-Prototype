using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class StoryGate : MonoBehaviour
{
    private bool isTriggered = false;

    [Header("要播放的 Timeline")]
    public PlayableAsset asset;

    [Header("限制：這些劇情都播過且結束才能播放 Timeline")]
    public List<GameScene> LimitScenes;

    void Update()
    {
        // 確保只會觸發一次
        if (!isTriggered && AllLimitScenesPlayed())
        {
            TimelineManager.Instance.PlayTimeline(asset);
            isTriggered = true;
        }
    }

    /// <summary>
    /// 檢查所有限制劇情是否都播過且結束 (TryGetValue 版本)
    /// </summary>
    private bool AllLimitScenesPlayed()
    {
        foreach (GameScene scene in LimitScenes)
        {
            bool played;
            // 如果沒紀錄，或值不是 true → 還沒完整播完
            if (!GameController.Instance.ScenePlayedState.TryGetValue(scene, out played) || !played)
            {
                return false;
            }
        }
        return true;
    }
}
