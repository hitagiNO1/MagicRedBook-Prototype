using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Random = UnityEngine.Random;
public enum BattleState { START, PLAYERTURN, ENEMYTURN, WON, LOST, RUN, END }
public class BattleSystem : MonoBehaviour
{
    public List<GameObject> playerPrefabs;
    public List<GameObject> enemyPrefabs;
    public List<Unit> playerUnits;
    public List<Unit> enemyUnits;
    public TextMeshProUGUI dialogueText;
    public List<BattleHUD> playerHUDs;
    public List<BattleHUD> enemyHUDs;
    public Enemyskill enemySkill;
    public Animator animator;
    private bool isHidden = false;
    public BattleState state;
    public SkillManager skillManager;
    public BuffDebuffManager buffDebuffManager;
    public int turnCounter = 0;
    public GameController gameScript;
    private List<bool> playersActed;
    private Dictionary<int, Unit> selectedTargets;
    // 新增：防止重複結束戰鬥
    public int currentPlayerIndex = 0;
    private bool isBattleEnding = false;
    private bool hideAnimationTriggered = false;
    public mutiplebattle battleTrigger;

    void Start()
    {
        state = BattleState.END;
        if (animator == null)
        {
            Debug.LogError("Animator 未附加到 BattleSystem 的 GameObject 上！");
        }
        skillManager = GetComponent<SkillManager>();
        if (skillManager == null)
        {
            Debug.LogError("SkillManager 未附加到 BattleSystem 的 GameObject 上！");
        }
        buffDebuffManager = GetComponent<BuffDebuffManager>();
        if (buffDebuffManager == null)
        {
            Debug.LogError("BuffDebuffManager 未附加到 BattleSystem 的 GameObject 上！");
        }
        playerUnits = new List<Unit>();
        enemyUnits = new List<Unit>();
        playersActed = new List<bool>();
        selectedTargets = new Dictionary<int, Unit>();
    }
    private void Update()
    {
        if (state == BattleState.START && !isHidden)
        {
            StartCoroutine(SetupBattle());
            GameStateManager.Instance.SetState(GameState.Battle);
            if (animator != null)
            {
                animator.SetTrigger("Show");
            }
            isHidden = true;
        }
    }
    public IEnumerator SetupBattle()
    {
        // ---------- 玩家初始化（保持不變） ----------
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
            playersActed.Add(false);
        }

        // ---------- 敵人初始化（關鍵修改） ----------
        enemyUnits.Clear(); // 先清空

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

            // HUD 已經由 mutiplebattle 綁定，這裡只需確認
            if (i < enemyHUDs.Count && enemyHUDs[i] != null)
            {
                enemyHUDs[i].SetHUD(enemyUnit);
                enemyUnit.battleHUD = enemyHUDs[i]; // 確保 battleHUD 正確
            }
            else
            {
                Debug.LogWarning($"enemyHUDs[{i}] 為 null，HUD 未綁定！");
            }

            // 設定 Enemyskill（每個敵人有自己的）
            Enemyskill enemySkillComp = enemyGO.GetComponent<Enemyskill>();
            if (enemySkillComp != null)
            {
                enemySkillComp.playerUnits = playerUnits;
                enemySkillComp.enemyUnits = enemyUnits;
                enemySkillComp.buffDebuffManager = buffDebuffManager;
                Debug.Log($"敵人 {enemyUnit.unitName} 的 Enemyskill 已設定");
            }
        }

        // ---------- 對話與延遲 ----------
        if (dialogueText != null)
            dialogueText.text = "野生的敵人們出現了..";

        yield return new WaitForSeconds(1f);

        // ---------- SkillManager 設定 ----------
        skillManager.playerUnits = playerUnits;
        skillManager.enemyUnits = enemyUnits;
        skillManager.buffDebuffManager = buffDebuffManager;

        if (skillManager != null)
            StartCoroutine(skillManager.DelayedInitSkillIcons(2f));

        // ---------- 進入玩家回合 ----------
        state = BattleState.PLAYERTURN;
        PlayerTurn();
    }
    public void PlayerTurn()
    {
        state = BattleState.PLAYERTURN;
        if (dialogueText != null)
        {
            dialogueText.text = "玩家回合: 選擇你們的行動和目標敵人";
        }
        for (int i = 0; i < playersActed.Count; i++)
        {
            playersActed[i] = false;
        }
        selectedTargets.Clear();
    }
    public void OnPlayerConfirmSkillQueue()
    {
        if (AllEnemiesDead())
        {
            state = BattleState.WON;
            EndBattle();
            return;
        }
        state = BattleState.ENEMYTURN;
        StartCoroutine(EnemyTurn());
    }
    public IEnumerator EnemyTurn()
    {
        skillManager.ClearAllHighlights();
        if (dialogueText != null) dialogueText.text = "敵人們的回合...";
        yield return new WaitForSeconds(1f);

        // ✅ 反向迴圈：從後往前，避免索引錯亂
        for (int i = enemyUnits.Count - 1; i >= 0; i--)
        {
            // 檢查敵人是否還活著
            if (enemyUnits[i] == null || enemyUnits[i].currentHP <= 0)
            {
                Debug.LogWarning($"敵人 {i} 已死亡，跳過");
                continue;
            }

            // 執行敵人技能
            Enemyskill es = enemyPrefabs[i]?.GetComponent<Enemyskill>();
            if (es != null)
            {
                bool skillExecuted = es.ExecuteEnemySkill(enemyUnits[i]);
                if (skillExecuted) yield return new WaitForSeconds(1.5f);
            }

            // 立即檢查死亡並銷毀
            if (enemyUnits[i] != null && enemyUnits[i].currentHP <= 0)
            {
                DestroyEnemy(i);  // 這裡會自動同步所有列表
            }
        }

        // Buff/Debuff 處理
        buffDebuffManager?.TickDefenseDebuffs();
        buffDebuffManager?.TickDefenseBuffs();
        buffDebuffManager?.TickDamageBuffs();
        buffDebuffManager?.TickPoison();
        buffDebuffManager?.TickSkillDisable();
        currentPlayerIndex = (currentPlayerIndex + 1) % playerUnits.Count;

        // 冷卻處理（也用反向迴圈）
        for (int i = enemyPrefabs.Count - 1; i >= 0; i--)
        {
            if (enemyPrefabs[i] != null)
            {
                Enemyskill es = enemyPrefabs[i].GetComponent<Enemyskill>();
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
            skillManager?.ReduceCooldown();
            skillManager?.InitSkillIcons();
            state = BattleState.PLAYERTURN;
            PlayerTurn();
        }
    }
    // 優化版：安全銷毀敵人
    public void DestroyEnemy(int index)
    {
        // 多重安全檢查
        if (index < 0 || index >= enemyPrefabs.Count || index >= enemyUnits.Count)
        {
            Debug.LogWarning($"DestroyEnemy({index}): 索引超出範圍，跳過");
            return;
        }

        GameObject enemyGO = enemyPrefabs[index];
        Unit enemyUnit = enemyUnits[index];
        if (enemyGO == null || enemyUnit == null || enemyUnit.currentHP > 0)
        {
            Debug.LogWarning($"DestroyEnemy({index}): 敵人無效或還活著，跳過");
            return;
        }

        Debug.Log($"銷毀敵人: {enemyUnit.unitName} (索引 {index})");

        // 關閉碰撞器（避免滑鼠誤觸）
        Collider2D col2D = enemyGO.GetComponent<Collider2D>();
        if (col2D != null) col2D.enabled = false;
        Collider col3D = enemyGO.GetComponent<Collider>();
        if (col3D != null) col3D.enabled = false;

        // 銷毀敵人物件
        Destroy(enemyGO);

        // ✅ 同步移除所有列表（同一個索引）
        enemyPrefabs.RemoveAt(index);
        enemyUnits.RemoveAt(index);

        // ✅ HUD 同步移除（關鍵！）
        if (index < enemyHUDs.Count && enemyHUDs[index] != null)
        {
            Destroy(enemyHUDs[index].gameObject);
            enemyHUDs.RemoveAt(index);
            Debug.Log($"同步移除 HUD (索引 {index})");
        }

        Debug.Log($"敵人銷毀完成，剩餘: {enemyPrefabs.Count} 隻");
    }
    private bool HasAnyPlayerActed()
    {
        foreach (bool acted in playersActed)
            if (acted) return true;
        return false;
    }
    private bool AllPlayersActed()
    {
        foreach (bool acted in playersActed)
            if (!acted) return false;
        return true;
    }
    private bool AllEnemiesDead()
    {
        foreach (Unit enemy in enemyUnits)
            if (enemy.currentHP > 0) return false;
        return true;
    }
    private bool AllPlayersDead()
    {
        foreach (Unit player in playerUnits)
            if (player.currentHP > 0) return false;
        return true;
    }
    public void OnRunbutton()
    {
        if (state != BattleState.PLAYERTURN) return;
        StartCoroutine(PlayerRun());
    }
    IEnumerator PlayerRun()
    {
        state = BattleState.RUN;
        yield return new WaitForSeconds(0.5f);
        EndBattle();
    }
    // 關鍵修復：防止動畫重複播放
    public void EndBattle()
    {
        if (isBattleEnding)
        {
            Debug.LogWarning("EndBattle 已執行中，忽略重複呼叫");
            return;
        }
        isBattleEnding = true;
        if (state == BattleState.WON)
        {
            if (dialogueText != null)
                dialogueText.text = "你贏得了戰鬥!";
            GameStateManager.Instance.SetState(GameState.Playing);
            // 銷毀敵人與 HUD
            for (int i = enemyPrefabs.Count - 1; i >= 0; i--)
            {
                if (enemyPrefabs[i] != null)
                {
                    Transform parent = enemyPrefabs[i].transform.parent;
                    if (parent != null) Destroy(parent.gameObject);
                    else Destroy(enemyPrefabs[i]);
                }
            }
            enemyPrefabs.Clear();
            foreach (var hud in enemyHUDs)
                if (hud != null) Destroy(hud.gameObject);
            enemyHUDs.Clear();
        }
        else if (state == BattleState.LOST || state == BattleState.RUN)
        {
            skillManager.ClearAllHighlights();
            if (dialogueText != null)
            {
                dialogueText.text = state == BattleState.LOST ? "你輸了..." : "逃跑成功...";
            }
            // 關鍵修復：逃跑或失敗也必須恢復 Playing 狀態
            GameStateManager.Instance.SetState(GameState.Playing);
            // 回滿敵人血量（保留敵人）
            foreach (Unit enemy in enemyUnits)
            {
                if (enemy != null)
                {
                    enemy.currentHP = enemy.maxHP;
                    if (enemy.battleHUD != null)
                        enemy.battleHUD.SetHUD(enemy);
                }
            }
            enemyHUDs.Clear();
        }
        // 清理 Buff/Debuff
        if (buffDebuffManager != null)
            buffDebuffManager.ClearAllEffects();
        // 動畫只觸發一次
        if (animator != null && !hideAnimationTriggered)
        {
            animator.SetTrigger("Hide");
            hideAnimationTriggered = true;
        }
        if (skillManager != null)
            skillManager.ClearSkillIcons();
        isHidden = false;
        StartCoroutine(ResetBattleEndFlags());
    }
    // 延遲重置旗標，支援連續戰鬥
    private IEnumerator ResetBattleEndFlags()
    {
        yield return new WaitForSeconds(1.5f); // 根據 Hide 動畫長度調整
        isBattleEnding = false;
        hideAnimationTriggered = false;
        state = BattleState.END;
        Debug.Log("戰鬥結束旗標已重置");
    }
    public void SetPlayerTarget(int playerIndex, Unit target)
    {
        if (playerIndex >= 0 && playerIndex < playerUnits.Count && enemyUnits.Contains(target))
        {
            selectedTargets[playerIndex] = target;
        }
        else
        {
            Debug.LogError($"無效的 playerIndex {playerIndex} 或 target {target?.unitName}");
        }
    }
    public Unit GetPlayerTarget(int playerIndex)
    {
        selectedTargets.TryGetValue(playerIndex, out Unit target);
        return target;
    }
}