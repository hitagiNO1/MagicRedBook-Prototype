using UnityEngine;

public static class CombatCalculator
{
    /// <summary>
    /// 計算最終傷害的主程式
    /// </summary>
    /// <param name="attacker">攻擊方的數值管理器</param>
    /// <param name="defender">防禦方的數值管理器</param>
    /// <returns>計算完成的最終傷害數值</returns>
    public static float CalculatePhysicalDamage(StatsManager attacker, StatsManager defender)
    {
        // 【步驟 1】取得攻擊方數值
        float atk = attacker.GetStatValue(StatType.Attack);
        float critRate = attacker.GetStatValue(StatType.CritRate);
        float critDmg = attacker.GetStatValue(StatType.CritDamage);

        // 【步驟 2】判斷是否爆擊 (Unity 的 Random.value 會回傳 0.0 ~ 1.0 的小數)
        bool isCritical = Random.value < critRate;

        // 基礎傷害計算
        float rawDamage = atk;
        if (isCritical)
        {
            rawDamage *= critDmg; // 如果爆擊，乘上爆擊傷害倍率
            Debug.Log("爆擊觸發！");
        }

        // 【步驟 3】取得防禦方數值
        float def = defender.GetStatValue(StatType.Defense);

        // 【步驟 4】查詢你的 CSV 表單，取得減傷比例
        float reductionPercent = DefenseCSVReader.Instance.GetDamageReduction(def);

        // 防禦機制安全鎖：確保減傷最多不超過 100% (通常遊戲會鎖在 80%~90% 避免打不死)
        reductionPercent = Mathf.Clamp(reductionPercent, 0f, 0.9f);

        // 【步驟 5】計算最終傷害
        // 公式：最終傷害 = 原始傷害 * (1 - 減傷比例)
        float finalDamage = rawDamage * (1f - reductionPercent);

        // 確保傷害最低為 1 (避免打出 0 或負數傷害)
        return Mathf.Max(finalDamage, 1f);
    }

    public static float CalculateSkillDamage(StatsManager attacker, StatsManager defender, SkillData skill)
    {
        // 1. 取得攻擊方數值
        float atk = attacker.GetStatValue(StatType.Attack);
        float critRate = attacker.GetStatValue(StatType.CritRate);
        float critDmg = attacker.GetStatValue(StatType.CritDamage);

        // 2. 判斷爆擊
        bool isCritical = Random.value < critRate;

        // 3. 基礎傷害 = 攻擊力 * 技能倍率
        float rawDamage = atk * skill.damageMultiplier;
        if (isCritical)
        {
            rawDamage *= critDmg;
        }

        // 4. 取得防禦方數值與 CSV 減傷 (沿用之前的邏輯)
        float def = defender.GetStatValue(StatType.Defense);
        float reductionPercent = DefenseCSVReader.Instance.GetDamageReduction(def);
        reductionPercent = Mathf.Clamp(reductionPercent, 0f, 0.9f);

        // 5. 最終結算
        float finalDamage = rawDamage * (1f - reductionPercent);
        return Mathf.Max(finalDamage, 1f);
    }
}