using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random; // 添加別名，解決模擬兩可問題

public class Enemyskill : MonoBehaviour
{
    public List<Unit> enemyUnits;
    public List<Unit> playerUnits;
    public BuffDebuffManager buffDebuffManager;

    // 新增：技能冷卻字典，假設每個敵人技能有獨立冷卻
    private Dictionary<string, int> skillCooldowns = new Dictionary<string, int>();

    // 新增：執行敵人技能的主要方法
    public bool ExecuteEnemySkill(Unit enemyUnit)
    {
        if (enemyUnit == null)
        {
            Debug.LogError("敵人單位無效！");
            return false;
        }

        string enemyName = enemyUnit.unitName;

        if (enemyName == "SOFT")
        {
            return ExecuteSoftSkills(enemyUnit);
        }
        else
        {
            // 對於其他敵人，隨機選擇一個技能執行
            List<Func<bool>> skills = GetEnemySkills(enemyName);
            if (skills.Count > 0)
            {
                int randomIndex = Random.Range(0, skills.Count);
                return skills[randomIndex]();
            }
        }

        return false;
    }

    // 新增：專門處理 SOFT 敵人的技能選擇邏輯
    private bool ExecuteSoftSkills(Unit self)
    {
        // 創建有冷卻的技能列表（優先技能）
        List<Func<bool>> prioritySkills = new List<Func<bool>>
        {
            () => CheckAndPerformSoftskill_2(self),
            () => CheckAndPerformSoftskill_3(self)
        };

        // 隨機打亂優先技能順序
        for (int i = 0; i < prioritySkills.Count; i++)
        {
            int randomIndex = Random.Range(0, prioritySkills.Count);
            var temp = prioritySkills[i];
            prioritySkills[i] = prioritySkills[randomIndex];
            prioritySkills[randomIndex] = temp;
        }

        // 嘗試執行優先技能
        foreach (var skill in prioritySkills)
        {
            if (skill()) // 如果技能執行成功
            {
                return true;
            }
        }

        // 如果所有優先技能都在冷卻中，執行備用技能 Softskill_1
        Debug.Log($"{self.unitName} 的所有特殊技能都在冷卻中，使用備用技能 Softskill_1");
        PerformSoftskill_1(self);
        return true;
    }

    public List<Func<bool>> GetEnemySkills(string enemyName)  // 修改為 Func<bool>
    {
        List<Func<bool>> skills = new List<Func<bool>>();
        Unit self = GetComponent<Unit>();

        if (self == null)
        {
            Debug.LogError("Enemyskill 缺少 Unit 組件！");
            return skills;
        }

        if (enemyName == "FISH")
        {
            skills.Add(() => { PerformFishskill_1(self); return true; });
            skills.Add(() => { PerformFishskill_2(self); return true; });
        }
        else if (enemyName == "SNAKE")
        {
            skills.Add(() => { PerformSnakeskill_1(self); return true; });
            skills.Add(() => { PerformSnakeskill_2(self); return true; });
        }
        else if (enemyName == "CLOCK")
        {
            skills.Add(() => { PerformClockskill_1(self); return true; });
            skills.Add(() => { PerformClockskill_2(self); return true; });
        }
        else if (enemyName == "SOFT")
        {
            skills.Add(() => { PerformSoftskill_1(self); return true; });
            skills.Add(() => CheckAndPerformSoftskill_2(self));  // 返回 bool
            skills.Add(() => CheckAndPerformSoftskill_3(self));  // 返回 bool
        }
        else
        {
            Debug.LogWarning($"未知的敵人名稱: {enemyName}，無技能分配");
        }

        return skills;
    }

    private bool CheckAndPerformSoftskill_2(Unit self)
    {
        string skillKey = $"{self.unitName}_Softskill_2";
        if (!skillCooldowns.ContainsKey(skillKey))
        {
            skillCooldowns[skillKey] = 0;
        }

        if (skillCooldowns[skillKey] > 0)
        {
            Debug.Log($"{self.unitName} 的 Softskill_2 冷卻中，剩餘 {skillCooldowns[skillKey]} 回合");
            return false; // 返回 false，表示未執行
        }

        PerformSoftskill_2(self);
        skillCooldowns[skillKey] = 2; // 設置 2 回合冷卻
        return true; // 返回 true，表示成功執行
    }

    private bool CheckAndPerformSoftskill_3(Unit self)
    {
        string skillKey = $"{self.unitName}_Softskill_3";
        if (!skillCooldowns.ContainsKey(skillKey))
        {
            skillCooldowns[skillKey] = 0;
        }

        if (skillCooldowns[skillKey] > 0)
        {
            Debug.Log($"{self.unitName} 的 Softskill_3 冷卻中，剩餘 {skillCooldowns[skillKey]} 回合");
            return false; // 返回 false，表示未執行
        }

        PerformSoftskill_3(self);
        skillCooldowns[skillKey] = 2; // 設置 2 回合冷卻
        return true; // 返回 true，表示成功執行
    }

    private void PerformFishskill_1(Unit self)
    {
        if (playerUnits == null || playerUnits.Count == 0)
        {
            Debug.LogError("playerUnits 未初始化或為空！");
            return;
        }

        Unit target = GetRandomValidTarget(playerUnits);
        if (target != null)
        {
            PerformBasicAttack(self, target, self.damage);
            EffectManager.Instance.PlayUIEffect("Fish1", new Vector2(-805, -352));
        }
    }

    private void PerformFishskill_2(Unit self)
    {
        if (playerUnits == null || playerUnits.Count == 0)
        {
            Debug.LogError("playerUnits 未初始化或為空！");
            return;
        }

        if (buffDebuffManager == null)
        {
            Debug.LogError("buffDebuffManager 未初始化！");
            return;
        }

        Unit target = GetRandomValidTarget(playerUnits);
        if (target != null)
        {
            int damage = Mathf.RoundToInt(self.damage * 1.2f);
            PerformBasicAttack(self, target, damage);
            EffectManager.Instance.PlayUIEffect("Fish2", new Vector2(-708, -206));
        }
    }

    private void PerformSnakeskill_1(Unit self)
    {
        if (playerUnits == null || playerUnits.Count == 0)
        {
            Debug.LogError("playerUnits 未初始化或為空！");
            return;
        }

        Unit target = GetRandomValidTarget(playerUnits);
        if (target != null)
        {
            PerformBasicAttack(self, target, self.damage * 2);
            EffectManager.Instance.PlayUIEffect("Snake1", new Vector2(-677, -230));
        }
    }

    private void PerformSnakeskill_2(Unit self)
    {
        if (playerUnits == null || playerUnits.Count == 0)
        {
            Debug.LogError("playerUnits 未初始化或為空！");
            return;
        }

        if (buffDebuffManager == null)
        {
            Debug.LogError("buffDebuffManager 未初始化！");
            return;
        }

        Unit target = GetRandomValidTarget(playerUnits);
        if (target != null)
        {
            EffectManager.Instance.PlayUIEffect("Snake2", new Vector2(-800, -450));
            buffDebuffManager.ApplyPoison(target);
        }
    }

    private void PerformClockskill_1(Unit self)
    {
        if (playerUnits == null || playerUnits.Count == 0)
        {
            Debug.LogError("playerUnits 未初始化或為空！");
            return;
        }

        Unit target = GetRandomValidTarget(playerUnits);
        if (target != null)
        {
            PerformBasicAttack(self, target, self.damage * 2);
            EffectManager.Instance.PlayUIEffect("Clock1", new Vector2(-696, -100));
        }
    }

    private void PerformClockskill_2(Unit self)
    {
        if (playerUnits == null || playerUnits.Count == 0)
        {
            Debug.LogError("playerUnits 未初始化或為空！");
            return;
        }

        if (buffDebuffManager == null)
        {
            Debug.LogError("buffDebuffManager 未初始化！");
            return;
        }

        Unit target = GetRandomValidTarget(playerUnits);
        if (target != null)
        {
            buffDebuffManager.ApplyDamageBuff(target, 0.7f, 2);
            buffDebuffManager.ApplyHealDebuff(target, 2);
            int damage = Mathf.RoundToInt(self.damage * 1.5f);
            PerformBasicAttack(self, target, damage);
            EffectManager.Instance.PlayUIEffect("Clock2", new Vector2(0, 0));
            Debug.Log($"{self.unitName} 對 {target.unitName} 使用 Clockskill_2：降低攻擊 30%、回血 30%，造成 {damage} 傷害");
        }
    }

    private void PerformSoftskill_1(Unit self)
    {
        if (playerUnits == null || playerUnits.Count == 0)
        {
            Debug.LogError("playerUnits 未初始化或為空！");
            return;
        }

        Unit target = GetRandomValidTarget(playerUnits);
        if (target != null)
        {
            PerformBasicAttack(self, target, self.damage * 2);
            Debug.Log($"{self.unitName} 使用 Softskill_1：對 {target.unitName} 造成 {self.damage * 2} 傷害");
        }
    }

    private void PerformSoftskill_2(Unit self)
    {
        if (playerUnits == null || playerUnits.Count == 0)
        {
            Debug.LogError("playerUnits 未初始化或為空！");
            return;
        }

        if (buffDebuffManager == null)
        {
            Debug.LogError("buffDebuffManager 未初始化！");
            return;
        }

        List<Unit> validTargets = playerUnits.FindAll(unit => unit != null && unit.currentHP > 0);
        if (validTargets.Count == 0)
        {
            Debug.LogWarning("無有效的玩家目標！");
            return;
        }

        Unit target = validTargets[Random.Range(0, validTargets.Count)];
        buffDebuffManager.ApplySkillDisable(target, 1);
        int damage = Mathf.RoundToInt(self.damage * 0.5f);
        PerformBasicAttack(self, target, damage);
        EffectManager.Instance.PlayUIEffect("Blanket2", new Vector2(0f, 571.11f));
        Debug.Log($"{self.unitName} 對 {target.unitName} 使用 Softskill_2：禁用技能 1 回合，造成 {damage} 傷害");
    }

    private void PerformSoftskill_3(Unit self)
    {
        if (playerUnits == null || playerUnits.Count == 0)
        {
            Debug.LogError("playerUnits 未初始化或為空！");
            return;
        }

        foreach (Unit target in playerUnits)
        {
            if (target != null && target.currentHP > 0)
            {
                int damage = Mathf.RoundToInt(self.damage * 3f);
                PerformBasicAttack(self, target, damage);
            }
        }

        EffectManager.Instance.PlayUIEffect("Blanket3", new Vector2(0f, 0f));
        Debug.Log($"{self.unitName} 使用 Softskill_3 對所有玩家造成 {Mathf.RoundToInt(self.damage * 3f)} 傷害！");
    }

    private Unit GetRandomValidTarget(List<Unit> units)
    {
        List<Unit> validTargets = units.FindAll(unit => unit != null && unit.currentHP > 0);
        if (validTargets == null || validTargets.Count == 0)
        {
            return null;
        }

        int randomIndex = Random.Range(0, validTargets.Count);
        return validTargets[randomIndex];
    }

    private void PerformBasicAttack(Unit attacker, Unit target, int damage)
    {
        if (target != null && target.TakeDamage(damage))
        {
            Debug.Log($"{attacker.unitName} 擊敗 {target.unitName}！");
        }
        else
        {
            Debug.Log($"{attacker.unitName} 對 {target.unitName} 造成 {damage} 傷害！");
        }
    }

    public void ReduceCooldowns()
    {
        foreach (var skill in new List<string>(skillCooldowns.Keys))
        {
            if (skillCooldowns[skill] > 0)
            {
                skillCooldowns[skill]--;
                Debug.Log($"技能 {skill} 冷卻剩餘 {skillCooldowns[skill]} 回合");
            }
        }
    }
}