using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BattleState
{
    Start,
    TurnStart,
    PlayerAction,
    WaitForTarget,
    ActionExecution,
    EnemyAction,
    TurnEnd,
    BattleWon,
    BattleLost
}

// ==========================================
// 技能實體 (記錄運行時的 CD 狀態)
// ==========================================
[System.Serializable]
public class SkillInstance
{
    public SkillData data;      // 原始技能資料
    public int currentCD = 0;   // 當前剩餘 CD 回合數

    public SkillInstance(SkillData skillData)
    {
        data = skillData;
        currentCD = 0; // 剛進戰鬥時可馬上使用
    }

    public bool IsReady => currentCD <= 0;

    public void PutOnCooldown()
    {
        currentCD = data.cooldownTurns;
    }

    public void DecreaseCooldown()
    {
        if (currentCD > 0) currentCD--;
    }
}

// ==========================================
// 戰鬥單位資料結構
// ==========================================
[System.Serializable]
public class BattleUnit
{
    public string unitName;
    public GameObject gameObject;
    public StatsManager stats;
    public StatusEffectManager status;
    public Health health;
    public bool IsDead => health != null && health.isDead;

    [Header("角色擁有的技能 (從 Unity 面板拖曳)")]
    public List<SkillData> learnedSkills = new List<SkillData>();

    [HideInInspector]
    public List<SkillInstance> activeSkills = new List<SkillInstance>();

    // 初始化技能實體 (為每個技能建立獨立的 CD 追蹤器)
    public void InitializeSkills()
    {
        activeSkills.Clear();
        foreach (var data in learnedSkills)
        {
            activeSkills.Add(new SkillInstance(data));
        }
    }
}

public class BattleSystem : MonoBehaviour
{
    public BattleState state;

    [Header("隊伍名單")]
    public List<BattleUnit> playerTeam = new List<BattleUnit>();
    public List<BattleUnit> enemyTeam = new List<BattleUnit>();

    private BattleUnit currentActor;
    private BattleUnit currentTarget;

    // 【修改】現在是記錄 SkillInstance 而不是 SkillData
    private SkillInstance currentSelectedSkill;

    private bool playerHasDecided = false;

    private void Start()
    {
        state = BattleState.Start;
        StartCoroutine(SetupBattle());
    }

    // ==========================================
    // 階段 1：初始化戰鬥
    // ==========================================
    private IEnumerator SetupBattle()
    {
        Debug.Log("戰鬥開始，正在初始化隊伍...");

        // 【新增】為所有人初始化獨立的技能 CD 追蹤器
        foreach (var p in playerTeam) p.InitializeSkills();
        foreach (var e in enemyTeam) e.InitializeSkills();

        yield return new WaitForSeconds(1f);

        state = BattleState.TurnStart;
        StartCoroutine(HandleTurnStart());
    }

    // ==========================================
    // 階段 2：回合開始
    // ==========================================
    private IEnumerator HandleTurnStart()
    {
        Debug.Log("--- 新回合開始 ---");
        yield return new WaitForSeconds(0.5f);

        state = BattleState.PlayerAction;
        StartCoroutine(PlayerPhaseRoutine());
    }

    // ==========================================
    // 階段 3：我方全體行動迴圈
    // ==========================================
    private IEnumerator PlayerPhaseRoutine()
    {
        foreach (var player in playerTeam)
        {
            if (player.IsDead) continue;

            currentActor = player;
            currentTarget = null;
            currentSelectedSkill = null;
            playerHasDecided = false;

            Debug.Log($"現在輪到 【{player.unitName}】 行動，等待玩家點擊技能與目標...");
            state = BattleState.PlayerAction;

            yield return new WaitUntil(() => playerHasDecided == true);

            state = BattleState.ActionExecution;
            yield return StartCoroutine(ExecuteActionRoutine(currentActor, currentTarget));

            if (CheckIfTeamDead(enemyTeam))
            {
                state = BattleState.BattleWon;
                EndBattle();
                yield break;
            }
        }

        state = BattleState.EnemyAction;
        StartCoroutine(EnemyPhaseRoutine());
    }

    // ==========================================
    // 階段 4：敵方行動 (包含 AI 技能 CD 判斷)
    // ==========================================
    private IEnumerator EnemyPhaseRoutine()
    {
        Debug.Log("--- 敵方回合開始 ---");

        foreach (var enemy in enemyTeam)
        {
            if (enemy.IsDead) continue;

            currentActor = enemy;
            currentTarget = GetRandomAliveTarget(playerTeam);

            if (currentTarget != null)
            {
                // 【修改】過濾出「不在 CD 中」的可用技能
                List<SkillInstance> readySkills = new List<SkillInstance>();
                foreach (var skill in enemy.activeSkills)
                {
                    if (skill.IsReady) readySkills.Add(skill);
                }

                if (readySkills.Count > 0)
                {
                    // 從可用技能中隨機挑選一招
                    int randomSkillIndex = Random.Range(0, readySkills.Count);
                    currentSelectedSkill = readySkills[randomSkillIndex];
                }
                else
                {
                    Debug.LogWarning($"{enemy.unitName} 所有的技能都在 CD 中！(跳過回合或改用預設普攻)");
                    currentSelectedSkill = null;
                }

                if (currentSelectedSkill != null)
                {
                    yield return StartCoroutine(ExecuteActionRoutine(currentActor, currentTarget));
                }
            }

            if (CheckIfTeamDead(playerTeam))
            {
                state = BattleState.BattleLost;
                EndBattle();
                yield break;
            }

            yield return new WaitForSeconds(0.5f);
        }

        state = BattleState.TurnEnd;
        StartCoroutine(HandleTurnEnd());
    }

    // ==========================================
    // 階段 5：執行攻擊與附加 Buff
    // ==========================================
    private IEnumerator ExecuteActionRoutine(BattleUnit actor, BattleUnit target)
    {
        if (currentSelectedSkill == null)
        {
            Debug.LogError($"{actor.unitName} 嘗試行動，但沒有選擇任何技能！");
            yield break;
        }

        // 【新增】技能確定施放，進入冷卻 CD
        currentSelectedSkill.PutOnCooldown();

        // 將資料提取出來，方便下面計算
        SkillData skillData = currentSelectedSkill.data;

        Debug.Log($"{actor.unitName} 使用了技能：【{skillData.skillName}】！");
        yield return new WaitForSeconds(0.5f);

        // 1. 計算技能傷害
        float damage = CombatCalculator.CalculateSkillDamage(actor.stats, target.stats, skillData);
        Debug.Log($"{target.unitName} 受到了 {damage} 點傷害！");
        target.health.TakeDamage(damage);

        // 2. 處理狀態附加
        if (skillData.applyStatusEffect && !target.IsDead)
        {
            StatBuffEffect effect = new StatBuffEffect(
                skillData.skillName + "效果",
                skillData.durationTurns,
                skillData.targetStat,
                skillData.effectValue,
                skillData.modifierType
            );

            if (skillData.applyToSelf)
            {
                actor.status.AddEffect(effect);
                Debug.Log($"{actor.unitName} 給自己附加了狀態！");
            }
            else
            {
                target.status.AddEffect(effect);
                Debug.Log($"{target.unitName} 被附加了狀態！");
            }
        }

        yield return new WaitForSeconds(1f);
    }

    // ==========================================
    // 階段 6：回合結算 (減少 CD)
    // ==========================================
    private IEnumerator HandleTurnEnd()
    {
        Debug.Log("進行回合結算...");

        foreach (var player in playerTeam)
        {
            if (!player.IsDead)
            {
                player.status.ProcessTurnEnd();
                // 【新增】每回合減少 CD
                foreach (var skill in player.activeSkills) skill.DecreaseCooldown();
            }
        }

        foreach (var enemy in enemyTeam)
        {
            if (!enemy.IsDead)
            {
                enemy.status.ProcessTurnEnd();
                // 【新增】每回合減少 CD
                foreach (var skill in enemy.activeSkills) skill.DecreaseCooldown();
            }
        }

        yield return new WaitForSeconds(0.5f);

        state = BattleState.TurnStart;
        StartCoroutine(HandleTurnStart());
    }

    // ==========================================
    // UI 綁定與輔助方法
    // ==========================================

    /// <summary>
    /// 【修改】改為傳入 SkillInstance，並檢查是否在 CD
    /// </summary>
    public void OnSkillButton(SkillInstance selectedSkill)
    {
        if (state != BattleState.PlayerAction) return;

        // 【防呆】如果技能還在 CD，不給按
        if (!selectedSkill.IsReady)
        {
            Debug.Log($"技能【{selectedSkill.data.skillName}】還在冷卻中！剩餘 {selectedSkill.currentCD} 回合。");
            return;
        }

        currentSelectedSkill = selectedSkill;
        Debug.Log($"準備使用 【{selectedSkill.data.skillName}】，請點擊目標！");

        state = BattleState.WaitForTarget;
    }

    public void OnEnemySelected(BattleUnit clickedEnemy)
    {
        if (state != BattleState.WaitForTarget) return;
        if (clickedEnemy.IsDead)
        {
            Debug.Log("這個敵人已經死了，請選別人！");
            return;
        }

        currentTarget = clickedEnemy;
        playerHasDecided = true;
    }

    private bool CheckIfTeamDead(List<BattleUnit> team)
    {
        foreach (var unit in team)
        {
            if (!unit.IsDead) return false;
        }
        return true;
    }

    private BattleUnit GetRandomAliveTarget(List<BattleUnit> team)
    {
        List<BattleUnit> aliveUnits = new List<BattleUnit>();
        foreach (var unit in team)
        {
            if (!unit.IsDead) aliveUnits.Add(unit);
        }

        if (aliveUnits.Count > 0)
        {
            return aliveUnits[Random.Range(0, aliveUnits.Count)];
        }
        return null;
    }

    private void EndBattle()
    {
        if (state == BattleState.BattleWon)
        {
            Debug.Log("戰鬥勝利！");
        }
        else if (state == BattleState.BattleLost)
        {
            Debug.Log("戰鬥失敗！Game Over。");
        }
    }
}