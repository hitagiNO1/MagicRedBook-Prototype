using UnityEngine;

public abstract class StatusEffect
{
    public string EffectName { get; protected set; }
    public int RemainingTurns { get; protected set; }

    // 紀錄這個狀態是掛在哪個角色的數值管理器上
    protected StatsManager targetStats;

    public StatusEffect(string name, int durationTurns)
    {
        EffectName = name;
        RemainingTurns = durationTurns;
    }

    /// <summary>
    /// 初始化：當狀態被掛到角色身上時呼叫
    /// </summary>
    public virtual void Initialize(StatsManager target)
    {
        targetStats = target;
        OnApply(); // 觸發具體效果
    }

    // 交給子類別實作的具體行為
    protected abstract void OnApply();
    protected abstract void OnRemove();

    /// <summary>
    /// 戰鬥系統每回合呼叫一次。回傳 true 代表狀態已到期，需要被移除。
    /// </summary>
    public virtual bool OnTurnEndTick()
    {
        RemainingTurns--;

        if (RemainingTurns <= 0)
        {
            OnRemove();
            return true;
        }
        return false;
    }

    /// <summary>
    /// 供外部呼叫的強制移除 (例如被「淨化」技能解除時)
    /// </summary>
    public void ForceRemove()
    {
        OnRemove();
    }
}