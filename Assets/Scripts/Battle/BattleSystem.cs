using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public enum BattleState { START, PLAYERTURN, ENEMYTURN, WON, LOST, RUN, END }
public class BattleSystem : MonoBehaviour
{
    public List<GameObject> playerPrefabs;
    public List<GameObject> enemyPrefabs; // 實例化的敵人 GameObject
    public List<Unit> playerUnits;
    public List<Unit> enemyUnits;
    public TextMeshProUGUI dialogueText;
    public List<BattleHUD> playerHUDs;
    public List<BattleHUD> enemyHUDs;
    public Animator animator;
    public BattleState state;
    public SkillManager skillManager;
    public BuffDebuffManager buffDebuffManager;
    public GameController gameScript;
    public mutiplebattle battleTrigger;
    [Header("特效容器")]
    public GameObject battleFxContainer;
    // 新增旗標：防止重複結束戰鬥或動畫
    private bool isBattleEnding = false;
    private bool hideAnimationTriggered = false;
    private bool isHidden = false;
    // 用來追蹤目前是哪位玩家（給 SkillManager.OnPlayerHeadClick 使用）
    public int currentPlayerIndex = 0;
    void Start()
    {
        state = BattleState.END;
        if (animator == null)
            Debug.LogError("Animator 未附加到 BattleSystem！");
        skillManager = GetComponent<SkillManager>();
        if (skillManager == null)
            Debug.LogError("SkillManager 未附加！");
        buffDebuffManager = GetComponent<BuffDebuffManager>();
        if (buffDebuffManager == null)
            Debug.LogError("BuffDebuffManager 未附加！");
        playerUnits = new List<Unit>();
        enemyUnits = new List<Unit>();
    }
    private void Update()
    {
        // 戰鬥開始觸發（由外部設定 state = START 時觸發一次）
        if (state == BattleState.START && !isHidden)
        {
            StartCoroutine(SetupBattle());
            GameStateManager.Instance.SetState(GameState.Battle);
            if (animator != null)
                animator.SetTrigger("Show");
            isHidden = true;
        }
    }
    public IEnumerator SetupBattle()
    {
        // ---------- 玩家初始化 ----------
        playerUnits.Clear();
        for (int i = 0; i < playerPrefabs.Count; i++)
        {
            Unit playerUnit = playerPrefabs[i].GetComponent<Unit>();
            if (playerUnit == null)
            {
                Debug.LogError($"playerPrefabs[{i}] 缺少 Unit 組件！");
                yield break;
            }
            playerUnits.Add(playerUnit);

            if (i < playerHUDs.Count && playerHUDs[i] != null)
            {
                playerHUDs[i].SetHUD(playerUnit);
                playerUnit.battleHUD = playerHUDs[i];
            }
        }

        // ---------- 敵人初始化 ----------
        enemyUnits.Clear();
        for (int i = 0; i < enemyPrefabs.Count; i++)
        {
            GameObject enemyGO = enemyPrefabs[i];
            if (enemyGO == null)
            {
                Debug.LogError($"enemyPrefabs[{i}] 為 null！");
                continue;
            }

            Unit enemyUnit = enemyGO.GetComponent<Unit>();
            if (enemyUnit == null)
            {
                Debug.LogError($"enemyPrefabs[{i}] 缺少 Unit 組件！");
                continue;
            }
            enemyUnits.Add(enemyUnit);

            if (i < enemyHUDs.Count && enemyHUDs[i] != null)
            {
                enemyHUDs[i].SetHUD(enemyUnit);
                enemyUnit.battleHUD = enemyHUDs[i];
            }

            // 設定每個敵人的 Enemyskill
            Enemyskill enemySkillComp = enemyGO.GetComponent<Enemyskill>();
            if (enemySkillComp != null)
            {
                enemySkillComp.playerUnits = playerUnits;
                enemySkillComp.enemyUnits = enemyUnits;
                enemySkillComp.buffDebuffManager = buffDebuffManager;
            }
        }

        // ---------- 初始對話 ----------
        if (dialogueText != null)
            dialogueText.text = "野生的敵人們出現了..";

        // 這裡的等待是為了讓玩家看清楚文字，同時確保 Animator 的 Trigger 已經觸發
        yield return new WaitForSeconds(1f);

        // ---------- SkillManager 初始化 ----------
        skillManager.playerUnits = playerUnits;
        skillManager.enemyUnits = enemyUnits;
        skillManager.enemyHUDs = enemyHUDs;
        skillManager.buffDebuffManager = buffDebuffManager;
        skillManager.dialogueText = dialogueText;

        // [修改處]：移除原本的 DelayedInitSkillIcons，改為等待動畫播放完畢
        // 確保 Animator 存在
        if (animator != null)
        {
            // 等待一幀，確保 Animator 狀態機已經切換到 Show
            yield return null;

            // 檢查動畫是否播放完畢
            // normalizedTime >= 1.0f 代表播放進度達到 100%
            // 注意：請確保 Unity 中該動畫的 "Loop Time" 已關閉，否則此條件可能失效
            while (animator.IsInTransition(0) || animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
            {
                yield return null; // 每幀檢查一次
            }
        }

        // ---------- 進入玩家回合 ----------
        // 當程式執行到這裡時，動畫已結束，PlayerTurn 內的 InitSkillIcons 會立即顯示 AP
        state = BattleState.PLAYERTURN;
        PlayerTurn();
    }
    public void PlayerTurn()
    {
        state = BattleState.PLAYERTURN;
        currentPlayerIndex = 0; // 重置為第0位玩家（給 catSkill_4 點頭像用）
        if (dialogueText != null)
            dialogueText.text = "玩家回合：請選擇技能與目標";
        // 重要：重置技能圖示與按鈕狀態
        skillManager?.InitSkillIcons(); // 這會呼叫 Reset + UpdateAllSkillButtonStates
    }
    // 玩家按下「確認」後呼叫此方法（由 SkillManager.OnConfirmSkillQueue 最後呼叫）
    public void OnPlayerConfirmSkillQueue()
    {
        if (AllEnemiesDead())
        {
            state = BattleState.WON;
            EndBattle();
            return;
        }

        // 防止重複進入敵人回合
        if (state == BattleState.ENEMYTURN) return;

        StartCoroutine(EnemyTurn());
    }
    public IEnumerator EnemyTurn()
    {
        // --- 新增：等待玩家特效結束 ---
        if (battleFxContainer != null)
        {
            // 當 battleFxContainer 裡面還有子物件時，就持續等待下一幀
            while (battleFxContainer.transform.childCount > 0)
            {
                yield return null; // 等待一幀
            }
        }
        // ----------------------------
        state = BattleState.ENEMYTURN;
        skillManager?.ClearAllHighlights();
        if (dialogueText != null)
            dialogueText.text = "敵人回合...";
        yield return new WaitForSeconds(1f);
        // 反向遍歷，避免銷毀時索引錯亂
        for (int i = enemyUnits.Count - 1; i >= 0; i--)
        {
            if (i >= enemyUnits.Count || enemyUnits[i] == null || enemyUnits[i].currentHP <= 0)
                continue;
            Enemyskill es = enemyPrefabs[i]?.GetComponent<Enemyskill>();
            if (es != null)
            {
                bool executed = es.ExecuteEnemySkill(enemyUnits[i]);
                if (executed)
                    yield return new WaitForSeconds(1.5f);
            }
            // 若此敵人在技能中死亡，立即銷毀
            if (enemyUnits[i] != null && enemyUnits[i].currentHP <= 0)
            {
                DestroyEnemy(i);
            }
        }
        // Buff/Debuff 回合結束 tick
        buffDebuffManager?.TickDefenseDebuffs();
        buffDebuffManager?.TickDefenseBuffs();
        buffDebuffManager?.TickDamageBuffs();
        buffDebuffManager?.TickPoison();
        buffDebuffManager?.TickSkillDisable();
        // 敵人技能冷卻減少
        foreach (GameObject enemyGO in enemyPrefabs)
        {
            if (enemyGO != null)
            {
                Enemyskill es = enemyGO.GetComponent<Enemyskill>();
                es?.ReduceCooldowns();
            }
        }
        // 勝負判定
        if (AllPlayersDead())
        {
            state = BattleState.LOST;
            EndBattle();
        }
        else if (AllEnemiesDead())
        {
            state = BattleState.WON;
            EndBattle();
        }
        else
        {
            // 玩家技能冷卻減少 + UI 重置
            skillManager?.ReduceCooldown(); // 減少冷卻並更新按鈕
            skillManager?.InitSkillIcons(); // 重置圖示與按鈕狀態
            state = BattleState.PLAYERTURN;
            PlayerTurn();
        }
    }
    // 安全銷毀敵人（同步三個列表）
    public void DestroyEnemy(int index)
    {
        if (index < 0 || index >= enemyPrefabs.Count || index >= enemyUnits.Count)
        {
            Debug.LogWarning($"DestroyEnemy 索引無效: {index}");
            return;
        }
        GameObject enemyGO = enemyPrefabs[index];
        Unit enemyUnit = enemyUnits[index];
        if (enemyGO == null || enemyUnit == null || enemyUnit.currentHP > 0)
            return;
        Debug.Log($"銷毀敵人: {enemyUnit.unitName} (索引 {index})");
        // 關閉碰撞器避免誤觸
        Collider2D col2D = enemyGO.GetComponent<Collider2D>();
        if (col2D) col2D.enabled = false;
        Collider col3D = enemyGO.GetComponent<Collider>();
        if (col3D) col3D.enabled = false;
        // 銷毀 GameObject
        Destroy(enemyGO);
        // 同步移除三個列表
        enemyPrefabs.RemoveAt(index);
        enemyUnits.RemoveAt(index);
        if (index < enemyHUDs.Count && enemyHUDs[index] != null)
        {
            Destroy(enemyHUDs[index].gameObject);
            enemyHUDs.RemoveAt(index);
        }
        // 若戰鬥中敵人全滅，立即結束
        if (AllEnemiesDead() && state != BattleState.WON && state != BattleState.LOST && state != BattleState.END)
        {
            state = BattleState.WON;
            EndBattle();
        }
    }
    private bool AllEnemiesDead()
    {
        foreach (Unit enemy in enemyUnits)
            if (enemy != null && enemy.currentHP > 0)
                return false;
        return true;
    }
    private bool AllPlayersDead()
    {
        foreach (Unit player in playerUnits)
            if (player != null && player.currentHP > 0)
                return false;
        return true;
    }
    public void OnRunbutton()
    {
        if (state != BattleState.PLAYERTURN) return;
        if (skillManager != null)
        {
            // 1. 清空兩個玩家的技能選擇
            skillManager.skillQueues[0].Clear();
            skillManager.skillQueues[1].Clear();
            skillManager.selectedSkillNames[0].Clear();
            skillManager.selectedSkillNames[1].Clear();
            skillManager.skillNeedsTarget[0].Clear();
            skillManager.skillNeedsTarget[1].Clear();
            skillManager.selectedTargets.Clear();
            // 2. 清除目標高亮
            skillManager.ClearAllHighlights();
            // 3. 🔥 重置所有玩家技能冷卻為 0（最優雅的方式）
            foreach (var key in new List<string>(skillManager.skillCooldowns.Keys))
            {
                // 只重置玩家技能（key 結尾為 _0 或 _1）
                if (key.EndsWith("_0") || key.EndsWith("_1"))
                {
                    skillManager.skillCooldowns[key] = 0;
                }
            }
            // 4. 重置技能圖示與按鈕狀態
            skillManager.InitSkillIcons(); // 這會呼叫 ResetSkillIcons() + ResetAllSkillButtonStates() + UpdateAllSkillButtonStates()
            // 5. 額外保險：強制更新一次按鈕狀態（確保冷卻歸零後外觀正確）
            skillManager.UpdateAllSkillButtonStates();
            Debug.Log("逃跑成功：已清空技能選擇、目標高亮，並重置所有技能冷卻時間");
        }
        state = BattleState.RUN;
        EndBattle();
    }
    public void EndBattle()
    {
        if (isBattleEnding)
        {
            Debug.LogWarning("EndBattle 已執行中，防止重複呼叫");
            return;
        }
        isBattleEnding = true;
        if (state == BattleState.WON)
        {
            dialogueText.text = "你贏得了戰鬥！";
            GameStateManager.Instance.SetState(GameState.Playing);
            // 贏了：清除所有敵人與 HUD
            foreach (GameObject enemyGO in enemyPrefabs)
            {
                if (enemyGO != null)
                {
                    if (enemyGO.transform.parent != null)
                        Destroy(enemyGO.transform.parent.gameObject);
                    else
                        Destroy(enemyGO);
                }
            }
            enemyPrefabs.Clear();
            enemyUnits.Clear();
            foreach (BattleHUD hud in enemyHUDs)
                if (hud != null) Destroy(hud.gameObject);
            enemyHUDs.Clear();
        }
        else if (state == BattleState.LOST || state == BattleState.RUN)
        {
            dialogueText.text = state == BattleState.LOST ? "你輸了..." : "逃跑成功...";
            GameStateManager.Instance.SetState(GameState.Playing);
            // 輸了或逃跑：保留敵人但回滿血
            foreach (Unit enemy in enemyUnits)
            {
                if (enemy != null)
                {
                    enemy.currentHP = enemy.maxHP;
                    if (enemy.battleHUD != null)
                        enemy.battleHUD.SetHUD(enemy);
                }
            }
        }
        // 清除所有 Buff/Debuff
        buffDebuffManager?.ClearAllEffects();
        // 清除技能選擇高亮與技能清單（EndBattle 通用邏輯）
        skillManager?.ClearAllHighlights();
        skillManager?.ClearSkillIcons();
        // 播放隱藏動畫（只一次）
        if (animator != null && !hideAnimationTriggered)
        {
            animator.SetTrigger("Hide");
            hideAnimationTriggered = true;
        }
        // 延遲重置旗標，準備下一次戰鬥
        StartCoroutine(ResetBattleFlags());
    }
    private IEnumerator ResetBattleFlags()
    {
        // 根據你的 Hide 動畫長度調整（建議 1~2 秒）
        yield return new WaitForSeconds(1.5f);
        isBattleEnding = false;
        hideAnimationTriggered = false;
        isHidden = false;
        state = BattleState.END;
        Debug.Log("戰鬥完全結束，可接受下一次戰鬥觸發");
    }
}