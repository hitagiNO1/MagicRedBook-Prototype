using System;
using System.Collections.Generic;

public class CharacterStat
{
    // 基礎數值 (角色剛出生、脫光裝備時的原始數值)
    public float BaseValue { get; private set; }

    // 儲存目前影響這個數值的所有修飾者
    private readonly List<StatModifier> statModifiers;

    // 髒標記 (Dirty Flag)：用來判斷數值是否需要重新計算
    private bool isDirty = true;

    // 快取值：儲存上一次計算好的最終結果
    private float lastCalculatedValue;

    // 建構子：建立這個數值時，給予一個初始基礎值
    public CharacterStat(float baseValue)
    {
        BaseValue = baseValue;
        statModifiers = new List<StatModifier>();
    }

    // 外部獲取「最終數值」的唯一屬性 (唯讀)
    public float Value
    {
        get
        {
            // 只有當 isDirty 為 true 時 (代表裝備或 Buff 有變動)，才重新計算
            if (isDirty)
            {
                lastCalculatedValue = CalculateFinalValue();
                isDirty = false;
            }
            return lastCalculatedValue;
        }
    }

    // 加入修飾者 (例如：穿上裝備、獲得 Buff)
    public void AddModifier(StatModifier mod)
    {
        statModifiers.Add(mod);
        isDirty = true; // 數值改變了，標記為需要重新計算
    }

    // 移除指定的修飾者
    public bool RemoveModifier(StatModifier mod)
    {
        if (statModifiers.Remove(mod))
        {
            isDirty = true;
            return true;
        }
        return false;
    }

    // 一次性移除某個來源的所有修飾者 (非常實用：用在脫下裝備或解除狀態時)
    public bool RemoveAllModifiersFromSource(object source)
    {
        bool didRemove = false;

        // 從後面往前刪除，避免 List 索引錯亂
        for (int i = statModifiers.Count - 1; i >= 0; i--)
        {
            if (statModifiers[i].Source == source)
            {
                statModifiers.RemoveAt(i);
                didRemove = true;
            }
        }

        if (didRemove)
        {
            isDirty = true;
        }
        return didRemove;
    }

    // 核心計算邏輯
    private float CalculateFinalValue()
    {
        float finalValue = BaseValue;
        float percentAddSum = 0;

        // 步驟 1：先加總所有的 Flat (固定值增加)
        // 例如：基礎攻擊 10 + 鐵劍(Flat 10) = 20
        for (int i = 0; i < statModifiers.Count; i++)
        {
            if (statModifiers[i].Type == ModifierType.Flat)
            {
                finalValue += statModifiers[i].Value;
            }
        }

        // 步驟 2：收集所有的 PercentAdd (百分比加法) 並一次性相乘
        // 例如：狂暴Buff(+20%) + 戰吼Buff(+30%) = 總共加 50%
        for (int i = 0; i < statModifiers.Count; i++)
        {
            if (statModifiers[i].Type == ModifierType.PercentAdd)
            {
                percentAddSum += statModifiers[i].Value;
            }
        }
        finalValue *= (1 + percentAddSum);

        // 步驟 3：依序乘上所有的 PercentMult (百分比乘法)
        // 通常用於最終傷害放大，例如：爆擊傷害倍率 (x1.5)
        for (int i = 0; i < statModifiers.Count; i++)
        {
            if (statModifiers[i].Type == ModifierType.PercentMult)
            {
                finalValue *= (1 + statModifiers[i].Value);
            }
        }

        // 四捨五入到小數點後四位，避免浮點數誤差 (例如 10.0000001)
        return (float)Math.Round(finalValue, 4);
    }
}