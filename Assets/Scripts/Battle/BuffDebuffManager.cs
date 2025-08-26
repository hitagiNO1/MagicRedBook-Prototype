using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuffDebuffManager : MonoBehaviour
{
    public Unit playerUnit;
    public Unit enemyUnit;

    private bool isBuffed = false;
    private int originalDamage;
    private Dictionary<Unit, int> defenseBuffTurnsRemaining = new Dictionary<Unit, int>();
    private Dictionary<Unit, int> originalDefenseValues = new Dictionary<Unit, int>();
    private HashSet<Unit> isDefenseBuffed = new HashSet<Unit>();
    private Dictionary<Unit, int> originalDefenseValuesDebuff = new Dictionary<Unit, int>();
    private Dictionary<Unit, int> defenseDebuffTurns = new Dictionary<Unit, int>();
    private HashSet<Unit> isDefenseDebuffed = new HashSet<Unit>();
    private Dictionary<Unit, float> fixedReductionPercentages = new Dictionary<Unit, float>(); // 每個單位的減傷百分比
    private Dictionary<Unit, int> reductionTimesRemaining = new Dictionary<Unit, int>(); // 剩餘觸發次數
    private HashSet<Unit> hasFixedReduction = new HashSet<Unit>(); // 是否有此效果
    public int originalDefense;
    private Dictionary<Unit, int> poisonTurnsRemaining = new Dictionary<Unit, int>(); // 中毒剩餘回合
    private HashSet<Unit> isPoisoned = new HashSet<Unit>(); // 是否已有中毒效果
    private float poisonDamagePercentage = 0.15f; // 中毒傷害百分比（15%）

    void Start()
    {
      
    }

    void Update()
    {
 
    }

    public void ApplyDamageBuff(float multiplier)
    {
        if (!isBuffed)
        {
            originalDamage = playerUnit.damage;
            playerUnit.damage = Mathf.RoundToInt(playerUnit.damage * multiplier);
            isBuffed = true;
        }
    }

    public void RemoveDamageBuff()
    {
        if (isBuffed)
        {
            playerUnit.damage = originalDamage;
            isBuffed = false;
        }
    }

    public void ApplyDefenseDebuff(Unit targetUnit, float debuffMultiplier, int turns)
    {
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
        // 檢查目標單位是否有效
        if (targetUnit == null)
        {
            Debug.Log("治療目標無效！");
            return;
        }

        // 檢查目標是否存活
        if (targetUnit.currentHP <= 0)
        {
            Debug.Log($"{targetUnit.unitName} 已死亡，無法治療！");
            return;
        }

        // 檢查是否已滿血
        if (targetUnit.currentHP >= targetUnit.maxHP)
        {
            Debug.Log($"{targetUnit.unitName} 已滿血，無需治療！");
            return;
        }

        // 計算失去的血量
        int lostHealth = targetUnit.maxHP - targetUnit.currentHP;
        // 恢復失去血量的 75%
        int healAmount = Mathf.RoundToInt(lostHealth * lostper);
        // 計算新血量，但不超過最大血量
        int newHealth = Mathf.Min(targetUnit.currentHP + healAmount, targetUnit.maxHP);
        int actualHealedAmount = newHealth - targetUnit.currentHP;
        targetUnit.currentHP = newHealth;

        // 輸出治療訊息
        Debug.Log($"{targetUnit.unitName} 恢復了 {actualHealedAmount} 點血量（失去血量的 75%），當前血量：{targetUnit.currentHP}/{targetUnit.maxHP}");
    }

    public void ApplyDefenseBuff(Unit targetUnit, float multiplier, int turns)
    {
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

    // 移除固定減傷效果（當次數用完時自動呼叫）
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

    // 檢查並應用減傷（在單位受到傷害時呼叫，返回實際傷害量）
    public int ApplyReductionIfApplicable(Unit targetUnit, int incomingDamage)
    {
        if (!hasFixedReduction.Contains(targetUnit))
        {
            return incomingDamage; // 沒有效果，直接返回原傷害
        }

        float reductionPercentage = fixedReductionPercentages[targetUnit];
        int reducedDamage = Mathf.RoundToInt(incomingDamage * (1f - reductionPercentage)); // 計算減傷後傷害
        int actualReduction = incomingDamage - reducedDamage; // 實際減傷量

        // 減少剩餘次數
        reductionTimesRemaining[targetUnit]--;
        Debug.Log($"{targetUnit.unitName} 觸發固定減傷，減傷 {actualReduction} 點（{reductionPercentage * 100}%），剩餘次數：{reductionTimesRemaining[targetUnit]}");

        // 如果次數用完，移除效果
        if (reductionTimesRemaining[targetUnit] <= 0)
        {
            RemoveFixedDamageReduction(targetUnit);
        }

        return reducedDamage; // 返回減傷後的傷害量
    }

    public void ApplyPoison(Unit targetUnit, int turns = 3)
    {
        // 移除原本的檢查邏輯，直接應用或更新中毒效果
        poisonTurnsRemaining[targetUnit] = turns; // 重置或設置為 3 回合
        if (!isPoisoned.Contains(targetUnit))
        {
            isPoisoned.Add(targetUnit); // 如果是新中毒，加入集合
        }

        Debug.Log($"{targetUnit.unitName} {(isPoisoned.Contains(targetUnit) ? "重新中毒" : "中毒")}，每回合損失當前血量 {poisonDamagePercentage * 100}%，持續 {turns} 回合");
    }

    public void TickPoison()
    {
        List<Unit> unitsToRemove = new List<Unit>();
        foreach (Unit unit in new List<Unit>(poisonTurnsRemaining.Keys))
        {
            // 計算傷害：當前血量的 15%
            int poisonDamage = Mathf.RoundToInt(unit.currentHP * poisonDamagePercentage);
            unit.currentHP = Mathf.Max(0, unit.currentHP - poisonDamage); // 扣除傷害，最低為 0

            Debug.Log($"{unit.unitName} 受到中毒傷害 {poisonDamage} 點，當前血量：{unit.currentHP}/{unit.maxHP}");

            // 檢查是否死亡
            if (unit.currentHP <= 0)
            {
                Debug.Log($"{unit.unitName} 被中毒致死！");
                // 可以這裡觸發死亡邏輯，如果有相關事件
            }

            // 減少剩餘回合
            poisonTurnsRemaining[unit]--;
            Debug.Log($"{unit.unitName} 的中毒倒數：剩下 {poisonTurnsRemaining[unit]} 回合");

            if (poisonTurnsRemaining[unit] <= 0)
            {
                unitsToRemove.Add(unit);
                Debug.Log($"{unit.unitName} 的中毒效果結束");
            }
        }

        // 統一移除已結束的效果
        foreach (Unit unit in unitsToRemove)
        {
            poisonTurnsRemaining.Remove(unit);
            isPoisoned.Remove(unit);
        }
    }

    public void TickDefenseBuffs()
    {
        // 先抓出目前有 Buff 的單位清單
        List<Unit> buffedUnits = new List<Unit>(defenseBuffTurnsRemaining.Keys);
        List<Unit> unitsToRemove = new List<Unit>();

        foreach (Unit unit in buffedUnits)
        {
            if (defenseBuffTurnsRemaining.ContainsKey(unit))
            {
                defenseBuffTurnsRemaining[unit]--;

                int remaining = defenseBuffTurnsRemaining[unit];
                Debug.Log($"{unit.unitName} 的防禦 Buff 倒數：剩下 {remaining} 回合");

                if (remaining <= 0)
                {
                    unit.defense = originalDefenseValues[unit];
                    unitsToRemove.Add(unit);
                    Debug.Log($"{unit.unitName} 的防禦 Buff 結束，恢復為 {unit.defense}");
                }
            }
        }

        // 統一處理移除，避免在迴圈中修改集合
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


}