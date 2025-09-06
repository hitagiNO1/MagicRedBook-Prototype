using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillManager : MonoBehaviour
{
    public List<Unit> playerUnits; // 支援多玩家
    public List<Unit> enemyUnits;  // 支援多敵人
    public TextMeshProUGUI dialogueText;
    public List<BattleHUD> enemyHUDs; // 支援多敵人 HUD
    public BattleSystem battlesystem;
    public BuffDebuffManager buffDebuffManager;

    // 每個玩家的冷卻時間字典（key: skillName + playerIndex, value: remaining turns）
    private Dictionary<string, int> skillCooldowns = new Dictionary<string, int>();

    private int skill2CooldownTurns = 3;
    private int skill3CooldownTurns = 2;
    private int skill4CooldownTurns = 3;
    private int skill5CooldownTurns = 2;
    private int catskill2CooldownTurns = 2;
    private int catskill3CooldownTurns = 1;
    private int catskill4CooldownTurns = 2;
    private int catskill5CooldownTurns = 3;

    public AudioSource audioSource;
    public GameObject skillIconPrefab;
    public Transform skillIconParent; // 單一共享的技能圖示父物件
    private List<GameObject> skillIcons = new List<GameObject>(); // 共享的圖示列表
    private Dictionary<int, List<System.Action>> skillQueues = new Dictionary<int, List<System.Action>>();
    private Dictionary<int, List<string>> selectedSkillNames = new Dictionary<int, List<string>>();
    private int maxSkillPerTurn = 3; // 兩個玩家共享的總上限

    // 新增：目標選擇相關
    private Unit selectedTarget; // 儲存當前選中的目標
    public Dropdown targetDropdown; // 在 Inspector 中指派，用於多敵人選擇目標

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (playerUnits.Count != 2) Debug.LogError("playerUnits count is not 2, check initialization");
        for (int i = 0; i < playerUnits.Count; i++)
        {
            Debug.Log($"Initializing player {i}: {playerUnits[i]?.unitName}");
            skillQueues[i] = new List<System.Action>();
            selectedSkillNames[i] = new List<string>();
            if (i == 0)
            {
                skillCooldowns["Skill_2_0"] = 0;
                skillCooldowns["Skill_3_0"] = 0;
                skillCooldowns["Skill_4_0"] = 0;
                skillCooldowns["Skill_5_0"] = 0;
            }
            else if (i == 1)
            {
                skillCooldowns["catSkill_2_1"] = 0;
                skillCooldowns["catSkill_3_1"] = 0;
                skillCooldowns["catSkill_4_1"] = 0;
                skillCooldowns["catSkill_5_1"] = 0;
            }
        }
        UpdateTargetSelectionUI();
    }

    // 更新目標選擇 UI，根據敵人數量動態調整
    public void UpdateTargetSelectionUI()
    {
        if (enemyUnits == null || enemyUnits.Count == 0)
        {
            selectedTarget = null;
            Debug.LogWarning("No enemy units available!");
            return;
        }

        if (enemyUnits.Count == 1)
        {
            selectedTarget = enemyUnits[0]; // 單敵人自動選中
            if (targetDropdown != null) targetDropdown.gameObject.SetActive(false); // 隱藏 Dropdown
            Debug.Log($"Auto-selected target: {selectedTarget.unitName}");
        }
        else
        {
            // 多敵人（例如 2 或 3 個），顯示選擇 UI
            selectedTarget = enemyUnits[0]; // 預設第一個
            if (targetDropdown != null)
            {
                targetDropdown.gameObject.SetActive(true);
                targetDropdown.ClearOptions();
                List<string> options = new List<string>();
                for (int i = 0; i < enemyUnits.Count; i++)
                {
                    if (enemyUnits[i].currentHP > 0) // 只顯示存活的敵人
                        options.Add(enemyUnits[i].unitName);
                }
                targetDropdown.AddOptions(options);
                targetDropdown.value = 0; // 預設第一個存活敵人
                targetDropdown.onValueChanged.RemoveAllListeners(); // 避免重複綁定
                targetDropdown.onValueChanged.AddListener(delegate { SelectTarget(targetDropdown.value); });
                Debug.Log($"Default target: {selectedTarget.unitName}, please select from UI");
            }
        }
    }

    // 設置選中目標，綁定到 Dropdown 的 OnValueChanged
    public void SelectTarget(int targetIndex)
    {
        int validIndex = 0;
        int currentIndex = 0;
        for (int i = 0; i < enemyUnits.Count; i++)
        {
            if (enemyUnits[i].currentHP > 0)
            {
                if (currentIndex == targetIndex)
                {
                    validIndex = i;
                    break;
                }
                currentIndex++;
            }
        }
        if (validIndex < enemyUnits.Count && enemyUnits[validIndex].currentHP > 0)
        {
            selectedTarget = enemyUnits[validIndex];
            Debug.Log($"Selected target: {selectedTarget.unitName}");
        }
        else
        {
            Debug.LogWarning("Invalid target index or target is dead!");
        }
    }

    // 技能方法
    public void Skill_1(int playerIndex, Unit target)
    {
        if (playerIndex != 0 || !ValidateSkill(playerIndex, "Skill_1")) return; // 僅限 player0
        selectedSkillNames[playerIndex].Add("Skill_1");
        skillQueues[playerIndex].Add(() =>
        {
            PlaySoundByName("skill1");
            int buffedDamage = Mathf.RoundToInt(playerUnits[playerIndex].damage * 1.2f);
            PerformBasicAttack(playerIndex, target, buffedDamage);
            dialogueText.text = $"{playerUnits[playerIndex].unitName} 甩出手上的球攻擊 {target.unitName}";
        });
        dialogueText.text = $"{playerUnits[playerIndex].unitName} 選擇了技能 1";
        RemoveSkillIcon();
    }

    public void Skill_2(int playerIndex) // 全體攻擊，無需 target
    {
        if (playerIndex != 0 || !ValidateSkill(playerIndex, "Skill_2") || skillCooldowns["Skill_2_" + playerIndex] > 0) return; // 僅限 player0
        selectedSkillNames[playerIndex].Add("Skill_2");
        skillQueues[playerIndex].Add(() =>
        {
            PlaySoundByName("skill2");
            int buffedDamage = Mathf.RoundToInt(playerUnits[playerIndex].damage * 1.5f);
            PerformMultiTargetAttack(playerIndex, buffedDamage);
            skillCooldowns["Skill_2_" + playerIndex] = skill2CooldownTurns;
            dialogueText.text = $"{playerUnits[playerIndex].unitName} 將手上的球分散甩出攻擊全部敵人";
        });
        dialogueText.text = $"{playerUnits[playerIndex].unitName} 選擇了技能 2";
        RemoveSkillIcon();
    }

    public void Skill_3(int playerIndex) // 對自身使用，無需 target
    {
        if (playerIndex != 0 || !ValidateSkill(playerIndex, "Skill_3") || skillCooldowns["Skill_3_" + playerIndex] > 0) return; // 僅限 player0
        selectedSkillNames[playerIndex].Add("Skill_3");
        skillQueues[playerIndex].Add(() =>
        {
            PlaySoundByName("skill3");
            buffDebuffManager.ApplyDefenseBuff(playerUnits[playerIndex], 1.5f, 1); // 對自身應用
            skillCooldowns["Skill_3_" + playerIndex] = skill3CooldownTurns;
            dialogueText.text = $"{playerUnits[playerIndex].unitName} 將身上的毛整理好，使防禦力上升";
        });
        dialogueText.text = $"{playerUnits[playerIndex].unitName} 選擇了技能 3";
        RemoveSkillIcon();
    }

    public void Skill_4(int playerIndex) // 對自身使用，無需 target
    {
        if (playerIndex != 0 || !ValidateSkill(playerIndex, "Skill_4") || skillCooldowns["Skill_4_" + playerIndex] > 0) return; // 僅限 player0
        selectedSkillNames[playerIndex].Add("Skill_4");
        skillQueues[playerIndex].Add(() =>
        {
            PlaySoundByName("Skill_4");
            buffDebuffManager.ApplyDefenseBuff(playerUnits[playerIndex], 2.0f, 2); // 對自身應用
            skillCooldowns["Skill_4_" + playerIndex] = skill4CooldownTurns;
            dialogueText.text = $"{playerUnits[playerIndex].unitName} 緊縮成一團，使敵人找不到縫隙攻擊";
        });
        dialogueText.text = $"{playerUnits[playerIndex].unitName} 選擇了技能 4";
        RemoveSkillIcon();
    }

    public void Skill_5(int playerIndex, Unit target)
    {
        if (playerIndex != 0 || !ValidateSkill(playerIndex, "Skill_5") || skillCooldowns["Skill_5_" + playerIndex] > 0) return; // 僅限 player0
        selectedSkillNames[playerIndex].Add("Skill_5");
        skillQueues[playerIndex].Add(() =>
        {
            PlaySoundByName("skill4-1");
            PlaySoundByName("skill4-2");
            buffDebuffManager.ApplyDefenseDebuff(target, 0.7f, 2);
            int buffedDamage = Mathf.RoundToInt(playerUnits[playerIndex].damage * 1.3f);
            PerformBasicAttack(playerIndex, target, buffedDamage);
            skillCooldowns["Skill_5_" + playerIndex] = skill5CooldownTurns;
            dialogueText.text = $"{playerUnits[playerIndex].unitName} 抱住敵方，使出最強攻擊兔子踹！";
        });
        dialogueText.text = $"{playerUnits[playerIndex].unitName} 選擇了技能 5";
        RemoveSkillIcon();
    }

    public void catSkill_1(int playerIndex, Unit target)
    {
        Debug.Log($"Attempting catSkill_1 for player {playerIndex}, target: {target?.unitName}, target HP: {target?.currentHP}");
        if (playerIndex != 1 || !ValidateSkill(playerIndex, "catSkill_1") || target == null || target.currentHP <= 0) return;
        selectedSkillNames[playerIndex].Add("catSkill_1");
        skillQueues[playerIndex].Add(() =>
        {
            Debug.Log($"Executing catSkill_1 for player {playerIndex}, target: {target.unitName}");
            int buffedDamage = Mathf.RoundToInt(playerUnits[playerIndex].damage * 1f);
            PerformBasicAttack(playerIndex, target, buffedDamage);
            dialogueText.text = $"{playerUnits[playerIndex].unitName} 撿起身邊的東西丟向 {target.unitName}";
        });
        dialogueText.text = $"{playerUnits[playerIndex].unitName} 選擇了技能 1";
        RemoveSkillIcon();
    }

    public void catSkill_2(int playerIndex)
    {
        Debug.Log($"Attempting catSkill_2 for player {playerIndex}");
        if (playerIndex != 1 || !ValidateSkill(playerIndex, "catSkill_2") || skillCooldowns["catSkill_2_" + playerIndex] > 0) return;
        selectedSkillNames[playerIndex].Add("catSkill_2");
        skillQueues[playerIndex].Add(() =>
        {
            Debug.Log($"Executing catSkill_2 for player {playerIndex}");
            buffDebuffManager.Heal(playerUnits[playerIndex], 0.75f);
            skillCooldowns["catSkill_2_" + playerIndex] = catskill2CooldownTurns;
            dialogueText.text = $"{playerUnits[playerIndex].unitName} 將傷口進行包紮";
        });
        dialogueText.text = $"{playerUnits[playerIndex].unitName} 選擇了技能 2";
        RemoveSkillIcon();
    }

    public void catSkill_3(int playerIndex)
    {
        Debug.Log($"Attempting catSkill_3 for player {playerIndex}");
        if (playerIndex != 1 || !ValidateSkill(playerIndex, "catSkill_3") || skillCooldowns["catSkill_3_" + playerIndex] > 0) return;
        selectedSkillNames[playerIndex].Add("catSkill_3");
        skillQueues[playerIndex].Add(() =>
        {
            Debug.Log($"Executing catSkill_3 for player {playerIndex}");
            int buffedDamage = Mathf.RoundToInt(playerUnits[playerIndex].damage * 1f);
            PerformMultiTargetAttack(playerIndex, buffedDamage);
            skillCooldowns["catSkill_3_" + playerIndex] = catskill3CooldownTurns;
            dialogueText.text = $"{playerUnits[playerIndex].unitName} 透過唱歌給全體敵人造成傷害";
        });
        dialogueText.text = $"{playerUnits[playerIndex].unitName} 選擇了技能 3";
        RemoveSkillIcon();
    }

    public void catSkill_4(int playerIndex)
    {
        Debug.Log($"Attempting catSkill_4 for player {playerIndex}");
        if (playerIndex != 1 || !ValidateSkill(playerIndex, "catSkill_4") || skillCooldowns["catSkill_4_" + playerIndex] > 0) return;
        selectedSkillNames[playerIndex].Add("catSkill_4");
        skillQueues[playerIndex].Add(() =>
        {
            Debug.Log($"Executing catSkill_4 for player {playerIndex}");
            buffDebuffManager.ApplyDamageBuff(playerUnits[playerIndex], 2.0f, 2);
            skillCooldowns["catSkill_4_" + playerIndex] = catskill4CooldownTurns;
            dialogueText.text = $"{playerUnits[playerIndex].unitName} 施展魔法給予我方攻擊加成";
        });
        dialogueText.text = $"{playerUnits[playerIndex].unitName} 選擇了技能 4";
        RemoveSkillIcon();
    }

    public void catSkill_5(int playerIndex)
    {
        Debug.Log($"Attempting catSkill_5 for player {playerIndex}");
        if (playerIndex != 1 || !ValidateSkill(playerIndex, "catSkill_5") || skillCooldowns["catSkill_5_" + playerIndex] > 0) return;
        selectedSkillNames[playerIndex].Add("catSkill_5");
        skillQueues[playerIndex].Add(() =>
        {
            Debug.Log($"Executing catSkill_5 for player {playerIndex}");
            foreach (Unit player in playerUnits)
            {
                buffDebuffManager.ApplyFixedDamageReduction(player, 0.7f, 1);
            }
            skillCooldowns["catSkill_5_" + playerIndex] = catskill5CooldownTurns;
            dialogueText.text = $"{playerUnits[playerIndex].unitName} 透過歌聲振奮士氣，使我方全體承受傷害降低";
        });
        dialogueText.text = $"{playerUnits[playerIndex].unitName} 選擇了技能 5";
        RemoveSkillIcon();
    }

    // Trigger 方法（無參數，供 OnClick 綁定）
    public void TriggerSkill1ForPlayer0()
    {
        if (selectedTarget == null)
        {
            if (enemyUnits.Count > 0) selectedTarget = enemyUnits.Find(u => u.currentHP > 0); // 預設第一個存活敵人
            else { Debug.LogWarning("No target available for Player 0!"); return; }
        }
        Skill_1(0, selectedTarget);
    }

    public void TriggerSkill2ForPlayer0()
    {
        Skill_2(0);
    }

    public void TriggerSkill3ForPlayer0()
    {
        Skill_3(0); // 無需目標，直接應用
    }

    public void TriggerSkill4ForPlayer0()
    {
        Skill_4(0); // 無需目標，直接應用
    }

    public void TriggerSkill5ForPlayer0()
    {
        if (selectedTarget == null)
        {
            if (enemyUnits.Count > 0) selectedTarget = enemyUnits.Find(u => u.currentHP > 0);
            else { Debug.LogWarning("No target available for Player 0!"); return; }
        }
        Skill_5(0, selectedTarget);
    }

    public void TriggerCatSkill1ForPlayer1()
    {
        if (selectedTarget == null)
        {
            if (enemyUnits.Count > 0) selectedTarget = enemyUnits.Find(u => u.currentHP > 0);
            else { Debug.LogWarning("No target available for Player 1!"); return; }
        }
        catSkill_1(1, selectedTarget);
    }

    public void TriggerCatSkill2ForPlayer1()
    {
        if (selectedTarget == null)
        {
            if (enemyUnits.Count > 0) selectedTarget = enemyUnits.Find(u => u.currentHP > 0);
            else { Debug.LogWarning("No target available for Player 1!"); return; }
        }
        catSkill_2(1);
    }

    public void TriggerCatSkill3ForPlayer1()
    {
        catSkill_3(1);
    }

    public void TriggerCatSkill4ForPlayer1()
    {
        if (selectedTarget == null)
        {
            if (enemyUnits.Count > 0) selectedTarget = enemyUnits.Find(u => u.currentHP > 0);
            else { Debug.LogWarning("No target available for Player 1!"); return; }
        }
        catSkill_4(1);
    }

    public void TriggerCatSkill5ForPlayer1()
    {
        catSkill_5(1); // 無需目標，對全體應用
    }

    private bool ValidateSkill(int playerIndex, string skillName)
    {
        if (selectedSkillNames[playerIndex].Contains(skillName))
        {
            int index = selectedSkillNames[playerIndex].IndexOf(skillName);
            selectedSkillNames[playerIndex].RemoveAt(index);
            skillQueues[playerIndex].RemoveAt(index);
            dialogueText.text = $"{playerUnits[playerIndex].unitName} 取消了 {skillName}";
            AddSkillIcon();
            return false;
        }
        if (GetTotalSelectedSkills() >= maxSkillPerTurn) return false;
        return true;
    }

    public int GetTotalSelectedSkills()
    {
        int total = 0;
        foreach (var queue in skillQueues.Values)
        {
            total += queue.Count;
        }
        return total;
    }

    public void OnConfirmSkillQueue()
    {
        Debug.Log($"OnConfirmSkillQueue called, Total Skills = {GetTotalSelectedSkills()}");
        bool hasExecuted = false;

        // 遍歷所有玩家 (0 和 1)
        for (int playerIndex = 0; playerIndex < playerUnits.Count; playerIndex++)
        {
            if (skillQueues[playerIndex].Count > 0)
            {
                Debug.Log($"Executing skills for player {playerIndex}, Queue Count = {skillQueues[playerIndex].Count}");
                StartCoroutine(ExecuteSkillQueueWithCallback(playerIndex));
                hasExecuted = true;
            }
        }

        // 如果沒有技能執行，強制推進
        if (!hasExecuted && GetTotalSelectedSkills() == 0)
        {
            Debug.LogWarning("No skills in any queue, skipping to enemy turn");
            if (battlesystem != null) battlesystem.OnPlayerConfirmSkillQueue(0); // 使用 0 作為預設
            return;
        }
    }

    // 新協程：執行後呼叫確認
    IEnumerator ExecuteSkillQueueWithCallback(int playerIndex)
    {
        Debug.Log($"Starting ExecuteSkillQueueWithCallback for player {playerIndex}");
        yield return StartCoroutine(ExecuteSkillQueue(playerIndex));
        Debug.Log($"Finished ExecuteSkillQueueWithCallback for player {playerIndex}, Total Skills = {GetTotalSelectedSkills()}");

        // 檢查所有佇列是否清空
        bool allQueuesEmpty = true;
        for (int i = 0; i < playerUnits.Count; i++)
        {
            if (skillQueues[i].Count > 0)
            {
                allQueuesEmpty = false;
                break;
            }
        }

        if (allQueuesEmpty && battlesystem != null)
        {
            Debug.Log("All skill queues empty, proceeding to enemy turn");
            battlesystem.OnPlayerConfirmSkillQueue(playerIndex); // 使用當前玩家索引
        }
    }

    private void ExecuteSkillAction(System.Action skillAction, int playerIndex)
    {
        try
        {
            if (skillAction == null) Debug.LogError($"Null skill action for player {playerIndex}");
            else skillAction.Invoke();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error executing skill for player {playerIndex}: {e.Message}");
        }
    }

    IEnumerator ExecuteSkillQueue(int playerIndex)
    {
        Debug.Log($"Starting ExecuteSkillQueue for player {playerIndex}, Queue Count = {skillQueues[playerIndex].Count}");
        foreach (var skillAction in new List<System.Action>(skillQueues[playerIndex]))
        {
            ExecuteSkillAction(skillAction, playerIndex);
            yield return new WaitForSeconds(1f);
            Debug.Log($"Completed one action for player {playerIndex}, remaining count = {skillQueues[playerIndex].Count}");
        }
        Debug.Log($"Finished ExecuteSkillQueue for player {playerIndex}, Clearing queue");
        skillQueues[playerIndex].Clear();
        selectedSkillNames[playerIndex].Clear();
    }

    void PerformBasicAttack(int playerIndex, Unit target, int damageOverride = -1)
    {
        if (target == null || target.currentHP <= 0)
        {
            Debug.LogWarning($"Invalid target for player {playerIndex}");
            return;
        }
        int actualDamage = damageOverride > 0 ? damageOverride : playerUnits[playerIndex].damage;
        bool isDead = target.TakeDamage(actualDamage);
        int targetIndex = enemyUnits.IndexOf(target);
        if (targetIndex >= 0 && targetIndex < enemyHUDs.Count)
        {
            enemyHUDs[targetIndex].SetHP(target.currentHP);
        }
        if (isDead)
        {
            UpdateTargetSelectionUI();
            if (AllEnemiesDead())
            {
                battlesystem.state = BattleState.WON;
                battlesystem.EndBattle();
            }
        }
    }

    void PerformMultiTargetAttack(int playerIndex, int damage)
    {
        foreach (Unit enemy in enemyUnits)
        {
            if (enemy.currentHP > 0)
            {
                bool isDead = enemy.TakeDamage(damage);
                int targetIndex = enemyUnits.IndexOf(enemy);
                if (targetIndex >= 0 && targetIndex < enemyHUDs.Count)
                {
                    enemyHUDs[targetIndex].SetHP(enemy.currentHP);
                }
                if (isDead)
                {
                    UpdateTargetSelectionUI(); // 敵人死亡後更新
                    if (AllEnemiesDead())
                    {
                        battlesystem.state = BattleState.WON;
                        battlesystem.EndBattle();
                    }
                }
            }
        }
    }

    private bool AllEnemiesDead()
    {
        foreach (Unit enemy in enemyUnits)
        {
            if (enemy.currentHP > 0) return false;
        }
        return true;
    }

    void RemoveSkillIcon()
    {
        if (skillIcons.Count > 0)
        {
            GameObject lastIcon = skillIcons[skillIcons.Count - 1];
            skillIcons.RemoveAt(skillIcons.Count - 1);
            Destroy(lastIcon);
        }
    }

    void AddSkillIcon()
    {
        if (GetTotalSelectedSkills() < maxSkillPerTurn)
        {
            GameObject icon = Instantiate(skillIconPrefab, skillIconParent);
            skillIcons.Add(icon);
        }
    }

    public void ReduceCooldown()
    {
        foreach (var key in new List<string>(skillCooldowns.Keys))
        {
            if (skillCooldowns[key] > 0) skillCooldowns[key]--;
        }
    }

    void ResetSkillIcons()
    {
        foreach (GameObject icon in skillIcons)
        {
            Destroy(icon);
        }
        skillIcons.Clear();

        // 初始化為 maxSkillPerTurn（共享）
        for (int i = 0; i < maxSkillPerTurn; i++)
        {
            GameObject icon = Instantiate(skillIconPrefab, skillIconParent);
            skillIcons.Add(icon);
        }
    }

    public void InitSkillIcons()
    {
        ResetSkillIcons();
    }

    public IEnumerator DelayedInitSkillIcons(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        InitSkillIcons();
    }

    public void ClearSkillIcons()
    {
        foreach (GameObject icon in skillIcons)
        {
            Destroy(icon);
        }
        skillIcons.Clear();
    }

    public void PlaySoundByName(string soundName)
    {
        AudioClip clip = Resources.Load<AudioClip>("soundeffects/" + soundName);
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
        else
        {
            Debug.LogWarning("音效找不到: " + soundName);
        }
    }
}