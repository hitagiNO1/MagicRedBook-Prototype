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
    private int skill5CooldownTurns = 3;
    private int catskill2CooldownTurns = 3;
    private int catskill3CooldownTurns = 2;
    private int catskill4CooldownTurns = 3;
    private int catskill5CooldownTurns = 4;

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
            if (!selectedTargets.ContainsKey(i)) // 確保鍵存在
            {
                selectedTargets[i] = null;
            }
            if (i == 0)
            {
                skillCooldowns["Skill_1_0"] = 0;
                skillCooldowns["Skill_2_0"] = 0;
                skillCooldowns["Skill_3_0"] = 0;
                skillCooldowns["Skill_4_0"] = 0;
                skillCooldowns["Skill_5_0"] = 0;
            }
            else if (i == 1)
            {
                skillCooldowns["catSkill_1_1"] = 0;
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

            //RaycastHit2D hit2D = Physics2D.Raycast(ray.origin, ray.direction, Mathf.Infinity, targetLayer);
            RaycastHit2D hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity, LayerMask.GetMask("Enemy"));
            if (hit.collider != null)
            {
                targetUnit = hit.collider.GetComponent<Unit>();
                Debug.Log($"6666666666666666666666 hit: {hit.collider}, Unit: {targetUnit?.unitName}");
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

    /* public void OnPlayerHeadClick(int playerIndex)
     {
         Debug.Log($"OnPlayerHeadClick called for playerIndex: {playerIndex}, pendingSkill: {pendingSkill}, isWaitingForTarget: {isWaitingForTarget}, caller: {new System.Diagnostics.StackTrace().ToString()}");

         // 檢查 playerUnits 是否有效
         if (playerUnits == null || playerIndex < 0 || playerIndex >= playerUnits.Count)
         {
             Debug.LogError($"Invalid playerUnits or playerIndex: {playerIndex}, playerUnits count: {playerUnits?.Count}");
            // dialogueText.text = "錯誤：無效的玩家索引！";
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
     }*/

    public void OnPlayerHeadClick(int playerIndex)
    {
        Debug.Log($"OnPlayerHeadClick called for playerIndex: {playerIndex}, pendingSkill: {pendingSkill}, pendingPlayerIndex: {pendingPlayerIndex}");

        if (playerUnits == null || playerIndex < 0 || playerIndex >= playerUnits.Count)
        {
            Debug.LogError($"Invalid playerUnits or playerIndex: {playerIndex}");
            return;
        }

        Unit selectedTarget = playerUnits[playerIndex];

        if (selectedTarget == null || selectedTarget.currentHP <= 0)
        {
            dialogueText.text = "請選擇一個存活的玩家目標！";
            Debug.LogWarning("選擇的目標已死亡或無效");
            return;
        }

        // 檢查是否正在等待「選擇玩家目標」的技能/道具
        // 支援 catSkill_4 和所有背包道具
        bool isWaitingForPlayerTarget =
            pendingSkill == "catSkill_4" ||
            pendingSkill == "Can" ||
            pendingSkill == "Grass" ||
            pendingSkill == "Drying" ||
            pendingSkill == "Can"; // 如果你還有其他

        // 必須有 pendingSkill，且目前回合玩家要匹配（避免亂點）
        if (!isWaitingForPlayerTarget || pendingPlayerIndex < 0 || pendingPlayerIndex != GetCurrentPlayerIndex())
        {
            // 如果不在等待狀態，就不處理（避免誤點觸發）
            Debug.Log("目前沒有等待選擇玩家目標的技能或道具，或不是正確的玩家回合");
            return;
        }

        // 成功選擇目標
        dialogueText.text = $"{playerUnits[pendingPlayerIndex].unitName} 選擇對 {selectedTarget.unitName} 使用 {pendingSkill}";
        SelectTarget(pendingPlayerIndex, selectedTarget);
        ExecutePendingSkill();

        // 清除等待狀態
        pendingSkill = null;
        pendingPlayerIndex = -1;
    }

    // 建議加這個輔助方法，取得目前回合玩家
    private int GetCurrentPlayerIndex()
    {
        if (battlesystem != null)
        {
            return battlesystem.currentPlayerIndex; // 請確保 BattleSystem 有 public int currentPlayerIndex
        }
        return 0; // fallback
    }

    public void CancelSkill(int playerIndex, string skillName)
    {
        if (selectedSkillNames[playerIndex].Contains(skillName))
        {
            int index = selectedSkillNames[playerIndex].IndexOf(skillName);
            selectedSkillNames[playerIndex].RemoveAt(index);
            skillQueues[playerIndex].RemoveAt(index);
            if (selectedTargets.ContainsKey(playerIndex) && selectedTargets[playerIndex] != null)
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

    public bool ValidateSkill(int playerIndex, string skillName)
    {
        if (selectedSkillNames[playerIndex].Contains(skillName))
        {
            CancelSkill(playerIndex, skillName);
            return false;
        }
        if (GetTotalSelectedSkills() >= maxSkillPerTurn)
        {
            dialogueText.text = "已達技能數量上限！";
            return false;
        }
        string cooldownKey = $"{skillName}_{playerIndex}";
        if (!skillCooldowns.ContainsKey(cooldownKey))
        {
            Debug.LogWarning($"冷卻鍵 {cooldownKey} 不存在，初始化為 0");
            skillCooldowns[cooldownKey] = 0;
        }
        if (skillCooldowns[cooldownKey] > 0)
        {
            dialogueText.text = $"技能 {skillName} 正在冷卻中，剩餘 {skillCooldowns[cooldownKey]} 回合！";
            Debug.Log($"技能 {skillName} 冷卻中，剩餘 {skillCooldowns[cooldownKey]} 回合");
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
            if (target == null || target.currentHP <= 0 || !enemyUnits.Contains(target))
            {
                Debug.LogWarning($"Target {target?.unitName} is invalid or dead, skipping Skill_1 for player {playerIndex}");
                return;
            }

            PlaySoundByName("skill1");
            int buffedDamage = Mathf.RoundToInt(playerUnits[playerIndex].damage * 1.2f);
            PerformBasicAttack(playerIndex, target, buffedDamage);
            dialogueText.text = $"{playerUnits[playerIndex].unitName} 甩出手上的球攻擊 {target.unitName}";
            EffectManager.Instance.PlayUIEffect("B1", new Vector2(0, 0));

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
            EffectManager.Instance.PlayUIEffect("B2", new Vector2(0, 0));
            EffectManager.Instance.PlayUIEffect("B1", new Vector2(0, 0), 0.22f, 0f);
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
            EffectManager.Instance.PlayUIEffect("B3", new Vector2(0, 0));
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
            EffectManager.Instance.PlayUIEffect("B4", new Vector2(0, 0));
        });
        dialogueText.text = $"{playerUnits[playerIndex].unitName} 選擇了技能 4";
        RemoveSkillIcon();
    }

    public void Skill_5(int playerIndex, Unit target)
    {
        selectedSkillNames[playerIndex].Add("Skill_5");
        skillQueues[playerIndex].Add(() =>
        {
            if (playerUnits[playerIndex] == null || playerUnits[playerIndex].currentHP <= 0)
            {
                Debug.LogWarning($"玩家 {playerIndex} 無效或已死亡，跳過 Skill_5");
                return;
            }
            if (target == null || target.currentHP <= 0 || !enemyUnits.Contains(target))
            {
                Debug.LogWarning($"Target {target?.unitName} is invalid or dead, skipping Skill_5 for player {playerIndex}");
                skillCooldowns["Skill_5_" + playerIndex] = skill5CooldownTurns; // 即使目標無效，仍設置冷卻
                return;
            }
            PlaySoundByName("skill4-1");
            PlaySoundByName("skill4-2");
            buffDebuffManager.ApplyDefenseDebuff(target, 0.7f, 2);
            int buffedDamage = Mathf.RoundToInt(playerUnits[playerIndex].damage * 1.3f);
            PerformBasicAttack(playerIndex, target, buffedDamage);
            skillCooldowns["Skill_5_" + playerIndex] = skill5CooldownTurns; // 設置冷卻
            Debug.Log($"玩家 {playerIndex} 的 Skill_5 設置冷卻為 {skill5CooldownTurns} 回合");
            dialogueText.text = $"{playerUnits[playerIndex].unitName} 抱住敵方，使出最強攻擊兔子踹！";
            EffectManager.Instance.PlayUIEffect("B5", new Vector2(0, 0));
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
            if (target == null || target.currentHP <= 0 || !enemyUnits.Contains(target))
            {
                Debug.LogWarning($"Target {target?.unitName} is invalid or dead, skipping catSkill_1 for player {playerIndex}");
                return;
            }
            Debug.Log($"Executing catSkill_1 for player {playerIndex}, target: {target.unitName}");
                int buffedDamage = Mathf.RoundToInt(playerUnits[playerIndex].damage * 1f);
                PerformBasicAttack(playerIndex, target, buffedDamage);
                dialogueText.text = $"{playerUnits[playerIndex].unitName} 撿起身邊的東西丟向 {target.unitName}";
            EffectManager.Instance.PlayUIEffect("L1", new Vector2(0, 0));
            EffectManager.Instance.PlayUIEffect("B1", new Vector2(0, 0), 0.5f, 0f);

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
            EffectManager.Instance.PlayUIEffect("L2", new Vector2(0, 0));
        });
        dialogueText.text = $"{playerUnits[playerIndex].unitName} 選擇了技能 2";
        RemoveSkillIcon();
    }

    public void catSkill_3(int playerIndex)
    {
        selectedSkillNames[playerIndex].Add("catSkill_3");
        skillQueues[playerIndex].Add(() =>
        {
            if (playerUnits[playerIndex] == null || playerUnits[playerIndex].currentHP <= 0)
            {
                Debug.LogWarning($"玩家 {playerIndex} 無效或已死亡，跳過 catSkill_3");
                skillCooldowns["catSkill_3_" + playerIndex] = catskill3CooldownTurns;
                return;
            }
            Debug.Log($"Executing catSkill_3 for player {playerIndex}");
            int buffedDamage = Mathf.RoundToInt(playerUnits[playerIndex].damage * 1f);
            PerformMultiTargetAttack(playerIndex, buffedDamage);
            skillCooldowns["catSkill_3_" + playerIndex] = catskill3CooldownTurns;
            Debug.Log($"玩家 {playerIndex} 的 catSkill_3 設置冷卻為 {catskill3CooldownTurns} 回合");
            dialogueText.text = $"{playerUnits[playerIndex].unitName} 透過唱歌給全體敵人造成傷害";
            EffectManager.Instance.PlayUIEffect("L3", new Vector2(0, 0));
        });
        dialogueText.text = $"{playerUnits[playerIndex].unitName} 選擇了技能 3";
        RemoveSkillIcon();
    }

    public void catSkill_4(int playerIndex, Unit target)
    {
        selectedSkillNames[playerIndex].Add("catSkill_4");
        skillQueues[playerIndex].Add(() =>
        {
            if (playerUnits[playerIndex] == null || playerUnits[playerIndex].currentHP <= 0)
            {
                Debug.LogWarning($"玩家 {playerIndex} 無效或已死亡，跳過 catSkill_4");
                skillCooldowns["catSkill_4_" + playerIndex] = catskill4CooldownTurns;
                return;
            }
            if (target == null || target.currentHP <= 0 || !playerUnits.Contains(target))
            {
                Debug.LogWarning($"Target {target?.unitName} is invalid or dead, skipping catSkill_4 for player {playerIndex}");
                skillCooldowns["catSkill_4_" + playerIndex] = catskill4CooldownTurns;
                return;
            }
            Debug.Log($"Executing catSkill_4 for player {playerIndex}, target: {target.unitName}");
            buffDebuffManager.ApplyDamageBuff(target, 2.0f, 2);
            skillCooldowns["catSkill_4_" + playerIndex] = catskill4CooldownTurns;
            Debug.Log($"玩家 {playerIndex} 的 catSkill_4 設置冷卻為 {catskill4CooldownTurns} 回合");
            dialogueText.text = $"{playerUnits[playerIndex].unitName} 對 {target.unitName} 施展魔法，提升其攻擊力";
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
        ClearAllHighlights();
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
        ClearAllHighlights();
        if (battlesystem.state != BattleState.PLAYERTURN) return;
        Skill_2(0);
    }

    public void TriggerSkill3ForPlayer0()
    {
        ClearAllHighlights();
        if (battlesystem.state != BattleState.PLAYERTURN) return;
        Skill_3(0);
    }

    public void TriggerSkill4ForPlayer0()
    {
        ClearAllHighlights();
        if (battlesystem.state != BattleState.PLAYERTURN) return;
        Skill_4(0);
    }

    public void TriggerSkill5ForPlayer0()
    {
        ClearAllHighlights();
        if (battlesystem.state != BattleState.PLAYERTURN)
        {
            Debug.LogWarning("無法觸發 Skill_5：不在玩家回合");
            return;
        }
        if (skillCooldowns.ContainsKey("Skill_5_0") && skillCooldowns["Skill_5_0"] > 0)
        {
            dialogueText.text = $"技能 5 正在冷卻中，剩餘 {skillCooldowns["Skill_5_0"]} 回合！";
            Debug.Log($"Skill_5_0 冷卻中，剩餘 {skillCooldowns["Skill_5_0"]} 回合");
            return;
        }
        if (selectedSkillNames[0].Contains("Skill_5"))
        {
            CancelSkill(0, "Skill_5");
        }
        else
        {
            if (!selectedTargets.ContainsKey(0)) // 確保鍵存在
            {
                selectedTargets[0] = null;
            }
            isWaitingForTarget = true;
            pendingSkill = "Skill_5";
            pendingPlayerIndex = 0;
            dialogueText.text = "請點擊一個敵人目標以使用技能 5！";
            Debug.Log("等待玩家選擇 Skill_5 的目標");
        }
    }

    public void TriggerCatSkill1ForPlayer1()
    {
        ClearAllHighlights();
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
        ClearAllHighlights();
        if (battlesystem.state != BattleState.PLAYERTURN) return;
        catSkill_2(1);
    }

    public void TriggerCatSkill3ForPlayer1()
    {
        ClearAllHighlights();
        if (battlesystem.state != BattleState.PLAYERTURN)
        {
            Debug.LogWarning("Cannot trigger catSkill_3: Not in player turn");
            return;
        }
        if (skillCooldowns.ContainsKey("catSkill_3_1") && skillCooldowns["catSkill_3_1"] > 0)
        {
            dialogueText.text = $"貓技能 3 正在冷卻中，剩餘 {skillCooldowns["catSkill_3_1"]} 回合！";
            Debug.Log($"catSkill_3_1 冷卻中，剩餘 {skillCooldowns["catSkill_3_1"]} 回合");
            return;
        }
        if (selectedSkillNames[1].Contains("catSkill_3"))
        {
            CancelSkill(1, "catSkill_3");
        }
        else
        {
            catSkill_3(1);
        }
    }

    public void TriggerCatSkill4ForPlayer1()
    {
        ClearAllHighlights();
        if (battlesystem.state != BattleState.PLAYERTURN)
        {
            Debug.LogWarning("Cannot trigger catSkill_4: Not in player turn");
            return;
        }
        if (skillCooldowns.ContainsKey("catSkill_4_1") && skillCooldowns["catSkill_4_1"] > 0)
        {
            dialogueText.text = $"貓技能 4 正在冷卻中，剩餘 {skillCooldowns["catSkill_4_1"]} 回合！";
            Debug.Log($"catSkill_4_1 冷卻中，剩餘 {skillCooldowns["catSkill_4_1"]} 回合");
            return;
        }
        if (selectedSkillNames[1].Contains("catSkill_4"))
        {
            CancelSkill(1, "catSkill_4");
        }
        else
        {
            if (!selectedTargets.ContainsKey(1))
            {
                selectedTargets[1] = null;
            }
            isWaitingForTarget = true;
            pendingSkill = "catSkill_4";
            pendingPlayerIndex = 1;
            dialogueText.text = "請點擊一個玩家頭像以使用貓技能 4！";
            Debug.Log("Waiting for player to select a target for catSkill_4");
        }
    }

    public void TriggerCatSkill5ForPlayer1()
    {
        ClearAllHighlights();
        if (battlesystem.state != BattleState.PLAYERTURN) return;
        catSkill_5(1);
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

        if (GetTotalSelectedSkills() == 0)
        {
            dialogueText.text = "請至少為一名玩家選擇一個技能！";
            return;
        }

        // 為每個有技能的玩家啟動協程
        for (int playerIndex = 0; playerIndex < playerUnits.Count; playerIndex++)
        {
            if (skillQueues[playerIndex].Count > 0)
            {
                StartCoroutine(ExecuteSkillQueueWithCallback(playerIndex));
            }
        }
    }

    void PerformMultiTargetAttack(int playerIndex, int damage)
    {
        if (enemyUnits == null || enemyUnits.Count == 0) return;

        List<int> deadIndices = new List<int>();

        foreach (Unit enemy in enemyUnits.ToArray())
        {
            if (enemy != null && enemy.currentHP > 0)
            {
                bool isDead = enemy.TakeDamage(damage);
                int targetIndex = enemyUnits.IndexOf(enemy);

                if (targetIndex >= 0 && targetIndex < enemyHUDs.Count && enemyHUDs[targetIndex] != null)
                {
                    enemyHUDs[targetIndex].SetHP(enemy.currentHP);
                }

                if (isDead)
                {
                    deadIndices.Add(targetIndex);
                    if (selectedTargets.ContainsValue(enemy))
                    {
                        foreach (var kvp in selectedTargets)
                        {
                            if (kvp.Value == enemy)
                            {
                                selectedTargets[kvp.Key] = null;
                                HighlightTarget(enemy, false);
                                break;
                            }
                        }
                    }
                    CleanSkillQueue(playerIndex, enemy);
                }
            }
        }

        StartCoroutine(DelayedDestroyEnemies(deadIndices));
    }

    private IEnumerator DelayedDestroyEnemies(List<int> indices)
    {
        yield return new WaitForEndOfFrame();
        indices.Sort((a, b) => b.CompareTo(a));
        foreach (int index in indices)
        {
            if (battlesystem != null && index >= 0 && index < enemyUnits.Count)
            {
                battlesystem.DestroyEnemy(index);
            }
        }

        if (AllEnemiesDead() && battlesystem != null)
        {
            battlesystem.state = BattleState.WON;
            battlesystem.EndBattle();
        }
    }

    IEnumerator ExecuteSkillQueueWithCallback(int playerIndex)
    {
        Debug.Log($"Starting ExecuteSkillQueueWithCallback for player {playerIndex}");
        yield return StartCoroutine(ExecuteSkillQueue(playerIndex));
        Debug.Log($"Finished ExecuteSkillQueueWithCallback for player {playerIndex}");

        // 檢查是否所有玩家的技能隊列都清空
        bool allQueuesEmpty = true;
        foreach (var queue in skillQueues.Values)
        {
            if (queue.Count > 0)
            {
                allQueuesEmpty = false;
                break;
            }
        }

        if (allQueuesEmpty && battlesystem != null)
        {
            Debug.Log("所有技能隊列已清空，進入敵人回合");
            battlesystem.OnPlayerConfirmSkillQueue(); // 不再傳 playerIndex！
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
            // 檢查 playerUnits 是否有效
            if (playerUnits == null || playerIndex < 0 || playerIndex >= playerUnits.Count || playerUnits[playerIndex] == null)
            {
                Debug.LogWarning($"Invalid playerUnits or playerIndex {playerIndex}, skipping skill action.");
                return;
            }

            // 檢查 selectedTargets（僅對需要目標的技能）
            if (selectedTargets.ContainsKey(playerIndex) && selectedTargets[playerIndex] == null)
            {
                Debug.LogWarning($"Null target for player {playerIndex}, skipping skill action.");
                return;
            }

            skillAction.Invoke();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error executing skill for player {playerIndex}: {e.Message}\nStackTrace: {e.StackTrace}");
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
            if (playerUnits[playerIndex] == null || playerUnits[playerIndex].currentHP <= 0)
            {
                Debug.LogWarning($"Player {playerIndex} is invalid or dead, skipping skill execution.");
                continue;
            }
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
        if (target == null || target.currentHP <= 0 || !enemyUnits.Contains(target)) return;

        int actualDamage = damageOverride > 0 ? damageOverride : playerUnits[playerIndex].damage;
        bool isDead = target.TakeDamage(actualDamage);

        // 更新 HUD
        int targetIndex = enemyUnits.IndexOf(target);
        if (targetIndex >= 0 && targetIndex < enemyHUDs.Count && enemyHUDs[targetIndex] != null)
        {
            enemyHUDs[targetIndex].SetHP(target.currentHP);
        }

        if (isDead)
        {
            // 關鍵：延遲銷毀，避免當前幀崩潰
            StartCoroutine(DelayedDestroyEnemy(targetIndex));
        }
    }

    // 新增：延遲銷毀
    private IEnumerator DelayedDestroyEnemy(int index)
    {
        yield return new WaitForEndOfFrame(); // 等一幀
        if (battlesystem != null && index >= 0 && index < enemyUnits.Count)
        {
            battlesystem.DestroyEnemy(index);
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
                if (!IsActionTargeting(deadEnemy, skillQueues[playerIndex][i], selectedSkillNames[playerIndex][i]))
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

    private bool IsActionTargeting(Unit target, System.Action action, string skillName)
    {
        // 如果技能需要目標單位，檢查是否針對已死亡的敵人
        if (skillName == "Skill_1" || skillName == "Skill_5" || skillName == "catSkill_1" || skillName == "catSkill_4" ||
            skillName == "Can" || skillName == "Grass" || skillName == "Drying")
        {
            return selectedTargets.ContainsValue(target);
        }
        return false; // 不需要目標的技能（如 Skill_2、catSkill_3）不受影響
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
            if (skillCooldowns[key] > 0)
            {
                skillCooldowns[key]--;
                Debug.Log($"技能 {key} 冷卻減少，剩餘 {skillCooldowns[key]} 回合");
            }
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