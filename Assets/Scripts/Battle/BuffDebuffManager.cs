using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuffDebuffManager : MonoBehaviour
{
    public List<Unit> enemyUnits; // 新增敵人單位列表
    private Dictionary<Unit, int> defenseBuffTurnsRemaining = new Dictionary<Unit, int>();
    private Dictionary<Unit, int> originalDefenseValues = new Dictionary<Unit, int>();
    private HashSet<Unit> isDefenseBuffed = new HashSet<Unit>();
    private Dictionary<Unit, int> originalDefenseValuesDebuff = new Dictionary<Unit, int>();
    private Dictionary<Unit, int> defenseDebuffTurns = new Dictionary<Unit, int>();
    private HashSet<Unit> isDefenseDebuffed = new HashSet<Unit>();
    private Dictionary<Unit, float> fixedReductionPercentages = new Dictionary<Unit, float>();
    private Dictionary<Unit, int> reductionTimesRemaining = new Dictionary<Unit, int>();
    private HashSet<Unit> hasFixedReduction = new HashSet<Unit>();
    private Dictionary<Unit, int> poisonTurnsRemaining = new Dictionary<Unit, int>();
    private HashSet<Unit> isPoisoned = new HashSet<Unit>();
    private float poisonDamagePercentage = 0.15f;

    // 新增：攻擊 buff 管理
    private Dictionary<Unit, int> damageBuffTurnsRemaining = new Dictionary<Unit, int>();
    private Dictionary<Unit, int> originalDamageValues = new Dictionary<Unit, int>();
    private HashSet<Unit> isDamageBuffed = new HashSet<Unit>();
    // 新增：回血 Debuff 管理
    private Dictionary<Unit, int> healDebuffTurns = new Dictionary<Unit, int>();
    private Dictionary<Unit, float> originalHealMultiplier = new Dictionary<Unit, float>();
    private HashSet<Unit> isHealDebuffed = new HashSet<Unit>();
    private float healDebuffPercentage = 0.3f; // 預設降低 30% 回血效果
    // 新增：技能禁用管理
    private Dictionary<Unit, int> skillDisabledTurns = new Dictionary<Unit, int>();
    private HashSet<Unit> isSkillDisabled = new HashSet<Unit>();
    private Dictionary<Unit, float> originalCritChance = new Dictionary<Unit, float>();
    private Dictionary<Unit, float> critChanceBuffMultiplier = new Dictionary<Unit, float>();
    private Dictionary<Unit, int> critChanceBuffTurns = new Dictionary<Unit, int>();
    private HashSet<Unit> isCritChanceBuffed = new HashSet<Unit>();

    // 新增：專門記錄誰身上有「技能4(大雞腿)」的防禦 Buff
    public HashSet<Unit> skill4BuffUnits = new HashSet<Unit>();

    void Start()
    {
        if (enemyUnits == null)
        {
            enemyUnits = new List<Unit>();
        }
    }

    void Update()
    {
 
    }

    public void ApplyDamageBuff(Unit targetUnit, float multiplier, int turns)
    {
        if (targetUnit == null)
        {
            Debug.LogError("目標單位無效！");
            return;
        }
        if (isDamageBuffed.Contains(targetUnit))
        {
            Debug.Log($"{targetUnit.unitName} 已有攻擊 Buff，不重複套用。");
            return;
        }

        originalDamageValues[targetUnit] = targetUnit.damage;
        targetUnit.damage = Mathf.RoundToInt(targetUnit.damage * multiplier);
        damageBuffTurnsRemaining[targetUnit] = turns;
        isDamageBuffed.Add(targetUnit);

        Debug.Log($"{targetUnit.unitName} 的攻擊力上升至 {targetUnit.damage}，持續 {turns} 回合");
    }

    public void TickDamageBuffs()
    {
        List<Unit> unitsToRemove = new List<Unit>();
        foreach (Unit unit in new List<Unit>(damageBuffTurnsRemaining.Keys))
        {
            if (unit == null || unit.currentHP <= 0) // 檢查單位是否有效
            {
                unitsToRemove.Add(unit);
                continue;
            }

            damageBuffTurnsRemaining[unit]--;
            Debug.Log($"{unit.unitName} 的攻擊 Buff 倒數：剩下 {damageBuffTurnsRemaining[unit]} 回合");

            if (damageBuffTurnsRemaining[unit] <= 0)
            {
                unit.damage = originalDamageValues[unit];
                unitsToRemove.Add(unit);
                Debug.Log($"{unit.unitName} 的攻擊 Buff 結束，恢復為 {unit.damage}");
            }
        }

        foreach (Unit unit in unitsToRemove)
        {
            damageBuffTurnsRemaining.Remove(unit);
            originalDamageValues.Remove(unit);
            isDamageBuffed.Remove(unit);
        }
    }

    public void ApplyDefenseDebuff(Unit targetUnit, float debuffMultiplier, int turns)
    {
        if (targetUnit == null)
        {
            Debug.LogError("目標單位無效！");
            return;
        }
        if (isDefenseDebuffed.Contains(targetUnit))
        {
            Debug.Log($"{targetUnit.unitName} 已有防禦 Debuff，不重複套用。");
            return;
        }

        originalDefenseValuesDebuff[targetUnit] = targetUnit.defense;
        targetUnit.defense = Mathf.Max(1, Mathf.RoundToInt(targetUnit.defense * debuffMultiplier));
        defenseDebuffTurns[targetUnit] = turns;
        isDefenseDebuffed.Add(targetUnit);

        Debug.Log($"{targetUnit.unitName} 的防禦力被削弱至 {targetUnit.defense}，持續 {turns} 回合");
    }

    public void Heal(Unit targetUnit, float lostper)
    {
        if (targetUnit == null)
        {
            Debug.LogError("治療目標無效！");
            return;
        }
        if (targetUnit.currentHP <= 0)
        {
            Debug.Log($"{targetUnit.unitName} 已死亡，無法治療！");
            return;
        }
        if (targetUnit.currentHP >= targetUnit.maxHP)
        {
            Debug.Log($"{targetUnit.unitName} 已滿血，無需治療！");
            return;
        }

        int lostHealth = targetUnit.maxHP - targetUnit.currentHP;
        int healAmount = Mathf.RoundToInt(lostHealth * lostper);
        int reducedHeal = Mathf.RoundToInt(ApplyHealReduction(targetUnit, healAmount)); // 應用減益
        int newHealth = Mathf.Min(targetUnit.currentHP + reducedHeal, targetUnit.maxHP);
        int actualHealedAmount = newHealth - targetUnit.currentHP;
        targetUnit.currentHP = newHealth;

        Debug.Log($"{targetUnit.unitName} 恢復了 {actualHealedAmount} 點血量（失去血量的 {lostper * 100}%），當前血量：{targetUnit.currentHP}/{targetUnit.maxHP}");
    }

    public void ApplyDefenseBuff(Unit targetUnit, float multiplier, int turns)
    {
        if (targetUnit == null)
        {
            Debug.LogError("目標單位無效！");
            return;
        }
        if (isDefenseBuffed.Contains(targetUnit))
        {
            Debug.Log($"{targetUnit.unitName} 已有防禦 Buff，不重複套用。");
            return;
        }

        originalDefenseValues[targetUnit] = targetUnit.defense;
        targetUnit.defense = Mathf.RoundToInt(targetUnit.defense * multiplier);
        defenseBuffTurnsRemaining[targetUnit] = turns;
        isDefenseBuffed.Add(targetUnit);

        Debug.Log($"{targetUnit.unitName} 的防禦力上升至 {targetUnit.defense}，持續 {turns} 回合");
    }

    public void ApplyFixedDamageReduction(Unit targetUnit, float reductionPercentage, int times)
    {
        if (targetUnit == null)
        {
            Debug.LogError("目標單位無效！");
            return;
        }
        if (hasFixedReduction.Contains(targetUnit))
        {
            Debug.Log($"{targetUnit.unitName} 已有固定減傷效果，不重複套用。");
            return;
        }

        fixedReductionPercentages[targetUnit] = reductionPercentage;
        reductionTimesRemaining[targetUnit] = times;
        hasFixedReduction.Add(targetUnit);

        Debug.Log($"{targetUnit.unitName} 獲得固定減傷效果，減少 {reductionPercentage * 100}% 傷害，剩餘 {times} 次");
    }

    private void RemoveFixedDamageReduction(Unit targetUnit)
    {
        if (hasFixedReduction.Contains(targetUnit))
        {
            fixedReductionPercentages.Remove(targetUnit);
            reductionTimesRemaining.Remove(targetUnit);
            hasFixedReduction.Remove(targetUnit);
            Debug.Log($"{targetUnit.unitName} 的固定減傷效果結束");
        }
    }

    public int ApplyReductionIfApplicable(Unit targetUnit, int incomingDamage)
    {
        if (targetUnit == null || !hasFixedReduction.Contains(targetUnit))
        {
            return incomingDamage;
        }

        float reductionPercentage = fixedReductionPercentages[targetUnit];
        int reducedDamage = Mathf.RoundToInt(incomingDamage * (1f - reductionPercentage));
        int actualReduction = incomingDamage - reducedDamage;

        reductionTimesRemaining[targetUnit]--;
        Debug.Log($"{targetUnit.unitName} 觸發固定減傷，減傷 {actualReduction} 點（{reductionPercentage * 100}%），剩餘次數：{reductionTimesRemaining[targetUnit]}");

        if (reductionTimesRemaining[targetUnit] <= 0)
        {
            RemoveFixedDamageReduction(targetUnit);
        }

        return reducedDamage;
    }

    public void ApplyPoison(Unit targetUnit, int turns = 3)
    {
        if (targetUnit == null)
        {
            Debug.LogError("目標單位無效！");
            return;
        }
        if (isPoisoned.Contains(targetUnit))
        {
            Debug.Log($"{targetUnit.unitName} 已有中毒效果，重置回合數");
            poisonTurnsRemaining[targetUnit] = turns; // 重置回合數
        }
        else
        {
            poisonTurnsRemaining[targetUnit] = turns;
            isPoisoned.Add(targetUnit);
            Debug.Log($"{targetUnit.unitName} 中毒，每回合損失當前血量 {poisonDamagePercentage * 100}%，持續 {turns} 回合");
        }
    }

    public void TickPoison()
    {
        List<Unit> unitsToRemove = new List<Unit>();
        foreach (Unit unit in new List<Unit>(poisonTurnsRemaining.Keys))
        {
            if (unit == null || unit.currentHP <= 0)
            {
                unitsToRemove.Add(unit);
                continue;
            }

            int poisonDamage = Mathf.RoundToInt(unit.currentHP * poisonDamagePercentage);
            unit.currentHP = Mathf.Max(0, unit.currentHP - poisonDamage);

            Debug.Log($"{unit.unitName} 受到中毒傷害 {poisonDamage} 點，當前血量：{unit.currentHP}/{unit.maxHP}");

            if (unit.currentHP <= 0)
            {
                Debug.Log($"{unit.unitName} 被中毒致死！");
            }

            poisonTurnsRemaining[unit]--;
            Debug.Log($"{unit.unitName} 的中毒倒數：剩下 {poisonTurnsRemaining[unit]} 回合");

            if (poisonTurnsRemaining[unit] <= 0)
            {
                unitsToRemove.Add(unit);
                Debug.Log($"{unit.unitName} 的中毒效果結束");
            }
        }

        foreach (Unit unit in unitsToRemove)
        {
            poisonTurnsRemaining.Remove(unit);
            isPoisoned.Remove(unit);
        }
    }

    public void TickDefenseBuffs()
    {
        List<Unit> unitsToRemove = new List<Unit>();
        foreach (Unit unit in new List<Unit>(defenseBuffTurnsRemaining.Keys))
        {
            if (unit == null || unit.currentHP <= 0)
            {
                unitsToRemove.Add(unit);
                continue;
            }

            defenseBuffTurnsRemaining[unit]--;
            Debug.Log($"{unit.unitName} 的防禦 Buff 倒數：剩下 {defenseBuffTurnsRemaining[unit]} 回合");

            if (defenseBuffTurnsRemaining[unit] <= 0)
            {
                unit.defense = originalDefenseValues[unit];
                unitsToRemove.Add(unit);
                Debug.Log($"{unit.unitName} 的防禦 Buff 結束，恢復為 {unit.defense}");

                // --- 新增：如果是技能4的防禦結束，就觸發動畫 ---
                if (skill4BuffUnits.Contains(unit))
                {
                    skill4BuffUnits.Remove(unit); // 觸發過就從名單移除

                    // 這裡放你的破盾動畫，例如呼叫 EffectManager：
                    EffectManager.Instance.Def4back();
                    Debug.Log("觸發技能 4 大雞腿的防禦結束動畫！");
                }
            }
        }

        foreach (Unit unit in unitsToRemove)
        {
            defenseBuffTurnsRemaining.Remove(unit);
            originalDefenseValues.Remove(unit);
            isDefenseBuffed.Remove(unit);
        }
    }

    public void TickDefenseDebuffs()
    {
        List<Unit> unitsToRemove = new List<Unit>();
        foreach (Unit unit in new List<Unit>(defenseDebuffTurns.Keys))
        {
            if (unit == null || unit.currentHP <= 0)
            {
                unitsToRemove.Add(unit);
                continue;
            }

            defenseDebuffTurns[unit]--;
            Debug.Log($"{unit.unitName} 的防禦 Debuff 倒數：剩下 {defenseDebuffTurns[unit]} 回合");

            if (defenseDebuffTurns[unit] <= 0)
            {
                unit.defense = originalDefenseValuesDebuff[unit];
                unitsToRemove.Add(unit);
                Debug.Log($"{unit.unitName} 的防禦 Debuff 結束，恢復為 {unit.defense}");
            }
        }

        foreach (Unit unit in unitsToRemove)
        {
            defenseDebuffTurns.Remove(unit);
            originalDefenseValuesDebuff.Remove(unit);
            isDefenseDebuffed.Remove(unit);
        }
    }

    // 新增：清理所有狀態（戰鬥結束時呼叫）
    public void ClearAllEffects()
    {
        foreach (Unit unit in new List<Unit>(isDefenseBuffed))
        {
            if (unit != null)
            {
                unit.defense = originalDefenseValues[unit];
            }
            defenseBuffTurnsRemaining.Remove(unit);
            originalDefenseValues.Remove(unit);
            isDefenseBuffed.Remove(unit);
        }

        foreach (Unit unit in new List<Unit>(isDefenseDebuffed))
        {
            if (unit != null)
            {
                unit.defense = originalDefenseValuesDebuff[unit];
            }
            defenseDebuffTurns.Remove(unit);
            originalDefenseValuesDebuff.Remove(unit);
            isDefenseDebuffed.Remove(unit);
        }

        foreach (Unit unit in new List<Unit>(hasFixedReduction))
        {
            fixedReductionPercentages.Remove(unit);
            reductionTimesRemaining.Remove(unit);
            hasFixedReduction.Remove(unit);
        }

        foreach (Unit unit in new List<Unit>(isPoisoned))
        {
            poisonTurnsRemaining.Remove(unit);
            isPoisoned.Remove(unit);
        }

        foreach (Unit unit in new List<Unit>(isDamageBuffed))
        {
            if (unit != null)
            {
                unit.damage = originalDamageValues[unit];
            }
            damageBuffTurnsRemaining.Remove(unit);
            originalDamageValues.Remove(unit);
            isDamageBuffed.Remove(unit);
        }
        foreach (Unit unit in new List<Unit>(isHealDebuffed))
        {
            healDebuffTurns.Remove(unit);
            originalHealMultiplier.Remove(unit);
            isHealDebuffed.Remove(unit);
        }
        foreach (Unit unit in new List<Unit>(isSkillDisabled))
        {
            skillDisabledTurns.Remove(unit);
            isSkillDisabled.Remove(unit);
        }

        foreach (Unit unit in new List<Unit>(isCritChanceBuffed))
        {
            if (unit != null)
            {
                unit.critChance = originalCritChance[unit];
            }
            critChanceBuffTurns.Remove(unit);
            originalCritChance.Remove(unit);
            critChanceBuffMultiplier.Remove(unit);
            isCritChanceBuffed.Remove(unit);
        }
        skill4BuffUnits.Clear();
        Debug.Log("所有 Buff/Debuff 效果已清除");
    }

    public void ApplyEffectToAllEnemies(System.Action<Unit> effect)
    {
        if (enemyUnits == null) return;
        foreach (Unit enemy in enemyUnits)
        {
            if (enemy != null && enemy.currentHP > 0)
            {
                effect(enemy);
            }
        }
    }

    public void ApplyHealDebuff(Unit targetUnit, int turns)
    {
        if (targetUnit == null)
        {
            Debug.LogError("目標單位無效！");
            return;
        }
        if (isHealDebuffed.Contains(targetUnit))
        {
            Debug.Log($"{targetUnit.unitName} 已有回血 Debuff，重置回合數");
            healDebuffTurns[targetUnit] = turns; // 重置回合數
            return;
        }

        originalHealMultiplier[targetUnit] = 1.0f; // 假設初始回血倍率為 1.0，根據需求調整
        healDebuffTurns[targetUnit] = turns;
        isHealDebuffed.Add(targetUnit);

        Debug.Log($"{targetUnit.unitName} 的回血效果被削弱 {healDebuffPercentage * 100}%，持續 {turns} 回合");
    }

    public float ApplyHealReduction(Unit targetUnit, float healAmount)
    {
        if (targetUnit == null || !isHealDebuffed.Contains(targetUnit))
        {
            return healAmount; // 無減益，直接返回
        }

        float reducedHeal = healAmount * (1f - healDebuffPercentage);
        healDebuffTurns[targetUnit]--;
        Debug.Log($"{targetUnit.unitName} 觸發回血減益，減少 {healAmount - reducedHeal} 點治療，剩餘次數：{healDebuffTurns[targetUnit]}");

        if (healDebuffTurns[targetUnit] <= 0)
        {
            RemoveHealDebuff(targetUnit);
        }

        return reducedHeal;
    }

    private void RemoveHealDebuff(Unit targetUnit)
    {
        if (isHealDebuffed.Contains(targetUnit))
        {
            healDebuffTurns.Remove(targetUnit);
            originalHealMultiplier.Remove(targetUnit);
            isHealDebuffed.Remove(targetUnit);
            Debug.Log($"{targetUnit.unitName} 的回血 Debuff 結束");
        }
    }

    public void ApplySkillDisable(Unit targetUnit, int turns)
    {
        if (targetUnit == null)
        {
            Debug.LogError("目標單位無效！");
            return;
        }
        if (isSkillDisabled.Contains(targetUnit))
        {
            Debug.Log($"{targetUnit.unitName} 已有技能禁用效果，重置回合數");
            skillDisabledTurns[targetUnit] = turns; // 重置回合數
            return;
        }

        skillDisabledTurns[targetUnit] = turns;
        isSkillDisabled.Add(targetUnit);

        Debug.Log($"{targetUnit.unitName} 的技能被禁用，持續 {turns} 回合");
    }

    public bool IsSkillDisabled(Unit targetUnit)
    {
        return isSkillDisabled.Contains(targetUnit);
    }

    public void TickSkillDisable()
    {
        List<Unit> unitsToRemove = new List<Unit>();
        foreach (Unit unit in new List<Unit>(skillDisabledTurns.Keys))
        {
            if (unit == null || unit.currentHP <= 0)
            {
                unitsToRemove.Add(unit);
                continue;
            }

            skillDisabledTurns[unit]--;
            Debug.Log($"{unit.unitName} 的技能禁用倒數：剩下 {skillDisabledTurns[unit]} 回合");

            if (skillDisabledTurns[unit] <= 0)
            {
                unitsToRemove.Add(unit);
                Debug.Log($"{unit.unitName} 的技能禁用效果結束");
            }
        }

        foreach (Unit unit in unitsToRemove)
        {
            skillDisabledTurns.Remove(unit);
            isSkillDisabled.Remove(unit);
        }
    }

    public void ApplyCritChanceBuff(Unit targetUnit, float multiplier, int turns)
    {
        if (targetUnit == null)
        {
            Debug.LogError("目標單位無效！");
            return;
        }
        if (isCritChanceBuffed.Contains(targetUnit))
        {
            Debug.Log($"{targetUnit.unitName} 已有爆擊率 Buff，不重複套用。");
            return;
        }

        originalCritChance[targetUnit] = targetUnit.critChance;
        targetUnit.critChance = Mathf.Min(1.0f, targetUnit.critChance + (targetUnit.critChance * multiplier)); // 提升 50% 爆擊率
        critChanceBuffMultiplier[targetUnit] = multiplier;
        critChanceBuffTurns[targetUnit] = turns;
        isCritChanceBuffed.Add(targetUnit);

        Debug.Log($"{targetUnit.unitName} 的爆擊率提升至 {targetUnit.critChance * 100:F1}%，持續 {turns} 回合");
    }

    public void TickCritChanceBuffs()
    {
        List<Unit> unitsToRemove = new List<Unit>();
        foreach (Unit unit in new List<Unit>(critChanceBuffTurns.Keys))
        {
            if (unit == null || unit.currentHP <= 0)
            {
                unitsToRemove.Add(unit);
                continue;
            }

            critChanceBuffTurns[unit]--;
            Debug.Log($"{unit.unitName} 的爆擊率 Buff 倒數：剩下 {critChanceBuffTurns[unit]} 回合");

            if (critChanceBuffTurns[unit] <= 0)
            {
                unit.critChance = originalCritChance[unit];
                unitsToRemove.Add(unit);
                Debug.Log($"{unit.unitName} 的爆擊率 Buff 結束，恢復為 {unit.critChance * 100:F1}%");
            }
        }

        foreach (Unit unit in unitsToRemove)
        {
            critChanceBuffTurns.Remove(unit);
            originalCritChance.Remove(unit);
            critChanceBuffMultiplier.Remove(unit);
            isCritChanceBuffed.Remove(unit);
        }
    }
}