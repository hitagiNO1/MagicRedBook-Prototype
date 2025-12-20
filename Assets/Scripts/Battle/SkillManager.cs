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

    // ==================== 新增：技能按鈕引用 ====================
    [Header("Skill Buttons - Player 0 (兔子)")]
    public Button skill1Button;
    public Button skill2Button;
    public Button skill3Button;
    public Button skill4Button;
    public Button skill5Button;

    [Header("Skill Buttons - Player 1 (貓)")]
    public Button catSkill1Button;
    public Button catSkill2Button;
    public Button catSkill3Button;
    public Button catSkill4Button;
    public Button catSkill5Button;

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

        // 初始更新所有技能按鈕狀態（冷卻、已選等）
        UpdateAllSkillButtonStates();
    }

    void Update()
    {
        if (isWaitingForTarget && Input.GetMouseButtonDown(0))
        {
            // 移除對 catSkill_4 的特殊阻擋，讓它正常走 raycast
            // （因為 catSkill_4 要點頭像，我們用 OnPlayerHeadClick 處理，不用 raycast）

            Debug.Log("Left mouse button clicked, processing target selection");
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Unit targetUnit = null;

            // 判斷是需要點敵人還是點玩家
            bool isPlayerTarget = pendingSkill == "Can" || pendingSkill == "Grass" || pendingSkill == "Drying" || pendingSkill == "catSkill_4";

            if (isPlayerTarget)
            {
                // 玩家目標：不要用 raycast（因為頭像可能是 UI Button），靠 OnPlayerHeadClick 處理
                // 所以這裡什麼都不做，讓點頭像觸發 OnPlayerHeadClick
                return;
            }
            else
            {
                // 敵人目標：正常 raycast
                LayerMask targetLayer = enemyLayer;

                RaycastHit2D hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity, LayerMask.GetMask("Enemy"));
                if (hit.collider != null)
                {
                    targetUnit = hit.collider.GetComponent<Unit>();
                }
                else
                {
                    RaycastHit hit3D;
                    if (Physics.Raycast(ray, out hit3D, Mathf.Infinity, targetLayer))
                    {
                        targetUnit = hit3D.collider.GetComponent<Unit>();
                    }
                }

                if (targetUnit != null && enemyUnits.Contains(targetUnit) && targetUnit.currentHP > 0)
                {
                    SelectTarget(pendingPlayerIndex, targetUnit);
                    ExecutePendingSkill();
                }
                else
                {
                    dialogueText.text = "請點擊一個有效的敵人目標！";
                }
            }
        }

        // 右鍵取消：新增恢復按鈕亮度邏輯
        if (isWaitingForTarget && Input.GetMouseButtonDown(1))
        {
            CancelPendingSelection();

            // 關鍵：取消等待時恢復對應按鈕亮度
            if (pendingSkill != null)
            {
                Button buttonToRestore = GetSkillButton(pendingPlayerIndex, pendingSkill);
                if (buttonToRestore != null)
                    SetButtonDimmed(buttonToRestore, false);
            }

            UpdateAllSkillButtonStates(); // 保險起見，全面更新
        }
    }

    private Button GetSkillButton(int playerIndex, string skillName)
    {
        if (playerIndex == 0)
        {
            switch (skillName)
            {
                case "Skill_1": return skill1Button;
                case "Skill_2": return skill2Button;
                case "Skill_3": return skill3Button;
                case "Skill_4": return skill4Button;
                case "Skill_5": return skill5Button;
            }
        }
        else if (playerIndex == 1)
        {
            switch (skillName)
            {
                case "catSkill_1": return catSkill1Button;
                case "catSkill_2": return catSkill2Button;
                case "catSkill_3": return catSkill3Button;
                case "catSkill_4": return catSkill4Button;
                case "catSkill_5": return catSkill5Button;
            }
        }
        return null;
    }

    private void CancelPendingSelection()
    {
        Button pendingButton = GetSkillButton(pendingPlayerIndex, pendingSkill);
        if (pendingButton != null)
            SetButtonDimmed(pendingButton, false);

        isWaitingForTarget = false;
        pendingSkill = null;
        pendingPlayerIndex = -1;
        dialogueText.text = "已取消技能選擇";
        Debug.Log("Skill selection cancelled by right-click");

        UpdateAllSkillButtonStates();
    }

    // ==================== 按鈕顏色控制 ====================
    private void SetButtonDimmed(Button button, bool dimmed)
    {
        if (button == null) return;

        ColorBlock colors = button.colors;
        if (dimmed)
        {
            colors.normalColor = new Color(0.5f, 0.5f, 0.5f, 1f);
            colors.highlightedColor = new Color(0.6f, 0.6f, 0.6f, 1f);
            colors.pressedColor = new Color(0.4f, 0.4f, 0.4f, 1f);
            colors.selectedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
            button.interactable = false;
        }
        else
        {
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            colors.selectedColor = Color.white;
            button.interactable = true;
        }
        button.colors = colors;
    }

    private void UpdateAllSkillButtonStates()
    {
        // Player 0 (黑貓)
        SetButtonDimmed(skill1Button, selectedSkillNames[0].Contains("Skill_1"));
        SetButtonDimmed(skill2Button, skillCooldowns["Skill_2_0"] > 0 || selectedSkillNames[0].Contains("Skill_2"));
        SetButtonDimmed(skill3Button, skillCooldowns["Skill_3_0"] > 0 || selectedSkillNames[0].Contains("Skill_3"));
        SetButtonDimmed(skill4Button, skillCooldowns["Skill_4_0"] > 0 || selectedSkillNames[0].Contains("Skill_4"));
        SetButtonDimmed(skill5Button, skillCooldowns["Skill_5_0"] > 0 || selectedSkillNames[0].Contains("Skill_5"));

        // Player 1 (萊菈)
        SetButtonDimmed(catSkill1Button, selectedSkillNames[1].Contains("catSkill_1"));
        SetButtonDimmed(catSkill2Button, skillCooldowns["catSkill_2_1"] > 0 || selectedSkillNames[1].Contains("catSkill_2"));
        SetButtonDimmed(catSkill3Button, skillCooldowns["catSkill_3_1"] > 0 || selectedSkillNames[1].Contains("catSkill_3"));
        SetButtonDimmed(catSkill4Button, skillCooldowns["catSkill_4_1"] > 0 || selectedSkillNames[1].Contains("catSkill_4"));
        SetButtonDimmed(catSkill5Button, skillCooldowns["catSkill_5_1"] > 0 || selectedSkillNames[1].Contains("catSkill_5"));
    }

    private void ResetAllSkillButtonStates()
    {
        SetButtonDimmed(skill1Button, false);
        SetButtonDimmed(skill2Button, false);
        SetButtonDimmed(skill3Button, false);
        SetButtonDimmed(skill4Button, false);
        SetButtonDimmed(skill5Button, false);
        SetButtonDimmed(catSkill1Button, false);
        SetButtonDimmed(catSkill2Button, false);
        SetButtonDimmed(catSkill3Button, false);
        SetButtonDimmed(catSkill4Button, false);
        SetButtonDimmed(catSkill5Button, false);
    }

    public void SelectTarget(int playerIndex, Unit target)
    {
        Debug.Log($"SelectTarget called for playerIndex: {playerIndex}, target: {target?.unitName}");
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

        bool isWaitingForPlayerTarget =
            pendingSkill == "catSkill_4" ||
            pendingSkill == "Can" ||
            pendingSkill == "Grass" ||
            pendingSkill == "Drying";

        // 移除「pendingPlayerIndex != GetCurrentPlayerIndex()」這個錯誤檢查
        if (!isWaitingForPlayerTarget || pendingPlayerIndex < 0)
        {
            Debug.Log("目前沒有等待選擇玩家目標的技能或道具");
            return;
        }

        // 成功選擇目標
        dialogueText.text = $"{playerUnits[pendingPlayerIndex].unitName} 選擇對 {selectedTarget.unitName} 使用 {pendingSkill}";
        SelectTarget(pendingPlayerIndex, selectedTarget);
        ExecutePendingSkill();
    }

    private int GetCurrentPlayerIndex()
    {
        if (battlesystem != null)
        {
            return battlesystem.currentPlayerIndex;
        }
        return 0;
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

            // 取消後更新按鈕狀態
            UpdateAllSkillButtonStates();
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

    // ==================== 技能實作 ====================
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
        UpdateAllSkillButtonStates();
    }

    public void Skill_2(int playerIndex)
    {
        if (playerIndex != 0 || !ValidateSkill(playerIndex, "Skill_2")) return;

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
        UpdateAllSkillButtonStates();
    }

    public void Skill_3(int playerIndex)
    {
        if (playerIndex != 0 || !ValidateSkill(playerIndex, "Skill_3")) return;

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
        UpdateAllSkillButtonStates();
    }

    public void Skill_4(int playerIndex)
    {
        if (playerIndex != 0 || !ValidateSkill(playerIndex, "Skill_4")) return;

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
        UpdateAllSkillButtonStates();
    }

    public void Skill_5(int playerIndex, Unit target)
    {
        if (!ValidateSkill(playerIndex, "Skill_5") || !ValidateTarget(playerIndex, target)) return;

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
                skillCooldowns["Skill_5_" + playerIndex] = skill5CooldownTurns;
                return;
            }
            PlaySoundByName("skill4-1");
            PlaySoundByName("skill4-2");
            buffDebuffManager.ApplyDefenseDebuff(target, 0.7f, 2);
            int buffedDamage = Mathf.RoundToInt(playerUnits[playerIndex].damage * 1.3f);
            PerformBasicAttack(playerIndex, target, buffedDamage);
            skillCooldowns["Skill_5_" + playerIndex] = skill5CooldownTurns;
            dialogueText.text = $"{playerUnits[playerIndex].unitName} 抱住敵方，使出最強攻擊兔子踹！";
            EffectManager.Instance.PlayUIEffect("B5", new Vector2(0, 0));
        });
        dialogueText.text = $"{playerUnits[playerIndex].unitName} 選擇了技能 5 對 {target.unitName}";
        RemoveSkillIcon();
        UpdateAllSkillButtonStates();
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
            int buffedDamage = Mathf.RoundToInt(playerUnits[playerIndex].damage * 1f);
            PerformBasicAttack(playerIndex, target, buffedDamage);
            dialogueText.text = $"{playerUnits[playerIndex].unitName} 撿起身邊的東西丟向 {target.unitName}";
            EffectManager.Instance.PlayUIEffect("L1", new Vector2(0, 0));
            EffectManager.Instance.PlayUIEffect("B1", new Vector2(0, 0), 0.5f, 0f);
        });
        dialogueText.text = $"{playerUnits[playerIndex].unitName} 選擇了技能 1 對 {target.unitName}";
        RemoveSkillIcon();
        UpdateAllSkillButtonStates();
    }

    public void catSkill_2(int playerIndex)
    {
        if (playerIndex != 1 || !ValidateSkill(playerIndex, "catSkill_2")) return;

        selectedSkillNames[playerIndex].Add("catSkill_2");
        skillQueues[playerIndex].Add(() =>
        {
            buffDebuffManager.Heal(playerUnits[playerIndex], 0.75f);
            skillCooldowns["catSkill_2_" + playerIndex] = catskill2CooldownTurns;
            dialogueText.text = $"{playerUnits[playerIndex].unitName} 將傷口進行包紮";
            EffectManager.Instance.PlayUIEffect("L2", new Vector2(0, 0));
        });
        dialogueText.text = $"{playerUnits[playerIndex].unitName} 選擇了技能 2";
        RemoveSkillIcon();
        UpdateAllSkillButtonStates();
    }

    public void catSkill_3(int playerIndex)
    {
        if (!ValidateSkill(playerIndex, "catSkill_3")) return;

        selectedSkillNames[playerIndex].Add("catSkill_3");
        skillQueues[playerIndex].Add(() =>
        {
            if (playerUnits[playerIndex] == null || playerUnits[playerIndex].currentHP <= 0)
            {
                Debug.LogWarning($"玩家 {playerIndex} 無效或已死亡，跳過 catSkill_3");
                skillCooldowns["catSkill_3_" + playerIndex] = catskill3CooldownTurns;
                return;
            }
            int buffedDamage = Mathf.RoundToInt(playerUnits[playerIndex].damage * 1f);
            PerformMultiTargetAttack(playerIndex, buffedDamage);
            skillCooldowns["catSkill_3_" + playerIndex] = catskill3CooldownTurns;
            dialogueText.text = $"{playerUnits[playerIndex].unitName} 透過唱歌給全體敵人造成傷害";
            EffectManager.Instance.PlayUIEffect("L3", new Vector2(0, 0));
        });
        dialogueText.text = $"{playerUnits[playerIndex].unitName} 選擇了技能 3";
        RemoveSkillIcon();
        UpdateAllSkillButtonStates();
    }

    public void catSkill_4(int playerIndex, Unit target)
    {
        if (!ValidateSkill(playerIndex, "catSkill_4") || !ValidateTarget(playerIndex, target)) return;

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
            buffDebuffManager.ApplyDamageBuff(target, 2.0f, 2);
            skillCooldowns["catSkill_4_" + playerIndex] = catskill4CooldownTurns;
            dialogueText.text = $"{playerUnits[playerIndex].unitName} 對 {target.unitName} 施展魔法，提升其攻擊力";
        });
        dialogueText.text = $"{playerUnits[playerIndex].unitName} 選擇了技能 4 對 {target.unitName}";
        RemoveSkillIcon();
        UpdateAllSkillButtonStates();
    }

    public void catSkill_5(int playerIndex)
    {
        if (playerIndex != 1 || !ValidateSkill(playerIndex, "catSkill_5")) return;

        selectedSkillNames[playerIndex].Add("catSkill_5");
        skillQueues[playerIndex].Add(() =>
        {
            foreach (Unit player in playerUnits)
            {
                buffDebuffManager.ApplyFixedDamageReduction(player, 0.7f, 1);
            }
            skillCooldowns["catSkill_5_" + playerIndex] = catskill5CooldownTurns;
            dialogueText.text = $"{playerUnits[playerIndex].unitName} 透過歌聲振奮士氣，使我方全體承受傷害降低";
        });
        dialogueText.text = $"{playerUnits[playerIndex].unitName} 選擇了技能 5";
        RemoveSkillIcon();
        UpdateAllSkillButtonStates();
    }

    // ==================== Trigger 技能按鈕 ====================
    public void TriggerSkill1ForPlayer0()
    {
        ClearAllHighlights();
        if (battlesystem.state != BattleState.PLAYERTURN) return;

        if (selectedSkillNames[0].Contains("Skill_1"))
        {
            CancelSkill(0, "Skill_1");
            SetButtonDimmed(skill1Button, false);
        }
        else
        {
            isWaitingForTarget = true;
            pendingSkill = "Skill_1";
            pendingPlayerIndex = 0;
            dialogueText.text = "請點擊一個敵人目標以使用技能 1！";
            SetButtonDimmed(skill1Button, true);
        }
    }

    public void TriggerSkill2ForPlayer0()
    {
        ClearAllHighlights();
        if (battlesystem.state != BattleState.PLAYERTURN) return;

        if (selectedSkillNames[0].Contains("Skill_2"))
        {
            CancelSkill(0, "Skill_2");
            SetButtonDimmed(skill2Button, false);
            return;
        }

        Skill_2(0);
        SetButtonDimmed(skill2Button, true);
    }

    public void TriggerSkill3ForPlayer0()
    {
        ClearAllHighlights();
        if (battlesystem.state != BattleState.PLAYERTURN) return;

        if (selectedSkillNames[0].Contains("Skill_3"))
        {
            CancelSkill(0, "Skill_3");
            SetButtonDimmed(skill3Button, false);
            return;
        }

        Skill_3(0);
        SetButtonDimmed(skill3Button, true);
    }

    public void TriggerSkill4ForPlayer0()
    {
        ClearAllHighlights();
        if (battlesystem.state != BattleState.PLAYERTURN) return;

        if (selectedSkillNames[0].Contains("Skill_4"))
        {
            CancelSkill(0, "Skill_4");
            SetButtonDimmed(skill4Button, false);
            return;
        }

        Skill_4(0);
        SetButtonDimmed(skill4Button, true);
    }

    public void TriggerSkill5ForPlayer0()
    {
        ClearAllHighlights();
        if (battlesystem.state != BattleState.PLAYERTURN) return;

        if (skillCooldowns["Skill_5_0"] > 0)
        {
            dialogueText.text = $"技能 5 正在冷卻中，剩餘 {skillCooldowns["Skill_5_0"]} 回合！";
            return;
        }

        if (selectedSkillNames[0].Contains("Skill_5"))
        {
            CancelSkill(0, "Skill_5");
            SetButtonDimmed(skill5Button, false);
        }
        else
        {
            isWaitingForTarget = true;
            pendingSkill = "Skill_5";
            pendingPlayerIndex = 0;
            dialogueText.text = "請點擊一個敵人目標以使用技能 5！";
            SetButtonDimmed(skill5Button, true);
        }
    }

    public void TriggerCatSkill1ForPlayer1()
    {
        ClearAllHighlights();
        if (battlesystem.state != BattleState.PLAYERTURN) return;

        if (selectedSkillNames[1].Contains("catSkill_1"))
        {
            CancelSkill(1, "catSkill_1");
            SetButtonDimmed(catSkill1Button, false);
        }
        else
        {
            isWaitingForTarget = true;
            pendingSkill = "catSkill_1";
            pendingPlayerIndex = 1;
            dialogueText.text = "請點擊一個敵人目標以使用貓技能 1！";
            SetButtonDimmed(catSkill1Button, true);
        }
    }

    public void TriggerCatSkill2ForPlayer1()
    {
        ClearAllHighlights();
        if (battlesystem.state != BattleState.PLAYERTURN) return;

        if (selectedSkillNames[1].Contains("catSkill_2"))
        {
            CancelSkill(1, "catSkill_2");
            SetButtonDimmed(catSkill2Button, false);
            return;
        }

        catSkill_2(1);
        SetButtonDimmed(catSkill2Button, true);
    }

    public void TriggerCatSkill3ForPlayer1()
    {
        ClearAllHighlights();
        if (battlesystem.state != BattleState.PLAYERTURN) return;

        if (skillCooldowns["catSkill_3_1"] > 0)
        {
            dialogueText.text = $"貓技能 3 正在冷卻中，剩餘 {skillCooldowns["catSkill_3_1"]} 回合！";
            return;
        }

        if (selectedSkillNames[1].Contains("catSkill_3"))
        {
            CancelSkill(1, "catSkill_3");
            SetButtonDimmed(catSkill3Button, false);
        }
        else
        {
            catSkill_3(1);
            SetButtonDimmed(catSkill3Button, true);
        }
    }

    public void TriggerCatSkill4ForPlayer1()
    {
        ClearAllHighlights();
        if (battlesystem.state != BattleState.PLAYERTURN) return;

        if (skillCooldowns["catSkill_4_1"] > 0)
        {
            dialogueText.text = $"貓技能 4 正在冷卻中，剩餘 {skillCooldowns["catSkill_4_1"]} 回合！";
            return;
        }

        if (selectedSkillNames[1].Contains("catSkill_4"))
        {
            CancelSkill(1, "catSkill_4");
            SetButtonDimmed(catSkill4Button, false);
        }
        else
        {
            isWaitingForTarget = true;
            pendingSkill = "catSkill_4";
            pendingPlayerIndex = 1;
            dialogueText.text = "請點擊一個玩家頭像以使用貓技能 4！";
            SetButtonDimmed(catSkill4Button, true);
        }
    }

    public void TriggerCatSkill5ForPlayer1()
    {
        ClearAllHighlights();
        if (battlesystem.state != BattleState.PLAYERTURN) return;

        if (selectedSkillNames[1].Contains("catSkill_5"))
        {
            CancelSkill(1, "catSkill_5");
            SetButtonDimmed(catSkill5Button, false);
            return;
        }

        catSkill_5(1);
        SetButtonDimmed(catSkill5Button, true);
    }

    // ==================== 其他功能 ====================
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

        for (int playerIndex = 0; playerIndex < playerUnits.Count; playerIndex++)
        {
            if (skillQueues[playerIndex].Count > 0)
            {
                StartCoroutine(ExecuteSkillQueueWithCallback(playerIndex));
            }
        }
        battlesystem.OnPlayerConfirmSkillQueue();
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
            battlesystem.OnPlayerConfirmSkillQueue();
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
            if (playerUnits == null || playerIndex < 0 || playerIndex >= playerUnits.Count || playerUnits[playerIndex] == null)
            {
                Debug.LogWarning($"Invalid playerUnits or playerIndex {playerIndex}, skipping skill action.");
                return;
            }
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
        }
        skillQueues[playerIndex].Clear();
        selectedSkillNames[playerIndex].Clear();
    }

    void PerformBasicAttack(int playerIndex, Unit target, int damageOverride = -1)
    {
        if (target == null || target.currentHP <= 0 || !enemyUnits.Contains(target)) return;

        int actualDamage = damageOverride > 0 ? damageOverride : playerUnits[playerIndex].damage;
        bool isDead = target.TakeDamage(actualDamage);

        int targetIndex = enemyUnits.IndexOf(target);
        if (targetIndex >= 0 && targetIndex < enemyHUDs.Count && enemyHUDs[targetIndex] != null)
        {
            enemyHUDs[targetIndex].SetHP(target.currentHP);
        }

        if (isDead)
        {
            StartCoroutine(DelayedDestroyEnemy(targetIndex));
        }
    }

    private IEnumerator DelayedDestroyEnemy(int index)
    {
        yield return new WaitForEndOfFrame();
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
        }
    }

    private bool IsActionTargeting(Unit target, System.Action action, string skillName)
    {
        if (skillName == "Skill_1" || skillName == "Skill_5" || skillName == "catSkill_1" || skillName == "catSkill_4" ||
            skillName == "Can" || skillName == "Grass" || skillName == "Drying")
        {
            return selectedTargets.ContainsValue(target);
        }
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
            if (skillCooldowns[key] > 0)
            {
                skillCooldowns[key]--;
            }
        }
        UpdateAllSkillButtonStates();
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

    public IEnumerator DelayedInitSkillIcons(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        InitSkillIcons();
    }

    public void InitSkillIcons()
    {
        ResetSkillIcons();
        ResetAllSkillButtonStates();
        UpdateAllSkillButtonStates(); // 根據當前冷卻狀態更新
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