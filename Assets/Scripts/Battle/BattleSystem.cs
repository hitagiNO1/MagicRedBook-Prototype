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
    private bool isBattleEnding = false;
    private bool hideAnimationTriggered = false;

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
        // 初始化玩家單位和 HUD
        for (int i = 0; i < playerPrefabs.Count; i++)
        {
            if (playerPrefabs[i] == null)
            {
                Debug.LogError($"playerPrefabs[{i}] 為空！");
                yield break;
            }
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
            else
            {
                Debug.LogError($"playerHUDs[{i}] 未設置或為空！");
            }
            playersActed.Add(false);
        }

        // 初始化敵人單位和 HUD
        for (int i = 0; i < enemyPrefabs.Count; i++)
        {
            if (enemyPrefabs[i] == null)
            {
                Debug.LogError($"enemyPrefabs[{i}] 為空！");
                yield break;
            }
            Unit enemyUnit = enemyPrefabs[i].GetComponent<Unit>();
            if (enemyUnit == null)
            {
                Debug.LogError($"enemyPrefabs[{i}] 缺少 Unit 組件！");
                yield break;
            }
            enemyUnits.Add(enemyUnit);

            if (i < enemyHUDs.Count && enemyHUDs[i] != null)
            {
                enemyHUDs[i].SetHUD(enemyUnit);
                enemyUnit.battleHUD = enemyHUDs[i];
            }
            else
            {
                Debug.LogError($"enemyHUDs[{i}] 未設置或為空！");
            }

            Enemyskill enemySkillComponent = enemyPrefabs[i].GetComponent<Enemyskill>();
            if (enemySkillComponent == null)
            {
                Debug.LogError($"無法在 enemyPrefab {i} 上取得 Enemyskill 組件！");
            }
            else
            {
                enemySkillComponent.playerUnits = playerUnits;
                enemySkillComponent.enemyUnits = enemyUnits;
                enemySkillComponent.buffDebuffManager = buffDebuffManager;
                Debug.Log($"敵人 {enemyUnit.unitName} 的技能組件已設置完成");
            }
        }

        if (dialogueText != null)
        {
            dialogueText.text = "野生的敵人們出現了..";
        }

        yield return new WaitForSeconds(1f);

        skillManager.playerUnits = playerUnits;
        skillManager.enemyUnits = enemyUnits;
        skillManager.buffDebuffManager = buffDebuffManager;

        if (skillManager != null)
        {
            StartCoroutine(skillManager.DelayedInitSkillIcons(2f));
        }

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
        if (dialogueText != null)
        {
            dialogueText.text = "敵人們的回合...";
        }
        yield return new WaitForSeconds(1f);

        // 反向迴圈，安全處理每個敵人
        for (int i = enemyUnits.Count - 1; i >= 0; i--)
        {
            // 關鍵修復 1：檢查敵人是否還活著
            if (i >= enemyPrefabs.Count || enemyPrefabs[i] == null || enemyUnits[i] == null || enemyUnits[i].currentHP <= 0)
            {
                Debug.LogWarning($"敵人 {i} 已銷毀或死亡，跳過");
                continue;
            }

            // 關鍵修復 2：安全獲取 Enemyskill
            Enemyskill enemySkillComponent = enemyPrefabs[i]?.GetComponent<Enemyskill>();
            if (enemySkillComponent != null)
            {
                bool skillExecuted = enemySkillComponent.ExecuteEnemySkill(enemyUnits[i]);
                if (skillExecuted)
                {
                    yield return new WaitForSeconds(1.5f);
                }
            }

            // 關鍵修復 3：死亡後立即銷毀（已檢查 null）
            if (enemyUnits[i].currentHP <= 0)
            {
                DestroyEnemy(i);
            }
        }

        // Buff/Debuff 處理（保持不變）
        if (buffDebuffManager != null)
        {
            buffDebuffManager.TickDefenseDebuffs();
            buffDebuffManager.TickDefenseBuffs();
            buffDebuffManager.TickDamageBuffs();
            buffDebuffManager.TickPoison();
            buffDebuffManager.TickSkillDisable();
        }

        // 減免冷卻（安全迴圈）
        for (int i = enemyPrefabs.Count - 1; i >= 0; i--)
        {
            if (enemyPrefabs[i] != null)
            {
                Enemyskill enemySkillComponent = enemyPrefabs[i].GetComponent<Enemyskill>();
                if (enemySkillComponent != null)
                {
                    enemySkillComponent.ReduceCooldowns();
                }
            }
        }

        // 勝利/失敗檢查
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
            if (skillManager != null)
            {
                skillManager.ReduceCooldown();
                skillManager.InitSkillIcons();
            }
            state = BattleState.PLAYERTURN;
            PlayerTurn();
        }
    }

    // 優化版：安全銷毀敵人
    public void DestroyEnemy(int index)
    {
        // 多重檢查
        if (index < 0 || index >= enemyUnits.Count ||
            index >= enemyPrefabs.Count ||
            enemyPrefabs[index] == null ||
            enemyUnits[index] == null)
        {
            Debug.LogWarning($"DestroyEnemy({index})：索引或物件無效，跳過");
            return;
        }

        // 禁用碰撞器
        Collider2D col2D = enemyPrefabs[index].GetComponent<Collider2D>();
        if (col2D != null) col2D.enabled = false;
        Collider col3D = enemyPrefabs[index].GetComponent<Collider>();
        if (col3D != null) col3D.enabled = false;

        Debug.Log($"銷毀敵人 {enemyUnits[index].unitName}（剩餘：{enemyUnits.Count - 1}）");

        // 銷毀父物件
        Transform parent = enemyPrefabs[index].transform.parent;
        if (parent != null)
        {
            Destroy(parent.gameObject);
        }
        else
        {
            Destroy(enemyPrefabs[index]);
        }

        // 同步移除
        enemyPrefabs.RemoveAt(index);
        enemyUnits.RemoveAt(index);

        // 移除 HUD
        if (index < enemyHUDs.Count && enemyHUDs[index] != null)
        {
            Destroy(enemyHUDs[index].gameObject);
            enemyHUDs.RemoveAt(index);
        }
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