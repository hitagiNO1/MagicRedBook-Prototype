using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

using Random = UnityEngine.Random;

public class Enemyskill : MonoBehaviour
{
    public List<Unit> enemyUnits;
    public TextMeshProUGUI dialogueText;
    public List<Unit> playerUnits;
    public BuffDebuffManager buffDebuffManager;

    // 技能冷卻字典 (所有敵人共用)
    private Dictionary<string, int> skillCooldowns = new Dictionary<string, int>();

    // ==================== 各個技能冷卻時間 ====================
    private int skill2CooldownTurns = 3;           // 舊有設定 (建議保留)
    private int skill3CooldownTurns = 2;
    private int skill4CooldownTurns = 3;
    private int skill5CooldownTurns = 3;
    private int catskill2CooldownTurns = 3;
    private int catskill3CooldownTurns = 2;
    private int catskill4CooldownTurns = 3;
    private int catskill5CooldownTurns = 4;

    // DCAT 專用冷卻時間
    private int dcatskill2CooldownTurns = 2;
    private int dcatskill3CooldownTurns = 2;
    private int dcatskill4CooldownTurns = 3;

    // ==================== 主要方法 ====================
    private bool IsSkillReady(string skillKey)
    {
        if (!skillCooldowns.ContainsKey(skillKey))
            skillCooldowns[skillKey] = 0;

        return skillCooldowns[skillKey] <= 0;
    }

    public IEnumerator ExecuteEnemySkillCoroutine(Unit enemyUnit)
    {
        if (enemyUnit == null)
        {
            Debug.LogError("敵人單位無效！");
            yield break;
        }

        string enemyName = enemyUnit.originalUnitName;

        if (enemyName == "SOFT")
        {
            yield return StartCoroutine(ExecuteSoftSkillsCoroutine(enemyUnit));
        }
        else if (enemyName == "DCAT")
        {
            yield return StartCoroutine(ExecuteDcatSkillsCoroutine(enemyUnit));
        }
        else
        {
            var skills = GetEnemySkills(enemyName);
            if (skills.Count > 0)
            {
                int randomIndex = Random.Range(0, skills.Count);
                yield return StartCoroutine(skills[randomIndex](enemyUnit));
            }
        }
    }

    // ==================== SOFT 專用技能流程 ====================
    private IEnumerator ExecuteSoftSkillsCoroutine(Unit self)
    {
        // 可用技能列表
        var availableSkills = new List<Func<Unit, IEnumerator>>();

        string skill2Key = $"{self.unitName}_Softskill_2";
        string skill3Key = $"{self.unitName}_Softskill_3";

        if (IsSkillReady(skill2Key))
            availableSkills.Add(Softskill_2Coroutine);

        if (IsSkillReady(skill3Key))
            availableSkills.Add(Softskill_3Coroutine);

        // 如果有可用技能 則 隨機執行一個
        if (availableSkills.Count > 0)
        {
            var chosenSkill = availableSkills[Random.Range(0, availableSkills.Count)];
            yield return StartCoroutine(chosenSkill(self));
            yield break;
        }

        // 都在冷卻 則 使用普通技能
        Debug.Log($"{self.unitName} 所有技能冷卻中，使用 Softskill_1");
        yield return StartCoroutine(PerformSoftskill_1(self));
    }

    // ==================== DCAT 專用技能流程 ====================
    private IEnumerator ExecuteDcatSkillsCoroutine(Unit self)
    {
        var availableSkills = new List<Func<Unit, IEnumerator>>();

        string skill2Key = $"{self.unitName}_Dcatskill_2";
        string skill3Key = $"{self.unitName}_Dcatskill_3";
        string skill4Key = $"{self.unitName}_Dcatskill_4";

        if (IsSkillReady(skill2Key))
            availableSkills.Add(Dcatskill_2Coroutine);

        if (IsSkillReady(skill3Key))
            availableSkills.Add(Dcatskill_3Coroutine);

        if (IsSkillReady(skill4Key))
            availableSkills.Add(Dcatskill_4Coroutine);

        // 若技能可用 則 隨機執行
        if (availableSkills.Count > 0)
        {
            var chosenSkill = availableSkills[Random.Range(0, availableSkills.Count)];
            yield return StartCoroutine(chosenSkill(self));
            yield break;
        }

        // 都在冷卻 則 使用普通技能
        Debug.Log($"{self.unitName} 所有技能冷卻中，使用 Dcatskill_1");
        yield return StartCoroutine(Dcatskill_1Coroutine(self));
    }

    // ==================== 獲取技能列表 ====================
    public List<Func<Unit, IEnumerator>> GetEnemySkills(string enemyName)
    {
        var coroutines = new List<Func<Unit, IEnumerator>>();

        if (enemyName == "FISH")
        {
            coroutines.Add(PerformFishskill_1);
            coroutines.Add(PerformFishskill_2);
        }
        else if (enemyName == "SNAKE")
        {
            coroutines.Add(PerformSnakeskill_1);
            coroutines.Add(PerformSnakeskill_2);
        }
        else if (enemyName == "CLOCK")
        {
            coroutines.Add(PerformClockskill_1);
            coroutines.Add(PerformClockskill_2);
        }
        else if (enemyName == "SOFT")
        {
            coroutines.Add(PerformSoftskill_1);
            coroutines.Add(Softskill_2Coroutine);
            coroutines.Add(Softskill_3Coroutine);
        }
        else if (enemyName == "Death")
        {
            coroutines.Add(Dcatskill_1Coroutine);
            coroutines.Add(Dcatskill_2Coroutine);
            coroutines.Add(Dcatskill_3Coroutine);
            coroutines.Add(Dcatskill_4Coroutine);
        }
        else
        {
            Debug.LogWarning($"未知的敵人名稱: {enemyName}");
        }

        return coroutines;
    }

    // ==================== SOFT 技能冷卻管理 ====================
    private IEnumerator Softskill_2Coroutine(Unit self)
    {
        string skillKey = $"{self.unitName}_Softskill_2";
        skillCooldowns[skillKey] = 2; // 先進入冷卻，避免異常中斷時連發
        yield return StartCoroutine(PerformSoftskill_2(self));
    }

    private IEnumerator Softskill_3Coroutine(Unit self)
    {
        string skillKey = $"{self.unitName}_Softskill_3";
        skillCooldowns[skillKey] = 2;
        yield return StartCoroutine(PerformSoftskill_3(self));
    }

    // ==================== FISH 技能 ====================
    private IEnumerator PerformFishskill_1(Unit self)
    {
        Unit target = GetRandomValidTarget(playerUnits);
        if (target == null || target.isDead) yield break;

        int damage = self.damage;
        int actualDamageDealt;
        bool isKilled = target.TakeDamage(damage, out actualDamageDealt);

        if (dialogueText != null)
        {
            dialogueText.text = $"<b>{self.unitName}</b> 使用了 <b>魚叉突刺</b>！\n握緊手中叉子刺向 <b>{target.unitName}</b>\n" +
                                $"<b>{target.unitName}</b> 受到了 <b>{actualDamageDealt}</b> 點傷害！";
        }

        bool playLeft = target.unitName != "布萊克";
        EffectManager.Instance.PlayUIEffectOneSide("Fish1", playLeft);
        yield return new WaitForSeconds(1.5f);
    }

    private IEnumerator PerformFishskill_2(Unit self)
    {
        Unit target = GetRandomValidTarget(playerUnits);
        if (target == null || target.isDead) yield break;

        int damage = Mathf.RoundToInt(self.damage * 1.2f);
        int actualDamageDealt;
        bool isKilled = target.TakeDamage(damage, out actualDamageDealt);

        if (dialogueText != null)
        {
            dialogueText.text = $"<b>{self.unitName}</b> 使用了 <b>神魚擺尾</b>！\n用力彈起，用尾巴甩向 <b>{target.unitName}</b>。\n" +
                                $"<b>{target.unitName}</b> 受到了 <b>{actualDamageDealt}</b> 點傷害！";
        }

        bool playLeft = target.unitName != "布萊克";
        EffectManager.Instance.PlayUIEffectOneSide("Fish2", playLeft);
        yield return new WaitForSeconds(1.5f);
    }

    // ==================== SNAKE 技能 ====================
    private IEnumerator PerformSnakeskill_1(Unit self)
    {
        Unit target = GetRandomValidTarget(playerUnits);
        if (target == null || target.isDead) yield break;

        int damage = self.damage * 2;
        int actualDamageDealt;
        bool isKilled = target.TakeDamage(damage, out actualDamageDealt);

        if (dialogueText != null)
        {
            dialogueText.text = $"<b>{self.unitName}</b> 使用了 <b>咬咬咬</b>！\n大口咬向 <b>{target.unitName}</b>。\n" +
                                $"<b>{target.unitName}</b> 受到了 <b>{actualDamageDealt}</b> 點傷害！";
        }

        bool playLeft = target.unitName != "布萊克";
        EffectManager.Instance.PlayUIEffectOneSide("Snake1", playLeft);
        yield return new WaitForSeconds(1.5f);
    }

    private IEnumerator PerformSnakeskill_2(Unit self)
    {
        if (playerUnits == null || playerUnits.Count == 0) yield break;
        if (buffDebuffManager == null) yield break;

        Unit target = GetRandomValidTarget(playerUnits);
        if (target == null || target.isDead) yield break;

        buffDebuffManager.ApplyPoison(target);

        if (dialogueText != null)
        {
            dialogueText.text = $"<b>{self.unitName}</b> 使用了 <b>陳年老痰</b>！\n" +
                                $"對 <b>{target.unitName}</b> 噴出帶毒黏液， <b>{target.unitName}</b> 中毒。\n" +
                                $"<b>{target.unitName}</b> 將在接下來幾回合受到傷害...";
        }

        bool playLeft = target.unitName != "布萊克";
        EffectManager.Instance.PlayUIEffectOneSide("Snake2", playLeft);
        yield return new WaitForSeconds(1.5f);
    }

    // ==================== CLOCK 技能 ====================
    private IEnumerator PerformClockskill_1(Unit self)
    {
        if (playerUnits == null || playerUnits.Count == 0) yield break;

        Unit target = GetRandomValidTarget(playerUnits);
        if (target == null || target.isDead) yield break;

        int damage = self.damage * 2;
        int actualDamageDealt;
        bool isKilled = target.TakeDamage(damage, out actualDamageDealt);

        if (dialogueText != null)
        {
            dialogueText.text = $"<b>{self.unitName}</b> 使用了 <b>當頭棒喝</b>！\n" +
                                $"拿槌子敲向 <b>{target.unitName}</b>！\n" +
                                $"<b>{target.unitName}</b> 受到了 <b>{actualDamageDealt}</b> 點傷害！";
        }

        bool playLeft = target.unitName != "布萊克";
        EffectManager.Instance.PlayUIEffectOneSide("Clock1", playLeft);
        yield return new WaitForSeconds(1.5f);
    }

    private IEnumerator PerformClockskill_2(Unit self)
    {
        if (playerUnits == null || playerUnits.Count == 0) yield break;
        if (buffDebuffManager == null) yield break;

        Unit target = GetRandomValidTarget(playerUnits);
        if (target == null || target.isDead) yield break;

        buffDebuffManager.ApplyDamageBuff(target, 0.7f, 2);
        buffDebuffManager.ApplyHealDebuff(target, 2);

        int damage = Mathf.RoundToInt(self.damage * 1.5f);
        int actualDamageDealt;
        bool isKilled = target.TakeDamage(damage, out actualDamageDealt);

        if (dialogueText != null)
        {
            dialogueText.text = $"<b>{self.unitName}</b> 使用了 <b>滴答滴！</b>\n" +
                                $"敲擊自己腦袋，發出聲波攻擊 <b>{target.unitName}</b>！\n" +
                                $"<b>{target.unitName}</b> 受到了 <b>{actualDamageDealt}</b> 點傷害，" +
                                "攻擊與治療降低！";
        }

        EffectManager.Instance.PlayUIEffect("Clock2", new Vector2(0, 0));
        yield return new WaitForSeconds(1.5f);
    }

    // ==================== SOFT 技能 ====================
    private IEnumerator PerformSoftskill_1(Unit self)
    {
        if (playerUnits == null || playerUnits.Count == 0) yield break;

        Unit target = GetRandomValidTarget(playerUnits);
        if (target == null || target.isDead) yield break;

        int damage = self.damage * 2;
        int actualDamageDealt;
        bool isKilled = target.TakeDamage(damage, out actualDamageDealt);

        if (dialogueText != null)
        {
            dialogueText.text = $"<b>{self.unitName}</b> 使用了 軟軟甩擊！\n" +
                                $"對 <b>{target.unitName}</b> 甩了一巴掌！\n" +
                                $"<b>{target.unitName}</b> 受到了 <b>{actualDamageDealt}</b> 點傷害！";
        }
        bool playLeft = target.unitName != "布萊克";
        EffectManager.Instance.PlayUIEffectOneSide("Blanket1", playLeft);
        yield return new WaitForSeconds(1.5f);
    }

    private IEnumerator PerformSoftskill_2(Unit self)
    {
        if (playerUnits == null || playerUnits.Count == 0) yield break;
        if (buffDebuffManager == null) yield break;

        List<Unit> validTargets = playerUnits.FindAll(unit => unit != null && unit.currentHP > 0);
        if (validTargets.Count == 0)
        {
            Debug.LogWarning("無有效的玩家目標！");
            yield break;
        }

        Unit target = validTargets[Random.Range(0, validTargets.Count)];

        buffDebuffManager.ApplySkillDisable(target, 2);

        int damage = Mathf.RoundToInt(self.damage * 0.5f);
        int actualDamageDealt;
        bool isKilled = target.TakeDamage(damage, out actualDamageDealt);

        if (dialogueText != null)
        {
            dialogueText.text = $"<b>{self.unitName}</b> 使用了 <b>軟軟纏繞</b>！\n" +
                                $"抱住 <b>{target.unitName}</b>， <b>{target.unitName}</b> 動彈不得。\n" +
                                $"<b>{target.unitName}</b> 受到了 <b>{actualDamageDealt}</b> 點傷害，" +
                                "並且無法使用技能！";
        }

        bool playLeft = target.unitName != "布萊克";
        EffectManager.Instance.PlayUIEffectOneSide("Blanket2", playLeft);
        yield return new WaitForSeconds(1.5f);
    }

    private IEnumerator PerformSoftskill_3(Unit self)
    {
        if (playerUnits == null || playerUnits.Count == 0) yield break;

        int totalDamageDealt = 0;
        foreach (Unit target in playerUnits)
        {
            if (target == null || target.currentHP <= 0) continue;

            int damage = Mathf.RoundToInt(self.damage * 3f);
            int actualDamageDealt;
            bool isKilled = target.TakeDamage(damage, out actualDamageDealt);
            totalDamageDealt += actualDamageDealt;
        }

        if (dialogueText != null)
        {
            dialogueText.text = $"<b>{self.unitName}</b> 使用了 <b>軟軟擁抱</b>！\n" +
                                $"共造成了總計 <b>{totalDamageDealt}</b> 點傷害！";
        }

        EffectManager.Instance.PlayUIEffect("Blanket3", new Vector2(0f, 0f));
        yield return new WaitForSeconds(1.5f);
    }

    // ==================== DCAT 技能 ====================
    private IEnumerator Dcatskill_1Coroutine(Unit self)
    {
        Unit target = GetRandomValidTarget(playerUnits);
        if (target == null || target.isDead) yield break;

        int damage = Mathf.RoundToInt(self.damage * 2.5f);
        int actualDamageDealt;
        bool isKilled = target.TakeDamage(damage, out actualDamageDealt);

        if (dialogueText != null)
        {
            dialogueText.text = $"<b>{self.unitName}</b> 使用了 <b>斬擊</b>！\n" +
                                $"對 <b>{target.unitName}</b> 進行斬擊！\n" +
                                $"<b>{target.unitName}</b> 受到了 <b>{actualDamageDealt}</b> 點傷害！";
        }

        Debug.Log("DC1");
        yield return new WaitForSeconds(1.5f);
    }

    private IEnumerator Dcatskill_2Coroutine(Unit self)
    {
        string skillKey = $"{self.unitName}_Dcatskill_2";
        skillCooldowns[skillKey] = dcatskill2CooldownTurns; // 執行時直接設定冷卻

        int totalAffected = 0;

        foreach (Unit target in playerUnits)
        {
            if (target == null || target.isDead || target.currentHP <= 0) continue;

            buffDebuffManager.ApplyPoison(target, 3);  // 對每個存活玩家施加 3 回合毒素
            totalAffected++;
        }

        if (dialogueText != null)
        {
            if (totalAffected > 0)
            {
                dialogueText.text = $"<b>{self.unitName}</b> 使用了 <b>闇之氣息</b>！\n" +
                                    $"全場瀰漫著毒霧！\n";
            }
        }
        EffectManager.Instance.PlayUIEffect("Dcat2", new Vector2(0f, 0f));

        Debug.Log("DC2");
        yield return new WaitForSeconds(3f);
    }

    private IEnumerator Dcatskill_3Coroutine(Unit self)
    {
        string skillKey = $"{self.unitName}_Dcatskill_3";
        skillCooldowns[skillKey] = dcatskill3CooldownTurns; // 執行時直接設定冷卻

        Unit target = GetRandomValidTarget(playerUnits);
        if (target == null || target.isDead) yield break;

        int lostHP = target.maxHP - target.currentHP;
        int damage = Mathf.RoundToInt(lostHP * 0.75f);

        int actualDamageDealt;
        bool isKilled = target.TakeDamage(damage, out actualDamageDealt);

        buffDebuffManager.ApplyDefenseBuff(self, 1.5f, 2);   // 防禦 +150%
        buffDebuffManager.ApplyDamageBuff(self, 2.0f, 2);    // 攻擊 +200%

        if (dialogueText != null)
        {
            dialogueText.text = $"<b>{self.unitName}</b> 使用了 <b>神之庇護！</b>\n" +
                                $"強大的力量圍繞在？？？身旁！";
        }

        bool playLeft = target.unitName != "布萊克";
        EffectManager.Instance.Dcat3();

        Debug.Log("DC3");
        yield return new WaitForSeconds(3f);
    }

    private IEnumerator Dcatskill_4Coroutine(Unit self)
    {
        string skillKey = $"{self.unitName}_Dcatskill_4";
        skillCooldowns[skillKey] = dcatskill4CooldownTurns; // 執行時直接設定冷卻

        int totalDamageDealt = 0;
        int damage = Mathf.RoundToInt(self.damage * 3.5f);

        foreach (Unit target in playerUnits)
        {
            if (target == null || target.currentHP <= 0) continue;

            int actualDamageDealt;
            bool isKilled = target.TakeDamage(damage, out actualDamageDealt);
            totalDamageDealt += actualDamageDealt;
        }

        if (dialogueText != null)
        {
            dialogueText.text = $"<b>{self.unitName}</b> 使用了 <b>死式，茈</b>！\n" +
                                $"聚集強大力量，掃向所有人！\n" +
                                $"總共造成了總計 <b>{totalDamageDealt}</b> 點傷害！";
        }

        EffectManager.Instance.Dcat4();

        Debug.Log("DC4");
        yield return new WaitForSeconds(3f);
    }

    // ==================== 通用輔助方法 ====================
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
        if (attacker == null || target == null || target.isDead)
        {
            Debug.LogWarning("攻擊目標無效，忽略攻擊");
            return;
        }
        int actualDamageDealt;
        bool isKilled = target.TakeDamage(damage, out actualDamageDealt);
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