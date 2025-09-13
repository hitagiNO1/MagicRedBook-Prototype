using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum BattleState { START, PLAYERTURN, ENEMYTURN, WON, LOST, RUN, END }

public class BattleSystem : MonoBehaviour
{
    public List<GameObject> playerPrefabs; // 拖拉 player1 和 player2 的 prefab
    public List<GameObject> enemyPrefabs;  // 拖拉 enemy1 和 enemy2 的 prefab

    public List<Unit> playerUnits; // 儲存玩家單位
    public List<Unit> enemyUnits;  // 儲存敵人單位

    public TextMeshProUGUI dialogueText;
    public List<BattleHUD> playerHUDs; // 拖拉 player1 和 player2 的 HUD
    public List<BattleHUD> enemyHUDs;  // 拖拉 enemy1 和 enemy2 的 HUD
    public Enemyskill enemySkill;

    public Animator animator;

    private bool isHidden = false;
    public BattleState state;

    public SkillManager skillManager;
    public BuffDebuffManager buffDebuffManager;

    private List<List<System.Action>> enemySkillsList; // 每個敵人的技能列表
    public int turnCounter = 0;
    private GetBattle getBattle;
    public GameController gameScript;

    // 追蹤玩家是否完成行動
    private List<bool> playersActed; // 記錄每個玩家是否已確認技能隊列
    private Dictionary<int, Unit> selectedTargets; // 每個玩家的目標敵人

    void Start()
    {
        state = BattleState.END;
        // animator = GetComponent<Animator>();
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
        enemySkillsList = new List<List<System.Action>>();
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
        // 初始化玩家單位
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
                playerUnit.battleHUD = playerHUDs[i]; // 設置 Unit 的 battleHUD
            }
            else
            {
                Debug.LogError($"playerHUDs[{i}] 未設置或為空！");
            }
            playersActed.Add(false);
        }

        // 初始化敵人單位
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
                enemyUnit.battleHUD = enemyHUDs[i]; // 設置 Unit 的 battleHUD
            }
            else
            {
                Debug.LogError($"enemyHUDs[{i}] 未設置或為空！");
            }

            Enemyskill enemySkillComponent = enemyPrefabs[i].GetComponent<Enemyskill>();
            if (enemySkillComponent == null)
            {
                Debug.LogError($"無法在 enemyPrefab {i} 上取得 Enemyskill 組件！");
                enemySkillsList.Add(new List<System.Action>());
            }
            else
            {
                enemySkillComponent.playerUnits = playerUnits;
                enemySkillComponent.enemyUnits = enemyUnits;
                enemySkillComponent.buffDebuffManager = buffDebuffManager;
                SetupSkillsForEnemy(enemyUnit.unitName, i, enemySkillComponent);
            }
        }

        getBattle = enemyPrefabs.Count > 0 ? enemyPrefabs[0].GetComponentInChildren<GetBattle>() : null;
        if (getBattle == null)
        {
            Debug.LogWarning("GetBattle 組件未找到，可能影響劇情觸發");
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

        // 重置玩家行動狀態
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

        if (skillManager != null && skillManager.GetTotalSelectedSkills() == 0)
        {
            for (int i = 0; i < playersActed.Count; i++)
            {
                playersActed[i] = true; // 強制標記所有玩家為已行動
            }
            Debug.Log("No skills left, proceeding to enemy turn");
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

        for (int i = 0; i < enemyUnits.Count; i++)
        {
            if (enemyUnits[i].currentHP > 0)
            {
                List<System.Action> skills = enemySkillsList[i];
                if (skills != null && skills.Count > 0)
                {
                    bool skillExecuted = false;
                    int attempts = 0;
                    int maxAttempts = skills.Count; // 修正：移除 const，使用 int

                    while (!skillExecuted && attempts < maxAttempts)
                    {
                        int skillIndex = Random.Range(0, skills.Count);
                        System.Action selectedSkill = skills[skillIndex];

                        // 調用技能（依賴 CheckAndPerform 內部的冷卻檢查）
                        selectedSkill.Invoke();

                        // 簡化判斷：假設 Invoke 後視為執行（實際可根據日誌或返回值優化）
                        skillExecuted = true; // 如果需要精確判斷，可添加返回值邏輯
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
                        yield return new WaitForSeconds(1.5f); // 成功執行後延遲
                    }
                }
            }
        }

        // Buff/Debuff 處理
        if (buffDebuffManager != null)
        {
            buffDebuffManager.TickDefenseDebuffs();
            buffDebuffManager.TickDefenseBuffs();
            buffDebuffManager.TickDamageBuffs();
            buffDebuffManager.TickPoison();
            buffDebuffManager.TickSkillDisable(); // 如果有技能禁用
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
        else
        {
            if (skillManager != null)
            {
                skillManager.ReduceCooldown();
                skillManager.InitSkillIcons();
                skillManager.ClearAllHighlights(); // 清除高光
            }

            state = BattleState.PLAYERTURN;
            PlayerTurn();
        }
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
            if (getBattle != null && getBattle.IfScenes.Count == 1 && gameScript != null)
            {
                gameScript.PlayScene(getBattle.IfScenes[0] as StoryScene);
            }
            else
            {
                GameStateManager.Instance.SetState(GameState.Playing);
            }
            foreach (GameObject enemy in enemyPrefabs)
            {
                if (enemy != null)
                {
                    Destroy(enemy);
                }
            }
        }
        else if (state == BattleState.LOST)
        {
            if (dialogueText != null)
            {
                dialogueText.text = "你輸了...";
            }
            GameStateManager.Instance.SetState(GameState.Playing);
        }
        else if (state == BattleState.RUN)
        {
            if (dialogueText != null)
            {
                dialogueText.text = "逃跑成功...";
            }
            GameStateManager.Instance.SetState(GameState.Playing);
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
        List<System.Action> enemySkills = enemySkillComponent.GetEnemySkills(enemyName);
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