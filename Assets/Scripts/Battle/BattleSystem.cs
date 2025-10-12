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
    private List<List<Func<bool>>> enemySkillsList; // 每個敵人的技能列表
    public int turnCounter = 0;
    public GameController gameScript;
    private List<bool> playersActed; // 記錄每個玩家是否已確認技能隊列
    private Dictionary<int, Unit> selectedTargets; // 每個玩家的目標敵人

    public static BattleSystem Instance { get; private set; }
    void Start()
    {
        // 設定 Instance（單例模式）
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

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
        enemySkillsList = new List<List<Func<bool>>>();
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

            Enemyskill enemySkillComponent = enemyPrefabs[i].GetComponent<Enemyskill>();
            if (enemySkillComponent == null)
            {
                Debug.LogError($"無法在 enemyPrefab {i} 上取得 Enemyskill 組件！");
                enemySkillsList.Add(new List<Func<bool>>());
            }
            else
            {
                enemySkillComponent.playerUnits = playerUnits;
                enemySkillComponent.enemyUnits = enemyUnits;
                enemySkillComponent.buffDebuffManager = buffDebuffManager;
                SetupSkillsForEnemy(enemyUnit.unitName, i, enemySkillComponent);
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
                List<Func<bool>> skills = enemySkillsList[i];
                if (skills == null)
                {
                    Debug.LogWarning($"敵人 {enemyUnits[i].unitName} 的技能列表為 null，跳過行動");
                    continue;
                }

                if (skills.Count == 0)
                {
                    Debug.LogWarning($"敵人 {enemyUnits[i].unitName} 的技能列表為空，跳過行動");
                    continue;
                }

                bool skillExecuted = false;
                int attempts = 0;
                int maxAttempts = skills.Count;

                while (!skillExecuted && attempts < maxAttempts)
                {
                    int skillIndex = Random.Range(0, skills.Count); // 已確保 skills.Count > 0
                    Func<bool> selectedSkill = skills[skillIndex];
                    skillExecuted = selectedSkill.Invoke();
                    attempts++;
                    if (!skillExecuted)
                    {
                        Debug.Log($"敵人 {enemyUnits[i].unitName} 的技能 {skillIndex} 正在冷卻中，重選...");
                    }
                }

                if (!skillExecuted)
                {
                    Debug.LogWarning($"敵人 {enemyUnits[i].unitName} 所有技能都在冷卻中，跳過行動");
                }
                else
                {
                    yield return new WaitForSeconds(1.5f);
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

        // 減免冷卻
        if (enemySkill != null)
        {
            enemySkill.ReduceCooldowns();
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
                skillManager.ClearAllHighlights();
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
            Destroy(enemyPrefabs[index]);
            enemyUnits.RemoveAt(index);
            if (index < enemyHUDs.Count && enemyHUDs[index] != null)
            {
                enemyHUDs[index].gameObject.SetActive(false);
                Destroy(enemyHUDs[index].gameObject); // 銷毀 HUD
                enemyHUDs.RemoveAt(index);
            }
            if (index < enemySkillsList.Count)
            {
                enemySkillsList.RemoveAt(index); // 同步移除技能列表
            }
            Debug.Log($"敵人 {enemyUnits[index].unitName} 血量歸零，立即銷毀（剩餘敵人數：{enemyUnits.Count}）");
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
            if (dialogueText != null)
            {
                dialogueText.text = state == BattleState.LOST ? "你輸了..." : "逃跑成功...";
            }
            GameStateManager.Instance.SetState(GameState.Playing);
            // 銷毀所有剩餘敵人和 HUD 修改
            foreach (GameObject enemy in enemyPrefabs)
            {
                if (enemy != null)
                {
                    Destroy(enemy);
                }
            }

            enemyPrefabs.Clear();
            foreach (var hud in enemyHUDs)
            {
                if (hud != null)
                {
                    Destroy(hud.gameObject);
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

    public void SetupSkillsForEnemy(string enemyName, int enemyIndex, Enemyskill enemySkillComponent)
    {
        List<Func<bool>> enemySkills = enemySkillComponent.GetEnemySkills(enemyName);
        if (enemySkills == null)
        {
            Debug.LogError($"敵人 {enemyName} 的技能列表為 null，初始化為空列表");
            enemySkills = new List<Func<bool>>();
        }
        enemySkillsList.Add(enemySkills);
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