using System.Collections.Generic;
using UnityEngine;

public class Enemyskill : MonoBehaviour
{
    public List<Unit> enemyUnits;
    public List<Unit> playerUnits;
    public BuffDebuffManager buffDebuffManager;

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
        else
        {
            Debug.LogWarning($"未知的敵人名稱: {enemyName}，無技能分配");
        }

        return skills;
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
}