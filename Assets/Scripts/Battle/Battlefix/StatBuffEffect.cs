using UnityEngine;

public class StatBuffEffect : StatusEffect
{
    private StatType targetStatType;
    private float buffValue;
    private ModifierType modifierType;

    public StatBuffEffect(string name, int durationTurns, StatType statType, float value, ModifierType modType)
        : base(name, durationTurns)
    {
        targetStatType = statType;
        buffValue = value;
        modifierType = modType;
    }

    protected override void OnApply()
    {
        // 【核心精華】：把 "this" (這個 Buff 物件自己) 當作 Source 傳進去
        StatModifier mod = new StatModifier(buffValue, modifierType, this);
        targetStats.AddModifier(targetStatType, mod);

        Debug.Log($"{EffectName} 應用成功！ {targetStatType} 提升了，持續 {RemainingTurns} 回合。");
    }

    protected override void OnRemove()
    {
        // 時間到或被淨化時，呼叫數值管理器，把來源是 "this" 的修飾者全部拔掉！
        targetStats.RemoveAllModifiersFromSource(this);

        Debug.Log($"{EffectName} 效果結束，數值已恢復。");
    }
}