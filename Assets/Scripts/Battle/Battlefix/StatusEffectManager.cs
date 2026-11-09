using System.Collections.Generic;
using UnityEngine;

// 確保掛載這個腳本的物件上，也一定有 StatsManager
[RequireComponent(typeof(StatsManager))]
public class StatusEffectManager : MonoBehaviour
{
    private StatsManager statsManager;

    // 儲存目前角色身上的所有狀態
    private List<StatusEffect> activeEffects = new List<StatusEffect>();

    private void Awake()
    {
        statsManager = GetComponent<StatsManager>();
    }

    /// <summary>
    /// 對角色施加新的狀態
    /// </summary>
    public void AddEffect(StatusEffect effect)
    {
        // 實務上這裡還可以加入「檢查同名 Buff 是否覆蓋或疊加回合數」的邏輯
        effect.Initialize(statsManager);
        activeEffects.Add(effect);
    }

    /// <summary>
    /// 由戰鬥系統的 State Machine 在「該角色回合結束」時統一呼叫
    /// </summary>
    public void ProcessTurnEnd()
    {
        // 【注意】必須從 List 的後面往前跑迴圈！
        // 因為如果狀態結束，我們會將它從 List 移除，從前面跑會導致索引值錯亂跳過項目。
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            bool isFinished = activeEffects[i].OnTurnEndTick();

            if (isFinished)
            {
                activeEffects.RemoveAt(i);
            }
        }
    }
}