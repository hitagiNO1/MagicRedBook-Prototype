using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class SkillManager : MonoBehaviour
{
    public List<Unit> playerUnits;
    public List<Unit> enemyUnits;
    public TextMeshProUGUI dialogueText;
    public List<BattleHUD> enemyHUDs;
    public BattleSystem battlesystem;
    public BuffDebuffManager buffDebuffManager;
    public Itemuse itemuse;

    public LayerMask enemyLayer;
    public LayerMask playerLayer;

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
    public Transform skillIconParent;
    public List<GameObject> skillIcons = new List<GameObject>();
    public Dictionary<int, List<System.Action>> skillQueues = new Dictionary<int, List<System.Action>>();
    public Dictionary<int, List<string>> selectedSkillNames = new Dictionary<int, List<string>>();
    public int maxSkillPerTurn = 3;

    public Dictionary<int, Unit> selectedTargets = new Dictionary<int, Unit>();
    public bool isWaitingForTarget = false;
    public string pendingSkill = null;
    public int pendingPlayerIndex = -1;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (playerUnits.Count != 2) Debug.LogError("playerUnits count is not 2, check initialization");
        for (int i = 0; i < playerUnits.Count; i++)
        {
            Debug.Log($"Initializing player {i}: {playerUnits[i]?.unitName}");
            skillQueues[i] = new List<System.Action>();
            selectedSkillNames[i] = new List<string>();
            selectedTargets[i] = null;
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
    }

    void Update()
    {
        if (isWaitingForTarget && Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current.IsPointerOverGameObject())
            {
                Debug.Log("Click detected on UI, ignoring raycast for non-UI targets");
                return;
            }

            if (pendingSkill == "catSkill_4")
            {
                Debug.Log("Ignoring raycast for catSkill_4, please click a player portrait via UI");
                dialogueText.text = "請點擊一個玩家頭像以使用貓技能 4！";
                return;
            }

            Debug.Log("Left mouse button clicked, processing raycast for target selection");
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Unit targetUnit = null;

            bool isPlayerTarget = pendingSkill == "Can" || pendingSkill == "Grass" || pendingSkill == "Drying";
            LayerMask targetLayer = isPlayerTarget ? playerLayer : enemyLayer;

            RaycastHit2D hit2D = Physics2D.Raycast(ray.origin, ray.direction, Mathf.Infinity, targetLayer);
            if (hit2D.collider != null)
            {
                targetUnit = hit2D.collider.GetComponent<Unit>();
                Debug.Log($"2D Raycast hit: {hit2D.collider.name}, Unit: {targetUnit?.unitName}");
            }
            else
            {
                RaycastHit hit3D;
                if (Physics.Raycast(ray, out hit3D, Mathf.Infinity, targetLayer))
                {
                    targetUnit = hit3D.collider.GetComponent<Unit>();
                    Debug.Log($"3D Raycast hit: {hit3D.collider.name}, Unit: {targetUnit?.unitName}");
                }
            }

            if (targetUnit != null && (enemyUnits.Contains(targetUnit) || playerUnits.Contains(targetUnit)) && targetUnit.currentHP > 0)
            {
                if (isPlayerTarget && !playerUnits.Contains(targetUnit))
                {
                    dialogueText.text = "請點擊一個玩家頭像！";
                    Debug.LogWarning("Player target item can only target player units!");
                    return;
                }
                if (!isPlayerTarget && !enemyUnits.Contains(targetUnit))
                {
                    dialogueText.text = "請點擊一個敵人目標！";
                    Debug.LogWarning("Skill can only target enemy units!");
                    return;
                }

                Debug.Log($"Valid target selected: {targetUnit.unitName} for skill {pendingSkill}");
                SelectTarget(pendingPlayerIndex, targetUnit);
                ExecutePendingSkill();
            }
            else
            {
                string targetType = isPlayerTarget ? "玩家頭像" : "敵人目標";
                dialogueText.text = $"請點擊一個有效的{targetType}！";
                Debug.LogWarning("Invalid or dead target clicked!");
            }
        }

        if (isWaitingForTarget && Input.GetMouseButtonDown(1))
        {
            isWaitingForTarget = false;
            pendingSkill = null;
            pendingPlayerIndex = -1;
            dialogueText.text = "已取消技能選擇";
            Debug.Log("Skill selection cancelled by right-click");
        }
    }

    public void SelectTarget(int playerIndex, Unit target)
    {
        Debug.Log($"SelectTarget called for playerIndex: {playerIndex}, target: {target?.unitName}, caller: {new System.Diagnostics.StackTrace().ToString()}");
        if (target != null && (enemyUnits.Contains(target) || playerUnits.Contains(target)) && target.currentHP > 0)
        {
            selectedTargets[playerIndex] = target;
            dialogueText.text = $"{playerUnits[playerIndex].unitName} 已選擇目標：{target.unitName}";
            Debug.Log($"Player {playerIndex} selected target: {target.unitName}");

            HighlightTarget(target, true);

            foreach (var unit in enemyUnits)
            {
                if (unit != target && unit.currentHP > 0)
                {
                    HighlightTarget(unit, false);
                }
            }
            foreach (var unit in playerUnits)
            {
                if (unit != target && unit.currentHP > 0)
                {
                    HighlightTarget(unit, false);
                }
            }
        }
        else
        {
            selectedTargets[playerIndex] = null;
            dialogueText.text = $"{playerUnits[playerIndex].unitName}：無效的目標，請點擊一個存活的目標！";
            Debug.LogWarning($"Player {playerIndex}: Invalid or dead target!");
        }
    }

    public void HighlightTarget(Unit target, bool isHighlighted)
    {
        SpriteRenderer renderer = target.GetComponent<SpriteRenderer>();
        Image image = target.GetComponent<Image>();
        if (renderer != null)
        {
            renderer.color = isHighlighted ? Color.yellow : Color.white;
        }
        else if (image != null)
        {
            image.color = isHighlighted ? Color.yellow : Color.white;
        }
    }

    public void ClearAllHighlights()
    {
        List<int> keysToClear = new List<int>(selectedTargets.Keys);
        foreach (int playerIndex in keysToClear)
        {
            if (selectedTargets.ContainsKey(playerIndex) && selectedTargets[playerIndex] != null)
            {
                Unit target = selectedTargets[playerIndex];
                HighlightTarget(target, false);
                selectedTargets[playerIndex] = null;
                Debug.Log($"Cleared highlight from player {playerIndex}'s target: {target?.unitName}");
            }
        }
        selectedTargets.Clear();
        if (dialogueText != null)
        {
            dialogueText.text = "";
        }
        Debug.Log("All highlights cleared successfully");
    }

    public void OnPlayerHeadClick(int playerIndex)
    {
        Debug.Log($"OnPlayerHeadClick called for playerIndex: {playerIndex}, pendingSkill: {pendingSkill}, isWaitingForTarget: {isWaitingForTarget}, caller: {new System.Diagnostics.StackTrace().ToString()}");

        // 檢查 playerUnits 是否有效
        if (playerUnits == null || playerIndex < 0 || playerIndex >= playerUnits.Count)
        {
            Debug.LogError($"Invalid playerUnits or playerIndex: {playerIndex}, playerUnits count: {playerUnits?.Count}");
            dialogueText.text = "錯誤：無效的玩家索引！";
            return;
        }

        // 檢查狀態是否正確
        if (!isWaitingForTarget || pendingSkill != "catSkill_4" || pendingPlayerIndex != 1)
        {
            Debug.LogWarning($"OnPlayerHeadClick ignored: Invalid state (isWaitingForTarget: {isWaitingForTarget}, pendingSkill: {pendingSkill}, pendingPlayerIndex: {pendingPlayerIndex})");
            return;
        }

        Unit target = playerUnits[playerIndex];
        if (target != null && target.currentHP > 0)
        {
            Debug.Log($"Player head click: Selecting target {target.unitName} for catSkill_4");
            SelectTarget(pendingPlayerIndex, target);
            ExecutePendingSkill();
        }
        else
        {
            dialogueText.text = "請選擇一個存活的玩家目標！";
            Debug.LogWarning($"Invalid player target: {playerIndex}, target: {(target != null ? target.unitName : "null")}, HP: {(target != null ? target.currentHP : -1)}");
        }
    }

    public void CancelSkill(int playerIndex, string skillName)
    {
        if (selectedSkillNames[playerIndex].Contains(skillName))
        {
            int index = selectedSkillNames[playerIndex].IndexOf(skillName);
            selectedSkillNames[playerIndex].RemoveAt(index);
            skillQueues[playerIndex].RemoveAt(index);
            if (selectedTargets[playerIndex] != null)
            {
                HighlightTarget(selectedTargets[playerIndex], false);
                selectedTargets[playerIndex] = null;
            }
            dialogueText.text = $"{playerUnits[playerIndex].unitName} 取消了 {skillName}";
            AddSkillIcon();
            Debug.Log($"Player {playerIndex} cancelled skill: {skillName}");
        }
    }

    public bool ValidateTarget(int playerIndex, Unit target)
    {
        if (target == null || target.currentHP <= 0)
        {
            dialogueText.text = $"{playerUnits[playerIndex].unitName}：請點擊一個有效的目標！";
            Debug.LogWarning($"Player {playerIndex}: Invalid or dead target!");
            return false;
        }
        return true;
    }

    private void ExecutePendingSkill()
    {
        if (!isWaitingForTarget || pendingSkill == null || pendingPlayerIndex < 0 || !selectedTargets.ContainsKey(pendingPlayerIndex)) return;

        Unit target = selectedTargets[pendingPlayerIndex];
        if (!ValidateTarget(pendingPlayerIndex, target)) return;

        switch (pendingSkill)
        {
            case "Skill_1":
                Skill_1(pendingPlayerIndex, target);
                break;
            case "Skill_5":
                Skill_5(pendingPlayerIndex, target);
                break;
            case "catSkill_1":
                catSkill_1(pendingPlayerIndex, target);
                break;
            case "catSkill_4":
                catSkill_4(pendingPlayerIndex, target);
                break;
            case "Can":
                itemuse.UseCan(pendingPlayerIndex, target);
                break;
            case "Grass":
                itemuse.UseGrass(pendingPlayerIndex, target);
                break;
            case "Drying":
                itemuse.UseDrying(pendingPlayerIndex, target);
                break;
            default:
                Debug.LogError($"Unknown pending skill/item: {pendingSkill}");
                break;
        }

        isWaitingForTarget = false;
        pendingSkill = null;
        pendingPlayerIndex = -1;
    }

    public void Skill_1(int playerIndex, Unit target)
    {
        if (playerIndex != 0 || !ValidateSkill(playerIndex, "Skill_1") || !ValidateTarget(playerIndex, target)) return;
        selectedSkillNames[playerIndex].Add("Skill_1");
        skillQueues[playerIndex].Add(() =>
        {
            if (target.currentHP > 0)
            {
                PlaySoundByName("skill1");
                int buffedDamage = Mathf.RoundToInt(playerUnits[playerIndex].damage * 1.2f);
                PerformBasicAttack(playerIndex, target, buffedDamage);
                dialogueText.text = $"{playerUnits[playerIndex].unitName} 甩出手上的球攻擊 {target.unitName}";
            }
        });
        dialogueText.text = $"{playerUnits[playerIndex].unitName} 選擇了技能 1 對 {target.unitName}";
        RemoveSkillIcon();
    }

    public void Skill_2(int playerIndex)
    {
        if (playerIndex != 0 || !ValidateSkill(playerIndex, "Skill_2") || skillCooldowns["Skill_2_" + playerIndex] > 0) return;
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

    public void Skill_3(int playerIndex)
    {
        if (playerIndex != 0 || !ValidateSkill(playerIndex, "Skill_3") || skillCooldowns["Skill_3_" + playerIndex] > 0) return;
        selectedSkillNames[playerIndex].Add("Skill_3");
        skillQueues[playerIndex].Add(() =>
        {
            PlaySoundByName("skill3");
            buffDebuffManager.ApplyDefenseBuff(playerUnits[playerIndex], 1.5f, 1);
            skillCooldowns["Skill_3_" + playerIndex] = skill3CooldownTurns;
            dialogueText.text = $"{playerUnits[playerIndex].unitName} 將身上的毛整理好，使防禦力上升";
        });
        dialogueText.text = $"{playerUnits[playerIndex].unitName} 選擇了技能 3";
        RemoveSkillIcon();
    }

    public void Skill_4(int playerIndex)
    {
        if (playerIndex != 0 || !ValidateSkill(playerIndex, "Skill_4") || skillCooldowns["Skill_4_" + playerIndex] > 0) return;
        selectedSkillNames[playerIndex].Add("Skill_4");
        skillQueues[playerIndex].Add(() =>
        {
            PlaySoundByName("Skill_4");
            buffDebuffManager.ApplyDefenseBuff(playerUnits[playerIndex], 2.0f, 2);
            skillCooldowns["Skill_4_" + playerIndex] = skill4CooldownTurns;
            dialogueText.text = $"{playerUnits[playerIndex].unitName} 緊縮成一團，使敵人找不到縫隙攻擊";
        });
        dialogueText.text = $"{playerUnits[playerIndex].unitName} 選擇了技能 4";
        RemoveSkillIcon();
    }

    public void Skill_5(int playerIndex, Unit target)
    {
        if (playerIndex != 0 || !ValidateSkill(playerIndex, "Skill_5") || skillCooldowns["Skill_5_" + playerIndex] > 0 || !ValidateTarget(playerIndex, target)) return;
        selectedSkillNames[playerIndex].Add("Skill_5");
        skillQueues[playerIndex].Add(() =>
        {
            if (target.currentHP > 0)
            {
                PlaySoundByName("skill4-1");
                PlaySoundByName("skill4-2");
                buffDebuffManager.ApplyDefenseDebuff(target, 0.7f, 2);
                int buffedDamage = Mathf.RoundToInt(playerUnits[playerIndex].damage * 1.3f);
                PerformBasicAttack(playerIndex, target, buffedDamage);
                dialogueText.text = $"{playerUnits[playerIndex].unitName} 抱住敵方，使出最強攻擊兔子踹！";
            }
        });
        dialogueText.text = $"{playerUnits[playerIndex].unitName} 選擇了技能 5 對 {target.unitName}";
        RemoveSkillIcon();
    }

    public void catSkill_1(int playerIndex, Unit target)
    {
        if (playerIndex != 1 || !ValidateSkill(playerIndex, "catSkill_1") || !ValidateTarget(playerIndex, target)) return;
        selectedSkillNames[playerIndex].Add("catSkill_1");
        skillQueues[playerIndex].Add(() =>
        {
            if (target.currentHP > 0)
            {
                Debug.Log($"Executing catSkill_1 for player {playerIndex}, target: {target.unitName}");
                int buffedDamage = Mathf.RoundToInt(playerUnits[playerIndex].damage * 1f);
                PerformBasicAttack(playerIndex, target, buffedDamage);
                dialogueText.text = $"{playerUnits[playerIndex].unitName} 撿起身邊的東西丟向 {target.unitName}";
            }
        });
        dialogueText.text = $"{playerUnits[playerIndex].unitName} 選擇了技能 1 對 {target.unitName}";
        RemoveSkillIcon();
    }

    public void catSkill_2(int playerIndex)
    {
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

    public void catSkill_4(int playerIndex, Unit target)
    {
        if (playerIndex != 1 || !ValidateSkill(playerIndex, "catSkill_4") || skillCooldowns["catSkill_4_" + playerIndex] > 0 || !ValidateTarget(playerIndex, target) || !playerUnits.Contains(target)) return;
        selectedSkillNames[playerIndex].Add("catSkill_4");
        skillQueues[playerIndex].Add(() =>
        {
            if (target.currentHP > 0)
            {
                Debug.Log($"Executing catSkill_4 for player {playerIndex}, target: {target.unitName}");
                buffDebuffManager.ApplyDamageBuff(target, 2.0f, 2);
                skillCooldowns["catSkill_4_" + playerIndex] = catskill4CooldownTurns;
                dialogueText.text = $"{playerUnits[playerIndex].unitName} 對 {target.unitName} 施展魔法，提升其攻擊力";
            }
        });
        dialogueText.text = $"{playerUnits[playerIndex].unitName} 選擇了技能 4 對 {target.unitName}";
        RemoveSkillIcon();
    }

    public void catSkill_5(int playerIndex)
    {
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

    public void TriggerSkill1ForPlayer0()
    {
        if (battlesystem.state != BattleState.PLAYERTURN) return;
        if (selectedSkillNames[0].Contains("Skill_1"))
        {
            CancelSkill(0, "Skill_1");
        }
        else
        {
            isWaitingForTarget = true;
            pendingSkill = "Skill_1";
            pendingPlayerIndex = 0;
            dialogueText.text = "請點擊一個敵人目標以使用技能 1！";
        }
    }

    public void TriggerSkill2ForPlayer0()
    {
        if (battlesystem.state != BattleState.PLAYERTURN) return;
        Skill_2(0);
    }

    public void TriggerSkill3ForPlayer0()
    {
        if (battlesystem.state != BattleState.PLAYERTURN) return;
        Skill_3(0);
    }

    public void TriggerSkill4ForPlayer0()
    {
        if (battlesystem.state != BattleState.PLAYERTURN) return;
        Skill_4(0);
    }

    public void TriggerSkill5ForPlayer0()
    {
        if (battlesystem.state != BattleState.PLAYERTURN) return;
        if (selectedSkillNames[0].Contains("Skill_5"))
        {
            CancelSkill(0, "Skill_5");
        }
        else
        {
            isWaitingForTarget = true;
            pendingSkill = "Skill_5";
            pendingPlayerIndex = 0;
            dialogueText.text = "請點擊一個敵人目標以使用技能 5！";
        }
    }

    public void TriggerCatSkill1ForPlayer1()
    {
        if (battlesystem.state != BattleState.PLAYERTURN) return;
        if (selectedSkillNames[1].Contains("catSkill_1"))
        {
            CancelSkill(1, "catSkill_1");
        }
        else
        {
            isWaitingForTarget = true;
            pendingSkill = "catSkill_1";
            pendingPlayerIndex = 1;
            dialogueText.text = "請點擊一個敵人目標以使用貓技能 1！";
        }
    }

    public void TriggerCatSkill2ForPlayer1()
    {
        if (battlesystem.state != BattleState.PLAYERTURN) return;
        catSkill_2(1);
    }

    public void TriggerCatSkill3ForPlayer1()
    {
        if (battlesystem.state != BattleState.PLAYERTURN) return;
        catSkill_3(1);
    }

    public void TriggerCatSkill4ForPlayer1()
    {
        if (battlesystem.state != BattleState.PLAYERTURN)
        {
            Debug.LogWarning("Cannot trigger catSkill_4: Not in player turn");
            return;
        }
        if (selectedSkillNames[1].Contains("catSkill_4"))
        {
            CancelSkill(1, "catSkill_4");
        }
        else
        {
            isWaitingForTarget = true;
            pendingSkill = "catSkill_4";
            pendingPlayerIndex = 1;
            dialogueText.text = "請點擊一個玩家頭像以使用貓技能 4！";
            Debug.Log("Waiting for player to select a target for catSkill_4");
        }
    }

    public void TriggerCatSkill5ForPlayer1()
    {
        if (battlesystem.state != BattleState.PLAYERTURN) return;
        catSkill_5(1);
    }

    private bool ValidateSkill(int playerIndex, string skillName)
    {
        if (selectedSkillNames[playerIndex].Contains(skillName))
        {
            CancelSkill(playerIndex, skillName);
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
        if (isWaitingForTarget)
        {
            dialogueText.text = "請先點擊一個目標完成技能選擇！";
            return;
        }

        Debug.Log($"OnConfirmSkillQueue called, Total Skills = {GetTotalSelectedSkills()}");
        bool hasExecuted = false;

        for (int playerIndex = 0; playerIndex < playerUnits.Count; playerIndex++)
        {
            if (skillQueues[playerIndex].Count > 0)
            {
                Debug.Log($"Executing skills for player {playerIndex}, Queue Count = {skillQueues[playerIndex].Count}");
                StartCoroutine(ExecuteSkillQueueWithCallback(playerIndex));
                hasExecuted = true;
            }
        }

        if (!hasExecuted && GetTotalSelectedSkills() == 0)
        {
            Debug.LogWarning("No skills in any queue, skipping to enemy turn");
            if (battlesystem != null) battlesystem.OnPlayerConfirmSkillQueue(0);
            return;
        }
    }

    IEnumerator ExecuteSkillQueueWithCallback(int playerIndex)
    {
        Debug.Log($"Starting ExecuteSkillQueueWithCallback for player {playerIndex}");
        yield return StartCoroutine(ExecuteSkillQueue(playerIndex));
        Debug.Log($"Finished ExecuteSkillQueueWithCallback for player {playerIndex}, Total Skills = {GetTotalSelectedSkills()}");

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
            battlesystem.OnPlayerConfirmSkillQueue(playerIndex);
        }
    }

    private void ExecuteSkillAction(System.Action skillAction, int playerIndex)
    {
        if (skillAction == null)
        {
            Debug.LogError($"Null skill action for player {playerIndex}");
            return;
        }
        try
        {
            skillAction.Invoke();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error executing skill for player {playerIndex}: {e.Message}");
            if (skillQueues.ContainsKey(playerIndex))
            {
                skillQueues[playerIndex].Clear();
                selectedSkillNames[playerIndex].Clear();
                Debug.LogWarning($"Cleared invalid skill queue for player {playerIndex} due to error.");
            }
        }
    }

    IEnumerator ExecuteSkillQueue(int playerIndex)
    {
        Debug.Log($"Starting ExecuteSkillQueue for player {playerIndex}, Queue Count = {skillQueues[playerIndex].Count}");
        var actions = new List<System.Action>(skillQueues[playerIndex]);
        foreach (var skillAction in actions)
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
        if (target == null || target.currentHP <= 0 || !enemyUnits.Contains(target))
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
            if (selectedTargets.ContainsKey(playerIndex) && selectedTargets[playerIndex] == target)
            {
                selectedTargets[playerIndex] = null;
            }
            HighlightTarget(target, false);
            if (battlesystem != null)
            {
                battlesystem.DestroyEnemy(targetIndex);
                CleanSkillQueue(playerIndex, target);
            }
            if (AllEnemiesDead())
            {
                battlesystem.state = BattleState.WON;
                battlesystem.EndBattle();
            }
        }
    }

    void PerformMultiTargetAttack(int playerIndex, int damage)
    {
        bool anyDead = false;
        foreach (Unit enemy in enemyUnits.ToArray())
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
                    anyDead = true;
                    if (selectedTargets.ContainsKey(playerIndex) && selectedTargets[playerIndex] == enemy)
                    {
                        selectedTargets[playerIndex] = null;
                    }
                    HighlightTarget(enemy, false);
                    if (battlesystem != null)
                    {
                        battlesystem.DestroyEnemy(targetIndex);
                        CleanSkillQueue(playerIndex, enemy);
                    }
                }
            }
        }
        if (AllEnemiesDead())
        {
            battlesystem.state = BattleState.WON;
            battlesystem.EndBattle();
        }
    }

    private void CleanSkillQueue(int playerIndex, Unit deadEnemy)
    {
        if (skillQueues.ContainsKey(playerIndex))
        {
            var newActions = new List<System.Action>();
            var newSkillNames = new List<string>();
            for (int i = 0; i < skillQueues[playerIndex].Count; i++)
            {
                if (!IsActionTargeting(deadEnemy, skillQueues[playerIndex][i]))
                {
                    newActions.Add(skillQueues[playerIndex][i]);
                    newSkillNames.Add(selectedSkillNames[playerIndex][i]);
                }
            }
            skillQueues[playerIndex] = newActions;
            selectedSkillNames[playerIndex] = newSkillNames;
            Debug.Log($"Cleaned skill queue for player {playerIndex} after enemy {deadEnemy.unitName} died.");
        }
    }

    private bool IsActionTargeting(Unit target, System.Action action)
    {
        return false;
    }

    private bool AllEnemiesDead()
    {
        foreach (Unit enemy in enemyUnits)
        {
            if (enemy.currentHP > 0) return false;
        }
        return true;
    }

    public void RemoveSkillIcon()
    {
        if (skillIcons.Count > 0)
        {
            GameObject lastIcon = skillIcons[skillIcons.Count - 1];
            skillIcons.RemoveAt(skillIcons.Count - 1);
            Destroy(lastIcon);
        }
    }

    public void AddSkillIcon()
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

    public void OnCanButtonClick()
    {
        itemuse.TriggerCanForPlayer(0);
    }

    public void OnTunaButtonClick()
    {
        itemuse.TriggerTunaForPlayer(0);
    }

    public void OnGrassButtonClick()
    {
        itemuse.TriggerGrassForPlayer(0);
    }

    public void OnDryingButtonClick()
    {
        itemuse.TriggerDryingForPlayer(0);
    }
}