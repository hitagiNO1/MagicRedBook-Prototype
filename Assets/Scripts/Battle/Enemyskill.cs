using System.Collections.Generic;
using UnityEngine;

public class Enemyskill : MonoBehaviour
{
    public List<Unit> enemyUnits;
    public List<Unit> playerUnits;
    public BuffDebuffManager buffDebuffManager;
    // 新增：技能冷卻字典，假設每個敵人技能有獨立冷卻
    private Dictionary<string, int> skillCooldowns = new Dictionary<string, int>();

    public List<System.Action> GetEnemySkills(string enemyName)
    {
        List<System.Action> skills = new List<System.Action>();
        Unit self = GetComponent<Unit>();
        if (self == null)
        {
            Debug.LogError("Enemyskill 缺少 Unit 組件！");
            return skills;
        }

        if (enemyName == "FISH")
        {
            skills.Add(() => PerformFishskill_1(self));
            skills.Add(() => PerformFishskill_2(self));
        }
        else if (enemyName == "SNAKE")
        {
            skills.Add(() => PerformSnakeskill_1(self));
            skills.Add(() => PerformSnakeskill_2(self));
        }
        else if (enemyName == "CLOCK") // 新增 CLOCK 敵人
        {
            skills.Add(() => PerformClockskill_1(self));
            skills.Add(() => PerformClockskill_2(self));
        }
        else if (enemyName == "SOFT")
        {
            skills.Add(() => PerformSoftskill_1(self));
            skills.Add(() => CheckAndPerformSoftskill_2(self)); // 全體傷害，2 回合 CD
            skills.Add(() => CheckAndPerformSoftskill_3(self)); // 技能禁用，2 回合 CD
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
        Unit target = playerUnits[Random.Range(0, playerUnits.Count)];
        PerformBasicAttack(self, target, self.damage);
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
        Unit target = playerUnits[Random.Range(0, playerUnits.Count)];
        buffDebuffManager.ApplyDefenseDebuff(target, 0.7f, 2);
    }

    private void PerformSnakeskill_1(Unit self)
    {
        if (playerUnits == null || playerUnits.Count == 0)
        {
            Debug.LogError("playerUnits 未初始化或為空！");
            return;
        }
        Unit target = playerUnits[Random.Range(0, playerUnits.Count)];
        PerformBasicAttack(self, target, self.damage * 2);
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
        Unit target = playerUnits[Random.Range(0, playerUnits.Count)];
        buffDebuffManager.ApplyPoison(target);
    }

    private void PerformClockskill_1(Unit self)
    {
        if (playerUnits == null || playerUnits.Count == 0)
        {
            Debug.LogError("playerUnits 未初始化或為空！");
            return;
        }
        Unit target = playerUnits[Random.Range(0, playerUnits.Count)];
        PerformBasicAttack(self, target, self.damage * 2);
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

        Unit target = playerUnits[Random.Range(0, playerUnits.Count)];

        buffDebuffManager.ApplyDamageBuff(target, 0.7f, 2);
        buffDebuffManager.ApplyHealDebuff(target, 2);
        int damage = Mathf.RoundToInt(self.damage * 1.5f);
        PerformBasicAttack(self, target, damage);

        Debug.Log($"{self.unitName} 對 {target.unitName} 使用 Clockskill_2：降低攻擊 30%、回血 30%，造成 {damage} 傷害");
    }

    private void PerformSoftskill_1(Unit self)
    {
        if (playerUnits == null || playerUnits.Count == 0)
        {
            Debug.LogError("playerUnits 未初始化或為空！");
            return;
        }
        Unit target = playerUnits[Random.Range(0, playerUnits.Count)];
        PerformBasicAttack(self, target, self.damage * 2);
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

        // 隨機選擇一個存活的目標
        List<Unit> validTargets = playerUnits.FindAll(unit => unit != null && unit.currentHP > 0);
        if (validTargets.Count == 0)
        {
            Debug.LogWarning("無有效的玩家目標！");
            return;
        }
        Unit target = validTargets[Random.Range(0, validTargets.Count)];

        // 應用技能禁用效果，持續 1 回合
        buffDebuffManager.ApplySkillDisable(target, 1);

        // 造成 0.5 倍攻擊力的傷害
        int damage = Mathf.RoundToInt(self.damage * 0.5f);
        PerformBasicAttack(self, target, damage);

        Debug.Log($"{self.unitName} 對 {target.unitName} 使用 Softskill_3：禁用技能 1 回合，造成 {damage} 傷害");
    }

    private void PerformSoftskill_3(Unit self)
    {
        if (playerUnits == null || playerUnits.Count == 0)
        {
            Debug.LogError("playerUnits 未初始化或為空！");
            return;
        }

        // 對所有玩家單位造成三倍傷害
        foreach (Unit target in playerUnits)
        {
            if (target != null && target.currentHP > 0)
            {
                int damage = Mathf.RoundToInt(self.damage * 3f);
                PerformBasicAttack(self, target, damage);
            }
        }

        Debug.Log($"{self.unitName} 使用 Softskill_2 對所有玩家造成 {Mathf.RoundToInt(self.damage * 3f)} 傷害！");
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