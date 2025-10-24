using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Random = UnityEngine.Random; // 添加別名，解決模擬兩可問題

public enum BattleState { START, PLAYERTURN, ENEMYTURN, WON, LOST, RUN, END }

public class BattleSystem : MonoBehaviour
{
    public List<GameObject> playerPrefabs; // 拖拉 player1 和 player2 的 prefab
    public List<GameObject> enemyPrefabs;  // 敵人預製件列表
    public List<Unit> playerUnits; // 儲存玩家單位
    public List<Unit> enemyUnits;  // 儲存敵人單位
    public TextMeshProUGUI dialogueText;
    public List<BattleHUD> playerHUDs; // 拖拉 player1 和 player2 的 HUD
    public List<BattleHUD> enemyHUDs;  // 動態設置的敵人 HUD
    public Enemyskill enemySkill;
    public Animator animator;
    private bool isHidden = false;
    public BattleState state;
    public SkillManager skillManager;
    public BuffDebuffManager buffDebuffManager;
    public int turnCounter = 0;
    public GameController gameScript;
    private List<bool> playersActed; // 記錄每個玩家是否已確認技能隊列
    private Dictionary<int, Unit> selectedTargets; // 每個玩家的目標敵人

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

            // 使用 mutiplebattle 傳遞的 enemyHUDs
            if (i < enemyHUDs.Count && enemyHUDs[i] != null)
            {
                enemyHUDs[i].SetHUD(enemyUnit);
                enemyUnit.battleHUD = enemyHUDs[i];
            }
            else
            {
                Debug.LogError($"enemyHUDs[{i}] 未設置或為空！");
            }

            // 設置敵人技能組件
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

    public void OnPlayerConfirmSkillQueue(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= playerUnits.Count)
        {
            Debug.LogError($"無效的 playerIndex: {playerIndex}");
            return;
        }

        playersActed[playerIndex] = true;
        Debug.Log($"Player {playerIndex} acted: {playersActed[playerIndex]}, Total Skills = {skillManager.GetTotalSelectedSkills()}");

        // 修改：只要有任一玩家acted，就進入 ENEMYTURN
        if (HasAnyPlayerActed())
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
    }

    public IEnumerator EnemyTurn()
    {
        skillManager.ClearAllHighlights();

        if (dialogueText != null)
        {
            dialogueText.text = "敵人們的回合...";
        }

        yield return new WaitForSeconds(1f);

        // 使用反向迴圈以安全移除敵人
        for (int i = enemyUnits.Count - 1; i >= 0; i--)
        {
            if (enemyUnits[i].currentHP > 0)
            {
                // 獲取敵人的 Enemyskill 組件
                Enemyskill enemySkillComponent = enemyPrefabs[i].GetComponent<Enemyskill>();

                if (enemySkillComponent != null)
                {
                    // 使用新的技能執行方法
                    bool skillExecuted = enemySkillComponent.ExecuteEnemySkill(enemyUnits[i]);

                    if (skillExecuted)
                    {
                        yield return new WaitForSeconds(1.5f);
                    }
                    else
                    {
                        Debug.LogWarning($"敵人 {enemyUnits[i].unitName} 無法執行任何技能");
                    }
                }
                else
                {
                    Debug.LogError($"敵人 {enemyUnits[i].unitName} 缺少 Enemyskill 組件");
                }
            }

            // 檢查血量是否為 0，並即時銷毀
            if (enemyUnits[i].currentHP <= 0 && i < enemyPrefabs.Count && enemyPrefabs[i] != null)
            {
                DestroyEnemy(i);
            }
        }

        // Buff/Debuff 處理
        if (buffDebuffManager != null)
        {
            buffDebuffManager.TickDefenseDebuffs();
            buffDebuffManager.TickDefenseBuffs();
            buffDebuffManager.TickDamageBuffs();
            buffDebuffManager.TickPoison();
            buffDebuffManager.TickSkillDisable();
        }

        // 減免冷卻 - 對所有敵人的技能組件調用
        for (int i = 0; i < enemyPrefabs.Count; i++)
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

    // 新增：獨立處理敵人死亡的銷毀方法
    public void DestroyEnemy(int index)
    {
        if (index >= 0 && index < enemyPrefabs.Count && enemyPrefabs[index] != null)
        {
            Collider2D collider2D = enemyPrefabs[index].GetComponent<Collider2D>();
            if (collider2D != null) collider2D.enabled = false;

            Collider collider3D = enemyPrefabs[index].GetComponent<Collider>();
            if (collider3D != null) collider3D.enabled = false;

            Debug.Log($"敵人 {enemyUnits[index].unitName} 血量歸零，立即銷毀（剩餘敵人數：{enemyUnits.Count - 1}）");

            Destroy(enemyPrefabs[index]);
            enemyPrefabs.RemoveAt(index); // 同步移除 prefab
            enemyUnits.RemoveAt(index);

            if (index < enemyHUDs.Count && enemyHUDs[index] != null)
            {
                enemyHUDs[index].gameObject.SetActive(false);
                Destroy(enemyHUDs[index].gameObject);
                enemyHUDs.RemoveAt(index);
            }
        }
    }

    private bool HasAnyPlayerActed()
    {
        foreach (bool acted in playersActed)
        {
            if (acted) return true;
        }
        return false;
    }

    private bool AllPlayersActed()
    {
        foreach (bool acted in playersActed)
        {
            if (!acted) return false;
        }
        return true;
    }

    private bool AllEnemiesDead()
    {
        foreach (Unit enemy in enemyUnits)
        {
            if (enemy.currentHP > 0) return false;
        }
        return true;
    }

    private bool AllPlayersDead()
    {
        foreach (Unit player in playerUnits)
        {
            if (player.currentHP > 0) return false;
        }
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

    public void EndBattle()
    {
        if (state == BattleState.WON)
        {
            if (dialogueText != null)
            {
                dialogueText.text = "你贏得了戰鬥!";
            }
            GameStateManager.Instance.SetState(GameState.Playing);

            // 戰鬥勝利時銷毀所有剩餘敵人
            foreach (GameObject enemy in enemyPrefabs)
            {
                if (enemy != null)
                {
                    Debug.Log("Parent: " + enemy.transform.parent.name);
                    Destroy(enemy.transform.parent.gameObject);
                }
            }
            enemyPrefabs.Clear();

            // 銷毀所有敵人 HUD
            foreach (var hud in enemyHUDs)
            {
                if (hud != null)
                {
                    Destroy(hud.gameObject);
                }
            }
            enemyHUDs.Clear();
        }
        else if (state == BattleState.LOST || state == BattleState.RUN)
        {
            skillManager.ClearAllHighlights();
            if (dialogueText != null)
            {
                dialogueText.text = state == BattleState.LOST ? "你輸了..." : "逃跑成功...";
            }
            GameStateManager.Instance.SetState(GameState.Playing);

            // 保留敵人並回滿血量
            foreach (Unit enemy in enemyUnits)
            {
                if (enemy != null)
                {
                    enemy.currentHP = enemy.maxHP; // 回滿敵人血量
                    if (enemy.battleHUD != null)
                    {
                        enemy.battleHUD.SetHUD(enemy); // 更新 HUD 顯示
                    }
                }
            }
            enemyHUDs.Clear();
        }

        if (buffDebuffManager != null)
        {
            buffDebuffManager.ClearAllEffects();
        }

        if (animator != null)
        {
            animator.SetTrigger("Hide");
        }

        if (skillManager != null)
        {
            skillManager.ClearSkillIcons();
        }

        isHidden = false;
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