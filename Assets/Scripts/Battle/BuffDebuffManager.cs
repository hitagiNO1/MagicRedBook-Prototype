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

    public int originalDefense;
    

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

    /*public void RemoveDefenseDebuff()
    {
        if (isDefenseDebuffed)
        {
            enemyUnit.defense = originalDefense;
            isDefenseDebuffed = false;
            Debug.Log($"攻擊Buff移除：還原為 = {originalDamage}");
        }
    }*/

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