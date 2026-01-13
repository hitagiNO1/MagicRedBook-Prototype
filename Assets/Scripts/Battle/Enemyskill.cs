using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random; // 添加別名，解決模擬兩可問題

public class Enemyskill : MonoBehaviour
{
    public List<Unit> enemyUnits;
    public TextMeshProUGUI dialogueText;
    public List<Unit> playerUnits;
    public BuffDebuffManager buffDebuffManager;
    int damageDealt;
    // 新增：技能冷卻字典，假設每個敵人技能有獨立冷卻
    private Dictionary<string, int> skillCooldowns = new Dictionary<string, int>();

    // 新增：執行敵人技能的主要方法
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

    // 新增：專門處理 SOFT 敵人的技能選擇邏輯
    private IEnumerator ExecuteSoftSkillsCoroutine(Unit self)
    {
        // 優先技能（改用協程）
        var prioritySkills = new List<Func<Unit, IEnumerator>>
    {
        Softskill_2Coroutine,
        Softskill_3Coroutine
    };

        // 隨機打亂順序（保持原有邏輯）
        for (int i = 0; i < prioritySkills.Count; i++)
        {
            int randomIndex = Random.Range(0, prioritySkills.Count);
            var temp = prioritySkills[i];
            prioritySkills[i] = prioritySkills[randomIndex];
            prioritySkills[randomIndex] = temp;
        }

        // 嘗試執行優先技能（只要成功一個就結束）
        foreach (var skill in prioritySkills)
        {
            // 這裡需要檢查冷卻，但因為現在是協程，我們把冷卻檢查包在協程裡
            // 所以直接呼叫，裡面會自己判斷是否執行
            yield return StartCoroutine(skill(self));

            // 如果執行成功就離開（你可以根據需求加判斷）
            yield break;  // 這裡簡單處理：執行一個優先技能就結束
        }

        // 備用技能
        Debug.Log($"{self.unitName} 的所有特殊技能都在冷卻中，使用備用技能 Softskill_1");
        yield return StartCoroutine(PerformSoftskill_1(self));
    }

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
            coroutines.Add(Softskill_2Coroutine);  // 改用協程版本
            coroutines.Add(Softskill_3Coroutine);  // 改用協程版本
        }
        else
        {
            Debug.LogWarning($"未知的敵人類型: {enemyName}");
        }

        return coroutines;
    }

    private IEnumerator Softskill_2Coroutine(Unit self)
    {
        string skillKey = $"{self.unitName}_Softskill_2";
        if (!skillCooldowns.ContainsKey(skillKey))
            skillCooldowns[skillKey] = 0;

        if (skillCooldowns[skillKey] > 0)
        {
            Debug.Log($"{self.unitName} 的 Softskill_2 冷卻中，剩餘 {skillCooldowns[skillKey]} 回合");
            yield break; // 不執行
        }

        yield return StartCoroutine(PerformSoftskill_2(self));
        skillCooldowns[skillKey] = 2; // 設置冷卻
    }

    private IEnumerator Softskill_3Coroutine(Unit self)
    {
        string skillKey = $"{self.unitName}_Softskill_3";
        if (!skillCooldowns.ContainsKey(skillKey))
            skillCooldowns[skillKey] = 0;

        if (skillCooldowns[skillKey] > 0)
        {
            Debug.Log($"{self.unitName} 的 Softskill_3 冷卻中，剩餘 {skillCooldowns[skillKey]} 回合");
            yield break;
        }

        yield return StartCoroutine(PerformSoftskill_3(self));
        skillCooldowns[skillKey] = 2;
    }

    private IEnumerator PerformFishskill_1(Unit self)
    {
        Unit target = GetRandomValidTarget(playerUnits);
        if (target == null || target.isDead || !enemyUnits.Contains(target)) yield break;

        int damage = self.damage;

        int actualDamageDealt;
        bool isKilled = target.TakeDamage(damage, out actualDamageDealt);

        // ★ 這裡自己組合想要的文字 ★
        if (dialogueText != null)
        {
            dialogueText.text = $"{self.unitName} 使出了 魚叉突刺\n握緊手中叉子刺向  {target.unitName}\n" +
                               $"{target.unitName} 受到了 {actualDamageDealt} 點傷害！";
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
            dialogueText.text = $"{self.unitName} 使出了 神魚擺尾！\n蓄力轉身，用尾巴甩向 {target.unitName}。\n" +
                               $"{target.unitName} 受到了 {actualDamageDealt} 點傷害！";

        }

        bool playLeft = target.unitName != "布萊克";
        EffectManager.Instance.PlayUIEffectOneSide("Fish2", playLeft);

        yield return new WaitForSeconds(1.5f);
    }

    private IEnumerator PerformSnakeskill_1(Unit self)
    {
        Unit target = GetRandomValidTarget(playerUnits);
        if (target == null || target.isDead) yield break;

        int damage = self.damage * 2;

        int actualDamageDealt;
        bool isKilled = target.TakeDamage(damage, out actualDamageDealt);

        if (dialogueText != null)
        {
            dialogueText.text = $"{self.unitName} 使用了 咬咬咬！\n大口咬向 {target.unitName}。\n" +
                               $"{target.unitName} 受到了 {actualDamageDealt} 點傷害！";
        }

        bool playLeft = target.unitName != "布萊克";
        EffectManager.Instance.PlayUIEffectOneSide("Snake1", playLeft);

        yield return new WaitForSeconds(1.5f);
    }

    private IEnumerator PerformSnakeskill_2(Unit self)
    {
        if (playerUnits == null || playerUnits.Count == 0)
        {
            Debug.LogError("playerUnits 未初始化或為空！");
            yield break;
        }

        if (buffDebuffManager == null)
        {
            Debug.LogError("buffDebuffManager 未初始化！");
            yield break;
        }

        Unit target = GetRandomValidTarget(playerUnits);
        if (target == null || target.isDead) yield break;

        // 施加中毒效果
        buffDebuffManager.ApplyPoison(target);

        if (dialogueText != null)
        {
            dialogueText.text = $"{self.unitName} 使出了 陳年老痰！\n" +
                               $"對 {target.unitName} 噴出帶毒黏液， {target.unitName} 中毒。\n" +
                               $"{target.unitName} 將在接下來幾回合持續受到傷害...";
        }

        // 特效
        bool playLeft = target.unitName != "布萊克";
        EffectManager.Instance.PlayUIEffectOneSide("Snake2", playLeft);

        // 最後延遲 1.5 秒，讓玩家看清楚文字與特效
        yield return new WaitForSeconds(1.5f);
    }

    private IEnumerator PerformClockskill_1(Unit self)
    {
        if (playerUnits == null || playerUnits.Count == 0)
        {
            Debug.LogError("playerUnits 未初始化或為空！");
            yield break;
        }

        Unit target = GetRandomValidTarget(playerUnits);
        if (target == null || target.isDead) yield break;

        // 計算傷害（原本是 self.damage * 2）
        int damage = self.damage * 2;

        // 執行攻擊並取得實際傷害與是否擊殺
        int actualDamageDealt;
        bool isKilled = target.TakeDamage(damage, out actualDamageDealt);

        // 自己組合想要的對話文字（時鐘主題可以帶點時間、命運感）
        if (dialogueText != null)
        {
            dialogueText.text = $"{self.unitName} 使出了 當頭棒喝！\n" +
                               $"拿槌子敲向   {target.unitName}！\n" +
                               $"{target.unitName} 受到了 {actualDamageDealt} 點傷害！";
        }

        // 特效
        bool playLeft = target.unitName != "布萊克";
        EffectManager.Instance.PlayUIEffectOneSide("Clock1", playLeft);

        // 最後延遲 1.5 秒，讓玩家看清楚
        yield return new WaitForSeconds(1.5f);
    }

    private IEnumerator PerformClockskill_2(Unit self)
    {
        if (playerUnits == null || playerUnits.Count == 0)
        {
            Debug.LogError("playerUnits 未初始化或為空！");
            yield break;
        }

        if (buffDebuffManager == null)
        {
            Debug.LogError("buffDebuffManager 未初始化！");
            yield break;
        }

        Unit target = GetRandomValidTarget(playerUnits);
        if (target == null || target.isDead) yield break;

        // 施加 debuff 效果（先施加再攻擊，符合邏輯）
        buffDebuffManager.ApplyDamageBuff(target, 0.7f, 2);     // 傷害降低 30%（0.7f），持續 2 回合
        buffDebuffManager.ApplyHealDebuff(target, 2);           // 治療效果降低，持續 2 回合

        // 計算並執行攻擊
        int damage = Mathf.RoundToInt(self.damage * 1.5f);

        int actualDamageDealt;
        bool isKilled = target.TakeDamage(damage, out actualDamageDealt);

        // 自己組合文字（時鐘主題可以帶點「時間扭曲」「削弱」感）
        if (dialogueText != null)
        {
            dialogueText.text = $"{self.unitName} 使出了 鈴鈴鈴！\n" +
                               $"敲擊自己腦袋，發出聲波攻擊{target.unitName} ！\n" +
                               $"{target.unitName} 受到了 {actualDamageDealt} 點傷害，"/* +
                               "接下來 2 回合傷害與治療效果大幅降低！"*/;
        }

        // 特效（全場中央）
        EffectManager.Instance.PlayUIEffect("Clock2", new Vector2(0, 0));

        // 最後延遲 1.5 秒
        yield return new WaitForSeconds(1.5f);
    }

    private IEnumerator PerformSoftskill_1(Unit self)
    {
        if (playerUnits == null || playerUnits.Count == 0)
        {
            Debug.LogError("playerUnits 未初始化或為空！");
            yield break;
        }

        Unit target = GetRandomValidTarget(playerUnits);
        if (target == null || target.isDead) yield break;

        // 計算傷害（原本是 self.damage * 2）
        int damage = self.damage * 2;

        // 執行攻擊並取得實際傷害與是否擊殺
        int actualDamageDealt;
        bool isKilled = target.TakeDamage(damage, out actualDamageDealt);

        if (dialogueText != null)
        {
            dialogueText.text = $"{self.unitName} 使出了 軟軟甩擊！\n" +
                               $"對 {target.unitName} 甩了一巴掌！\n" +
                               $"{target.unitName} 受到了 {actualDamageDealt} 點傷害！";
        }

        yield return new WaitForSeconds(1.5f);
    }

    private IEnumerator PerformSoftskill_2(Unit self)
    {
        if (playerUnits == null || playerUnits.Count == 0)
        {
            Debug.LogError("playerUnits 未初始化或為空！");
            yield break;
        }

        if (buffDebuffManager == null)
        {
            Debug.LogError("buffDebuffManager 未初始化！");
            yield break;
        }

        List<Unit> validTargets = playerUnits.FindAll(unit => unit != null && unit.currentHP > 0);
        if (validTargets.Count == 0)
        {
            Debug.LogWarning("無有效的玩家目標！");
            yield break;
        }

        Unit target = validTargets[Random.Range(0, validTargets.Count)];

        // 先施加禁用技能效果
        buffDebuffManager.ApplySkillDisable(target, 1);

        // 計算並執行攻擊
        int damage = Mathf.RoundToInt(self.damage * 0.5f);

        int actualDamageDealt;
        bool isKilled = target.TakeDamage(damage, out actualDamageDealt);

        // 自己組合對話文字（毛毯主題 + 禁用說明）
        if (dialogueText != null)
        {
            dialogueText.text = $"{self.unitName} 使出了 軟軟纏繞！\n" +
                               $"纏住 {target.unitName}， {target.unitName} 動彈不得。！\n" +
                               $"{target.unitName} 受到了 {actualDamageDealt} 點傷害，" +
                               "並且下一回合無法使用技能！";
        }

        // 特效
        bool playLeft = target.unitName != "布萊克";
        EffectManager.Instance.PlayUIEffectOneSide("Blanket2", playLeft);

        // 最後延遲 1.5 秒，讓玩家看清楚文字與效果
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
            dialogueText.text = $"{self.unitName} 使用了 天黑請閉眼！\n" +
                               $"我方受到了總共 {totalDamageDealt} 點傷害！";
        }

        EffectManager.Instance.PlayUIEffect("Blanket3", new Vector2(0f, 0f));

        yield return new WaitForSeconds(1.5f);  
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
        if (attacker == null || target == null || target.isDead)
        {
            Debug.LogWarning("攻擊目標無效，跳過攻擊");
            return;
        }

        int actualDamageDealt;
        bool isKilled = target.TakeDamage(damage, out actualDamageDealt);

        // 只做必要的事：扣血、更新 HUD（如果有玩家 HUD 系統就加在這裡）
        // 這裡不再寫 Debug.Log 或 dialogueText
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