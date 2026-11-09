using System.Collections.Generic;
using UnityEngine;

public class StatsManager : MonoBehaviour
{
    [Header("基礎數值設定 (可在 Unity 面板直接調整)")]
    public float baseAttack = 10f;
    public float baseDefense = 5f;
    public float baseCritRate = 0.05f;     // 0.05 代表 5%
    public float baseCritDamage = 1.5f;    // 1.5 代表 150%

    // 用字典 (Dictionary) 來管理所有的數值，方便透過 StatType 快速查詢
    private Dictionary<StatType, CharacterStat> statsDict;

    private void Awake()
    {
        // 在遊戲開始時，初始化所有數值
        InitializeStats();
    }

    private void InitializeStats()
    {
        statsDict = new Dictionary<StatType, CharacterStat>
        {
            { StatType.Attack, new CharacterStat(baseAttack) },
            { StatType.Defense, new CharacterStat(baseDefense) },
            { StatType.CritRate, new CharacterStat(baseCritRate) },
            { StatType.CritDamage, new CharacterStat(baseCritDamage) }
        };
    }

    /// <summary>
    /// 取得某項數值的最終結果 (外部最常呼叫的方法)
    /// </summary>
    public float GetStatValue(StatType type)
    {
        if (statsDict.ContainsKey(type))
        {
            return statsDict[type].Value;
        }

        Debug.LogWarning($"角色缺少 {type} 的數值設定！");
        return 0f;
    }

    /// <summary>
    /// 取得完整的 CharacterStat 物件 (通常用於需要詳細操作時)
    /// </summary>
    public CharacterStat GetStat(StatType type)
    {
        if (statsDict.ContainsKey(type))
        {
            return statsDict[type];
        }
        return null;
    }

    /// <summary>
    /// 對指定的數值新增修飾者
    /// </summary>
    public void AddModifier(StatType type, StatModifier modifier)
    {
        if (statsDict.ContainsKey(type))
        {
            statsDict[type].AddModifier(modifier);
        }
    }

    /// <summary>
    /// 移除指定的修飾者
    /// </summary>
    public void RemoveModifier(StatType type, StatModifier modifier)
    {
        if (statsDict.ContainsKey(type))
        {
            statsDict[type].RemoveModifier(modifier);
        }
    }

    /// <summary>
    /// 一次性移除所有來自特定來源的修飾者 (非常重要！)
    /// 用法：脫裝備時傳入裝備物件，解除 Buff 時傳入 Buff 物件
    /// </summary>
    public void RemoveAllModifiersFromSource(object source)
    {
        // 遍歷字典裡面的所有數值，把該來源的修飾者全部拔除
        foreach (var stat in statsDict.Values)
        {
            stat.RemoveAllModifiersFromSource(source);
        }
    }
}