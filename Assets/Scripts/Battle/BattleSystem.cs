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

    // 新增旗標：用來判斷當前回合是否即將結束（用於觸發死亡淡出）
    private bool isRoundEnding = false;

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

        yield return new WaitForSeconds(1f);

        // ---------- SkillManager 初始化 ----------
        skillManager.playerUnits = playerUnits;
        skillManager.enemyUnits = enemyUnits;
        skillManager.enemyHUDs = enemyHUDs;
        skillManager.buffDebuffManager = buffDebuffManager;
        skillManager.dialogueText = dialogueText;
        foreach (Unit player in playerUnits)
        {
            EquipmentManager.Instance.ApplyPermanentEquipmentEffects(player);
        }

        // 等待入場動畫播放完畢
        if (animator != null)
        {
            yield return null;
            while (animator.IsInTransition(0) || animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
            {
                yield return null;
            }
        }

        // ---------- 進入玩家回合 ----------
        state = BattleState.PLAYERTURN;
        PlayerTurn();
    }

    public void PlayerTurn()
    {
        isRoundEnding = false;
        state = BattleState.PLAYERTURN;
        currentPlayerIndex = 0;

        // 這裡很重要！確保對話文字有更新
        if (dialogueText != null)
            dialogueText.text = "玩家回合：請選擇技能與目標";

        // 呼叫 SkillManager 初始化 UI
        if (skillManager != null)
        {
            // 如果你的 AP 儲存在 SkillManager，請確保這裡有重置它
            // 例如：skillManager.currentAP = skillManager.maxAP;
            skillManager.InitSkillIcons();
        }
        else
        {
            Debug.LogError("SkillManager 為空，無法初始化技能！");
        }
    }

    public void OnPlayerConfirmSkillQueue()
    {
        if (AllEnemiesDead())
        {
            state = BattleState.WON;
            EndBattle();
            return;
        }

        if (state == BattleState.ENEMYTURN) return;

        isRoundEnding = true; // 玩家回合結束，準備進入敵人回合
        StartCoroutine(EnemyTurn());
    }

    public IEnumerator EnemyTurn()
    {
        isRoundEnding = false; // 新回合開始，重置旗標

        state = BattleState.ENEMYTURN;
        skillManager?.ClearAllHighlights();

        if (dialogueText != null)
            dialogueText.text = "敵人回合...";

        yield return new WaitForSeconds(1f);

        // 輪流執行：一個敵人攻擊完成後，才輪到下一個
        for (int i = 0; i < enemyUnits.Count; i++)
        {
            Unit currentEnemy = enemyUnits[i];
            if (currentEnemy == null || currentEnemy.isDead)
                continue;

            Enemyskill es = enemyPrefabs[i]?.GetComponent<Enemyskill>();
            if (es != null)
            {
                // ★ 關鍵：使用 yield return 等待這個敵人的技能完全執行完（包含 1.5 秒延遲）
                yield return StartCoroutine(es.ExecuteEnemySkillCoroutine(currentEnemy));
            }

            // 可選：每個敵人攻擊完後加一點額外間隔（讓畫面呼吸一下）
            yield return new WaitForSeconds(0.5f);  // 這個 0.5f 可以調整或刪除
        }

        // Buff/Debuff 回合結束處理
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
            skillManager?.ReduceCooldown();
            skillManager?.InitSkillIcons();
            isRoundEnding = true;
            state = BattleState.PLAYERTURN;
            PlayerTurn();
        }
    }

    // 標記敵人死亡（關閉碰撞器、更新HUD，並啟動淡出協程）
    public void MarkEnemyAsDead(int index)
    {
      
       /* if (index < 0 || index >= enemyUnits.Count || index >= enemyPrefabs.Count)
            return;*/

        Unit enemyUnit = enemyUnits[index];
        GameObject enemyGO = enemyPrefabs[index];

        /*if (enemyUnit == null || enemyGO == null || enemyUnit.isDead)
            return;*/

        enemyUnit.isDead = true;
        enemyUnit.currentHP = 0;

        // 更新 HUD
       /* if (index < enemyHUDs.Count && enemyHUDs[index] != null)
            enemyHUDs[index].SetHP(0);*/
      
        // 關閉碰撞器
        Collider2D col2D = enemyGO.GetComponent<Collider2D>();
        if (col2D) col2D.enabled = false;
        Collider col3D = enemyGO.GetComponent<Collider>();
        if (col3D) col3D.enabled = false;
       

        Debug.Log($"敵人 {enemyUnit.unitName} 已死亡，將在回合結束後開始淡出消失");

        // 啟動淡出協程（等待回合結束後才開始 1 秒淡出）
        StartCoroutine(FadeOutEnemyOnRoundEnd(enemyGO, index));
    }

    // 淡出協程：等待回合結束後才開始 1 秒淡出
    private IEnumerator FadeOutEnemyOnRoundEnd(GameObject enemyGO, int index)
    {
        // 等待回合結束旗標為 true
     //   yield return new WaitUntil(() => isRoundEnding);

        SpriteRenderer[] renderers = enemyGO.GetComponentsInChildren<SpriteRenderer>();
        if (renderers.Length == 0)
        {
            Debug.LogWarning($"敵人物件 {enemyGO.name} 沒有 SpriteRenderer，無法執行淡出效果");
            yield break;
        }

        float fadeDuration = 1.0f;
        float timer = 0f;

        // 記錄初始顏色（確保不會影響原始顏色）
        Color[] initialColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            initialColors[i] = renderers[i].color;
        }

        // 逐幀淡出
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);

            for (int i = 0; i < renderers.Length; i++)
            {
                Color newColor = initialColors[i];
                newColor.a = alpha;
                renderers[i].color = newColor;
            }

            yield return null;
        }

        // 確保最終 alpha = 0
        for (int i = 0; i < renderers.Length; i++)
        {
            Color finalColor = initialColors[i];
            finalColor.a = 0f;
            renderers[i].color = finalColor;
        }

        Debug.Log($"敵人 {enemyUnits[index].unitName} 已完成淡出消失動畫");
    }

    private bool AllEnemiesDead()
    {
        foreach (Unit enemy in enemyUnits)
        {
            if (enemy != null && !enemy.isDead)
                return false;
        }
        return true;
    }

    private bool AllPlayersDead()
    {
        foreach (Unit player in playerUnits)
        {
            if (player != null && player.currentHP > 0)
                return false;
        }
        return true;
    }

    public void OnRunbutton()
    {
        if (state != BattleState.PLAYERTURN) return;

        if (skillManager != null)
        {
            skillManager.skillQueues[0].Clear();
            skillManager.skillQueues[1].Clear();
            skillManager.selectedSkillNames[0].Clear();
            skillManager.selectedSkillNames[1].Clear();
            skillManager.skillNeedsTarget[0].Clear();
            skillManager.skillNeedsTarget[1].Clear();
            skillManager.selectedTargets.Clear();

            skillManager.ClearAllHighlights();

            foreach (var key in new List<string>(skillManager.skillCooldowns.Keys))
            {
                if (key.EndsWith("_0") || key.EndsWith("_1"))
                {
                    skillManager.skillCooldowns[key] = 0;
                }
            }

            skillManager.InitSkillIcons();
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

            // 勝利時才真正銷毀所有敵人物件與 HUD
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

            // 輸了或逃跑：復活所有敵人（恢復血量、透明度、碰撞器）
            for (int i = 0; i < enemyUnits.Count; i++)
            {
                Unit enemy = enemyUnits[i];
                GameObject enemyGO = enemyPrefabs[i];

                if (enemy != null)
                {
                    enemy.currentHP = enemy.maxHP;
                    enemy.isDead = false;

                    // 恢復透明度
                    SpriteRenderer[] renderers = enemyGO.GetComponentsInChildren<SpriteRenderer>();
                    foreach (SpriteRenderer sr in renderers)
                    {
                        Color c = sr.color;
                        c.a = 1f;
                        sr.color = c;
                    }

                    // 重新開啟碰撞器
                    Collider2D col2D = enemyGO.GetComponent<Collider2D>();
                    if (col2D) col2D.enabled = true;
                    Collider col3D = enemyGO.GetComponent<Collider>();
                    if (col3D) col3D.enabled = true;

                    // 更新 HUD
                    if (i < enemyHUDs.Count && enemyHUDs[i] != null)
                        enemyHUDs[i].SetHUD(enemy);
                }
            }
        }

        // 清除所有 Buff/Debuff
        buffDebuffManager?.ClearAllEffects();

        // 清除技能相關 UI
        skillManager?.ClearAllHighlights();
        skillManager?.ClearSkillIcons();

        // 播放隱藏動畫
        if (animator != null && !hideAnimationTriggered)
        {
            animator.SetTrigger("Hide");
            hideAnimationTriggered = true;
        }

        StartCoroutine(ResetBattleFlags());
    }

    private IEnumerator ResetBattleFlags()
    {
        yield return new WaitForSeconds(1.5f);

        isBattleEnding = false;
        hideAnimationTriggered = false;
        isHidden = false;
        isRoundEnding = false; // 重置回合旗標
        state = BattleState.END;
        Debug.Log("戰鬥完全結束，可接受下一次戰鬥觸發");
    }
}