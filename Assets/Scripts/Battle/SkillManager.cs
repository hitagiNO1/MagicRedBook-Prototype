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
    public List<BattleHUD> playerHUDs;
    public BattleSystem battlesystem;
    public BuffDebuffManager buffDebuffManager;
    public Itemuse itemuse;
    public LayerMask enemyLayer;
    public LayerMask playerLayer;
    public Dictionary<string, int> skillCooldowns = new Dictionary<string, int>();
    public List<Button> playerSkillButtons;     // 或按玩家索引分開
    public List<Image> playerSkillIcons;        // 或其他 UI 元件

    private Dictionary<string, string> skillDisplayNameMap = new Dictionary<string, string>()
    {
        // 兔子技能 (Player 0)
        { "Skill_1", "吃我一擊！" },
        { "Skill_2", "上吧，球球們！" },
        { "Skill_3", "舔舔舔！" },
        { "Skill_4", "一顆煤炭～" },
        { "Skill_5", "連環兔子踹！" },

        // 貓技能 (Player 1)
        { "catSkill_1", "三分球！" },
        { "catSkill_2", "小意思！" },
        { "catSkill_3", "Do～Re～Mi～" },
        { "catSkill_4", "打起精神來！" },
        { "catSkill_5", "痛痛飛走～" },

        // 道具
        { "Can", "罐罐" },
        { "Tuna", "鮪魚肉泥" },
        { "Grass", "貓草" },
        { "Drying", "肉乾" }
    };

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

    public Dictionary<int, List<IEnumerator>> skillQueues = new Dictionary<int, List<IEnumerator>>();
    public Dictionary<int, List<string>> selectedSkillNames = new Dictionary<int, List<string>>();
    public Dictionary<int, List<bool>> skillNeedsTarget = new Dictionary<int, List<bool>>();
    public int maxSkillPerTurn = 3;
    public Dictionary<int, Unit> selectedTargets = new Dictionary<int, Unit>();

    // ==============================================================
    // ★ 新增：全域排隊追蹤器 (記錄是哪個玩家點擊的順序)
    // ==============================================================
    public List<int> executionOrderTracker = new List<int>();

    public bool isWaitingForTarget = false;
    public string pendingSkill = null;
    public int pendingPlayerIndex = -1;
    public static SkillManager Instance { get; private set; }

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

    public void Start()
    {
        audioSource = GetComponent<AudioSource>();

        for (int i = 0; i < 2; i++)
        {
            skillQueues[i] = new List<IEnumerator>();
            selectedSkillNames[i] = new List<string>();
            skillNeedsTarget[i] = new List<bool>();
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
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Update()
    {
        if (isWaitingForTarget && Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Unit targetUnit = null;

            bool isPlayerTarget = pendingSkill == "Can" || pendingSkill == "Grass" || pendingSkill == "Drying" || pendingSkill == "catSkill_4";

            if (isPlayerTarget)
            {
                return;
            }
            else
            {
                RaycastHit2D hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity, LayerMask.GetMask("Enemy"));
                if (hit.collider != null)
                {
                    targetUnit = hit.collider.GetComponent<Unit>();
                }
                else
                {
                    RaycastHit hit3D;
                    if (Physics.Raycast(ray, out hit3D, Mathf.Infinity, enemyLayer))
                    {
                        targetUnit = hit3D.collider.GetComponent<Unit>();
                    }
                }

                if (targetUnit != null && enemyUnits.Contains(targetUnit) && !targetUnit.isDead)
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

        if (Input.GetMouseButtonDown(1))
        {
            CancelLatestSkill();
        }
    }

    // ==============================================================
    // ★ 統一的「加入佇列」方法：所有的技能跟道具都呼叫這裡發號碼牌
    // ==============================================================
    public void AddSkillToQueue(int playerIndex, string skillName, bool needsTarget, IEnumerator coroutine)
    {
        selectedSkillNames[playerIndex].Add(skillName);
        skillNeedsTarget[playerIndex].Add(needsTarget);
        skillQueues[playerIndex].Add(coroutine);

        // 發號碼牌，記錄這個順位是誰的
        executionOrderTracker.Add(playerIndex);
    }

    // ==============================================================
    // ★ 統一的「移出佇列」方法：精準抽回號碼牌
    // ==============================================================
    public void RemoveSkillFromQueue(int playerIndex, string skillName)
    {
        if (selectedSkillNames[playerIndex].Contains(skillName))
        {
            int index = selectedSkillNames[playerIndex].IndexOf(skillName);

            // 從點擊順序追蹤器中精準移除
            int count = 0;
            for (int i = 0; i < executionOrderTracker.Count; i++)
            {
                if (executionOrderTracker[i] == playerIndex)
                {
                    if (count == index)
                    {
                        executionOrderTracker.RemoveAt(i);
                        break;
                    }
                    count++;
                }
            }

            selectedSkillNames[playerIndex].RemoveAt(index);
            skillQueues[playerIndex].RemoveAt(index);
            skillNeedsTarget[playerIndex].RemoveAt(index);
        }
    }

    public int GetSkillCost(string skillName)
    {
        if (skillName == "Skill_4" || skillName == "catSkill_4") return 2;
        if (skillName == "Skill_5" || skillName == "catSkill_5") return 3;
        return 1;
    }

    public int GetTotalSelectedSkills()
    {
        int totalAPUsed = 0;
        foreach (var kvp in selectedSkillNames)
        {
            foreach (string skillName in kvp.Value)
            {
                totalAPUsed += GetSkillCost(skillName);
            }
        }
        return totalAPUsed;
    }

    public void CancelLatestSkill()
    {
        if (isWaitingForTarget && pendingSkill != null && pendingPlayerIndex >= 0)
        {
            Button pendingButton = GetSkillButton(pendingPlayerIndex, pendingSkill);
            if (pendingButton != null) SetButtonDimmed(pendingButton, false);
            isWaitingForTarget = false;
            pendingSkill = null;
            pendingPlayerIndex = -1;
            dialogueText.text = "已取消正在選擇的技能";
            UpdateAllSkillButtonStates();
            return;
        }

        // ★ 修改：直接看號碼牌最後一張是誰的，超級精準！
        if (executionOrderTracker.Count == 0)
        {
            dialogueText.text = "沒有可取消的技能";
            return;
        }

        int latestPlayerIndex = executionOrderTracker[executionOrderTracker.Count - 1];
        int latestSkillIndex = selectedSkillNames[latestPlayerIndex].Count - 1;
        string latestSkillName = selectedSkillNames[latestPlayerIndex][latestSkillIndex];

        Unit playerUnit = playerUnits[latestPlayerIndex];
        string readableName = GetSkillDisplayName(latestSkillName);

        dialogueText.text = $"<b>{playerUnit.unitName}</b> 取消了 <b>{readableName}</b>";

        if (selectedTargets.ContainsKey(latestPlayerIndex) && selectedTargets[latestPlayerIndex] != null)
        {
            HighlightTarget(selectedTargets[latestPlayerIndex], false);
            selectedTargets[latestPlayerIndex] = null;
        }

        Button buttonToRestore = GetSkillButton(latestPlayerIndex, latestSkillName);
        if (buttonToRestore != null) SetButtonDimmed(buttonToRestore, false);

        AddSkillIcon(GetSkillCost(latestSkillName));

        // 呼叫統一的移除方法
        RemoveSkillFromQueue(latestPlayerIndex, latestSkillName);

        UpdateAllSkillButtonStates();
    }

    public void CancelSkill(int playerIndex, string skillName)
    {
        if (selectedSkillNames[playerIndex].Contains(skillName))
        {
            if (selectedTargets.ContainsKey(playerIndex) && selectedTargets[playerIndex] != null)
            {
                HighlightTarget(selectedTargets[playerIndex], false);
                selectedTargets[playerIndex] = null;
            }

            dialogueText.text = $"<b>{playerUnits[playerIndex].unitName}</b> 取消了 <b>{GetSkillDisplayName(skillName)}</b>";
            AddSkillIcon(GetSkillCost(skillName));

            // 呼叫統一的移除方法
            RemoveSkillFromQueue(playerIndex, skillName);

            UpdateAllSkillButtonStates();
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
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            button.interactable = false;
        }
        else
        {
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.78f, 0.78f, 0.78f, 0.5f);
            button.interactable = true;
        }
        button.colors = colors;
    }

    public void UpdateAllSkillButtonStates()
    {
        if (playerUnits == null || playerUnits.Count < 2) return;

        bool p0Dead = playerUnits[0] == null || playerUnits[0].isDead || playerUnits[0].currentHP <= 0;
        bool p1Dead = playerUnits[1] == null || playerUnits[1].isDead || playerUnits[1].currentHP <= 0;

        bool p0Silenced = buffDebuffManager != null && buffDebuffManager.IsSkillDisabled(playerUnits[0]);
        bool p1Silenced = buffDebuffManager != null && buffDebuffManager.IsSkillDisabled(playerUnits[1]);

        bool p0OnlySkill1 = playerUnits[0].equippedGear.Contains("穿戴式紙箱盔甲") || (EquipmentManager.Instance != null && EquipmentManager.Instance.HasEquipment("穿戴式紙箱盔甲"));
        bool p1OnlySkill1 = playerUnits[1].equippedGear.Contains("穿戴式紙箱盔甲") || (EquipmentManager.Instance != null && EquipmentManager.Instance.HasEquipment("穿戴式紙箱盔甲"));

        bool p0LockAll = p0Dead || p0Silenced;
        bool p1LockAll = p1Dead || p1Silenced;

        SetButtonDimmed(skill1Button, p0LockAll || selectedSkillNames[0].Contains("Skill_1") || skillCooldowns["Skill_1_0"] > 0);
        SetButtonDimmed(skill2Button, p0LockAll || p0OnlySkill1 || selectedSkillNames[0].Contains("Skill_2") || skillCooldowns["Skill_2_0"] > 0);
        SetButtonDimmed(skill3Button, p0LockAll || p0OnlySkill1 || selectedSkillNames[0].Contains("Skill_3") || skillCooldowns["Skill_3_0"] > 0);
        SetButtonDimmed(skill4Button, p0LockAll || p0OnlySkill1 || selectedSkillNames[0].Contains("Skill_4") || skillCooldowns["Skill_4_0"] > 0);
        SetButtonDimmed(skill5Button, p0LockAll || p0OnlySkill1 || selectedSkillNames[0].Contains("Skill_5") || skillCooldowns["Skill_5_0"] > 0);

        SetButtonDimmed(catSkill1Button, p1LockAll || selectedSkillNames[1].Contains("catSkill_1") || skillCooldowns["catSkill_1_1"] > 0);
        SetButtonDimmed(catSkill2Button, p1LockAll || p1OnlySkill1 || selectedSkillNames[1].Contains("catSkill_2") || skillCooldowns["catSkill_2_1"] > 0);
        SetButtonDimmed(catSkill3Button, p1LockAll || p1OnlySkill1 || selectedSkillNames[1].Contains("catSkill_3") || skillCooldowns["catSkill_3_1"] > 0);
        SetButtonDimmed(catSkill4Button, p1LockAll || p1OnlySkill1 || selectedSkillNames[1].Contains("catSkill_4") || skillCooldowns["catSkill_4_1"] > 0);
        SetButtonDimmed(catSkill5Button, p1LockAll || p1OnlySkill1 || selectedSkillNames[1].Contains("catSkill_5") || skillCooldowns["catSkill_5_1"] > 0);
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
        UpdateAllSkillButtonStates();
    }

    public bool CheckSkillCooldownAndShowMessage(int playerIndex, string skillName, string displayName = null)
    {
        if (displayName == null) displayName = skillName.Replace("Skill_", "").Replace("catSkill_", "");
        string cooldownKey = $"{skillName}_{playerIndex}";
        if (skillCooldowns.ContainsKey(cooldownKey) && skillCooldowns[cooldownKey] > 0)
        {
            dialogueText.text = $"技能 <b>{displayName}</b> 正在冷卻中，剩餘 <b>{skillCooldowns[cooldownKey]}</b> 回合！";
            return true;
        }
        return false;
    }

    public void SelectTarget(int playerIndex, Unit target)
    {
        if (target != null && (enemyUnits.Contains(target) || playerUnits.Contains(target)) && !target.isDead)
        {
            selectedTargets[playerIndex] = target;
            dialogueText.text = $"<b>{playerUnits[playerIndex].unitName}</b> 已選擇目標：<b>{target.unitName}</b>";
            HighlightTarget(target, true);

            foreach (var unit in enemyUnits) if (unit != target && !unit.isDead) HighlightTarget(unit, false);
            foreach (var unit in playerUnits) if (unit != target && !unit.isDead) HighlightTarget(unit, false);
        }
        else
        {
            selectedTargets[playerIndex] = null;
            dialogueText.text = $"<b>{playerUnits[playerIndex].unitName}</b>：無效的目標，請點擊一個存活的目標！";
        }
    }

    public void HighlightTarget(Unit target, bool isHighlighted)
    {
        if (target == null || target.isDead) return;

        SpriteRenderer renderer = target.GetComponent<SpriteRenderer>();
        Image image = target.GetComponent<Image>();
        if (renderer != null)
            renderer.color = isHighlighted ? Color.yellow : Color.white;
        else if (image != null)
            image.color = isHighlighted ? Color.yellow : Color.white;
    }

    public void ClearAllHighlights()
    {
        foreach (int playerIndex in new List<int>(selectedTargets.Keys))
        {
            if (selectedTargets[playerIndex] != null)
            {
                HighlightTarget(selectedTargets[playerIndex], false);
                selectedTargets[playerIndex] = null;
            }
        }
        selectedTargets.Clear();
        if (dialogueText != null) dialogueText.text = "";
    }

    public void OnPlayerHeadClick(int playerIndex)
    {
        if (playerUnits == null || playerIndex < 0 || playerIndex >= playerUnits.Count) return;
        Unit selectedTarget = playerUnits[playerIndex];
        if (selectedTarget == null || selectedTarget.isDead)
        {
            dialogueText.text = "請選擇一個存活的玩家目標！";
            return;
        }

        bool isWaitingForPlayerTarget = pendingSkill == "catSkill_4" || pendingSkill == "Can" || pendingSkill == "Grass" || pendingSkill == "Drying";
        if (!isWaitingForPlayerTarget || pendingPlayerIndex < 0) return;

        dialogueText.text = $"<b>{playerUnits[pendingPlayerIndex].unitName}</b> 選擇對 <b>{selectedTarget.unitName}</b> 使用 <b>{pendingSkill}</b>";
        SelectTarget(pendingPlayerIndex, selectedTarget);
        ExecutePendingSkill();
    }

    public bool ValidateTarget(int playerIndex, Unit target)
    {
        if (target == null || target.isDead)
        {
            dialogueText.text = $"<b>{playerUnits[playerIndex].unitName}</b>：請點擊一個有效的目標！";
            return false;
        }
        return true;
    }

    public bool ValidateSkill(int playerIndex, string skillName)
    {
        if (!CanUseSkill(playerIndex)) return false;

        if (selectedSkillNames[playerIndex].Contains(skillName))
        {
            CancelSkill(playerIndex, skillName);
            return false;
        }

        if (GetTotalSelectedSkills() + GetSkillCost(skillName) > maxSkillPerTurn)
        {
            dialogueText.text = "AP不足！";
            return false;
        }

        string cooldownKey = $"{skillName}_{playerIndex}";
        if (!skillCooldowns.ContainsKey(cooldownKey)) skillCooldowns[cooldownKey] = 0;
        if (skillCooldowns[cooldownKey] > 0) return false;

        return true;
    }

    private void ExecutePendingSkill()
    {
        if (!isWaitingForTarget || pendingSkill == null || pendingPlayerIndex < 0 || !selectedTargets.ContainsKey(pendingPlayerIndex)) return;

        Unit target = selectedTargets[pendingPlayerIndex];
        if (!ValidateTarget(pendingPlayerIndex, target)) return;

        switch (pendingSkill)
        {
            case "Skill_1": Skill_1(pendingPlayerIndex, target); break;
            case "Skill_5": Skill_5(pendingPlayerIndex, target); break;
            case "catSkill_1": catSkill_1(pendingPlayerIndex, target); break;
            case "catSkill_4": catSkill_4(pendingPlayerIndex, target); break;
            case "Can": itemuse.UseCan(pendingPlayerIndex, target); break;
            case "Grass": itemuse.UseGrass(pendingPlayerIndex, target); break;
            case "Drying": itemuse.UseDrying(pendingPlayerIndex, target); break;
            default: Debug.LogError($"Unknown pending skill/item: {pendingSkill}"); break;
        }

        isWaitingForTarget = false;
        pendingSkill = null;
        pendingPlayerIndex = -1;
    }

    private IEnumerator Skill1Coroutine(int playerIndex, Unit target)
    {
        if (target == null || target.isDead || !enemyUnits.Contains(target)) yield break;

        dialogueText.text = $"<b>{playerUnits[playerIndex].unitName}</b> 使出 <b>吃我一擊！</b>\n" +
                            $"甩出手上的球攻擊了 <b>{target.unitName}</b>";

        if (enemyUnits.Count == 1)
        {
            EffectManager.Instance.PlayUIEffect("B1", new Vector2(0, 0));
        }
        else
        {
            int targetIndexForEffect = enemyUnits.IndexOf(target);
            bool isTargetLeft = (targetIndexForEffect == 0);
            EffectManager.Instance.PlayUIEffectOneSide("B1", isTargetLeft);
        }

        yield return new WaitForSeconds(0.5f);
        PlaySoundByName("skill1");
        yield return new WaitForSeconds(1f);

        int buffedDamage = Mathf.RoundToInt(playerUnits[playerIndex].damage * 1.2f);
        int damageDealt;
        bool isKilled = target.TakeDamage(buffedDamage, out damageDealt);

        int targetIndex = enemyUnits.IndexOf(target);
        if (targetIndex >= 0 && targetIndex < enemyHUDs.Count && enemyHUDs[targetIndex] != null)
        {
            enemyHUDs[targetIndex].SetHP(target.currentHP);
        }

        dialogueText.text += $"\n<b>{target.unitName}</b> 受到了 <b>{damageDealt}</b> 點傷害！";

        yield return new WaitForSeconds(0.8f);

        if (EquipmentManager.Instance != null && !isKilled)
        {
            yield return StartCoroutine(EquipmentManager.Instance.ExecuteTapeGlovesExtraDamage(playerIndex, target));
        }
    }

    private IEnumerator Skill2Coroutine(int playerIndex)
    {
        dialogueText.text = $"<b>布萊克</b> 使出了 <b>上吧，球球們</b>！\n用手上的球分散甩出攻擊了全部敵人";
        skillCooldowns["Skill_2_" + playerIndex] = skill2CooldownTurns;

        EffectManager.Instance.PlayUIEffect("B2", new Vector2(0, 0));
        PlaySoundByName("skill2");

        yield return new WaitForSeconds(1.5f);

        int buffedDamage = Mathf.RoundToInt(playerUnits[playerIndex].damage * 1.5f);
        int totalDamageDealt = 0;

        foreach (Unit enemy in enemyUnits)
        {
            if (enemy == null || enemy.isDead) continue;
            int damageDealt;
            bool isKilled = enemy.TakeDamage(buffedDamage, out damageDealt);
            totalDamageDealt += damageDealt;

            int idx = enemyUnits.IndexOf(enemy);
            if (idx >= 0 && idx < enemyHUDs.Count && enemyHUDs[idx] != null)
            {
                enemyHUDs[idx].SetHP(enemy.currentHP);
            }
        }

        dialogueText.text += $"\n總共造成 <b>{totalDamageDealt}</b> 點傷害！";
        yield return new WaitForSeconds(1.0f);
    }

    private IEnumerator Skill3Coroutine(int playerIndex)
    {
        dialogueText.text = $"<b>布萊克</b> 使出了 <b>舔舔舔</b>！\n將身上的毛整理好，使防禦力上升。";
        skillCooldowns["Skill_3_" + playerIndex] = skill3CooldownTurns;

        EffectManager.Instance.PlayUIEffect("B3", new Vector2(-802, -362));
        PlaySoundByName("skill3");

        yield return new WaitForSeconds(1.5f);

        buffDebuffManager.ApplyDefenseBuff(playerUnits[playerIndex], 1.5f, 1);
    }

    private IEnumerator Skill4Coroutine(int playerIndex)
    {
        dialogueText.text = $"<b>布萊克</b> 使出了 <b>大雞腿</b>～\n緊縮成一團，使敵人找不到縫隙攻擊。";
        skillCooldowns["Skill_4_" + playerIndex] = skill4CooldownTurns;

        EffectManager.Instance.PlayUIEffect("B4", new Vector2(-802, -362));
        PlaySoundByName("Skill5");

        yield return new WaitForSeconds(1.5f);

        buffDebuffManager.ApplyDefenseBuff(playerUnits[playerIndex], 2.0f, 2);
    }

    private IEnumerator Skill5Coroutine(int playerIndex, Unit target)
    {
        if (playerUnits[playerIndex] == null || playerUnits[playerIndex].currentHP <= 0) yield break;
        if (target == null || target.isDead || !enemyUnits.Contains(target))
        {
            skillCooldowns["Skill_5_" + playerIndex] = skill5CooldownTurns;
            yield break;
        }

        skillCooldowns["Skill_5_" + playerIndex] = skill5CooldownTurns;

        dialogueText.text = $"<b>布萊克</b> 使出了 <b>連環兔子踹</b>！\n抱住 <b>{target.unitName}</b> ，使出最強攻擊兔子踹！";

        if (enemyUnits.Count == 1)
        {
            EffectManager.Instance.PlayUIEffect("B5", new Vector2(0, 0));
        }
        else
        {
            int targetIndexForEffect = enemyUnits.IndexOf(target);
            bool isTargetLeft = (targetIndexForEffect == 0);
            EffectManager.Instance.PlayUIEffectOneSide("B5", isTargetLeft);
        }

        PlaySoundByName("skill4-1");
        yield return new WaitForSeconds(0.5f);
        PlaySoundByName("skill4-2");
        yield return new WaitForSeconds(1.0f);

        buffDebuffManager.ApplyDefenseDebuff(target, 0.7f, 2);

        int buffedDamage = Mathf.RoundToInt(playerUnits[playerIndex].damage * 1.3f);
        int damageDealt;
        bool isKilled = target.TakeDamage(buffedDamage, out damageDealt);

        int targetIndex = enemyUnits.IndexOf(target);
        if (targetIndex >= 0 && targetIndex < enemyHUDs.Count && enemyHUDs[targetIndex] != null)
        {
            enemyHUDs[targetIndex].SetHP(target.currentHP);
        }

        dialogueText.text += $"\n<b>{target.unitName}</b> 受到了 <b>{damageDealt}</b> 點傷害！";

        yield return new WaitForSeconds(1.0f);

        if (EquipmentManager.Instance != null && !isKilled)
        {
            yield return StartCoroutine(EquipmentManager.Instance.ExecuteTapeGlovesExtraDamage(playerIndex, target));
        }
    }

    private IEnumerator CatSkill1Coroutine(int playerIndex, Unit target)
    {
        if (target == null || target.isDead || !enemyUnits.Contains(target)) yield break;

        dialogueText.text = $"<b>萊拉</b> 使出了 <b>三分球</b>！\n撿起身邊的東西丟向 <b>{target.unitName}</b>！";
        EffectManager.Instance.PlayUIEffect("L1", new Vector2(0, 0));
        PlaySoundByName("skill1");

        yield return new WaitForSeconds(1.5f);

        int buffedDamage = Mathf.RoundToInt(playerUnits[playerIndex].damage * 1f);
        int damageDealt;
        bool isKilled = target.TakeDamage(buffedDamage, out damageDealt);

        int targetIndex = enemyUnits.IndexOf(target);
        if (targetIndex >= 0 && targetIndex < enemyHUDs.Count && enemyHUDs[targetIndex] != null)
        {
            enemyHUDs[targetIndex].SetHP(target.currentHP);
        }

        dialogueText.text += $"\n<b>{target.unitName}</b> 受到了 <b>{damageDealt}</b> 點傷害！";
        yield return new WaitForSeconds(1.0f);
    }

    private IEnumerator CatSkill2Coroutine(int playerIndex)
    {
        skillCooldowns["catSkill_2_" + playerIndex] = catskill2CooldownTurns;
        dialogueText.text = $"<b>萊拉</b> 使出了 <b>小意思</b>！\n將傷口進行包紮";
        EffectManager.Instance.PlayUIEffect("L2", new Vector2(800, 0));
        EffectManager.Instance.PlayUIEffect("L2", new Vector2(-800, 0));

        yield return new WaitForSeconds(1.5f);

        foreach (Unit player in playerUnits)
        {
            if (player != null && !player.isDead)
            {
                buffDebuffManager.Heal(player, 0.75f);
                PlaySoundByName("catskill2");
            }
        }

        yield return new WaitForSeconds(1.0f);
    }

    private IEnumerator CatSkill3Coroutine(int playerIndex)
    {
        if (playerUnits[playerIndex] == null || playerUnits[playerIndex].currentHP <= 0)
        {
            skillCooldowns["catSkill_3_" + playerIndex] = catskill3CooldownTurns;
            yield break;
        }

        skillCooldowns["catSkill_3_" + playerIndex] = catskill3CooldownTurns;
        dialogueText.text = $"<b>萊拉</b> 使出了 <b>Do～Re～Mi～</b>\n透過唱歌給全體敵人造成傷害！";
        EffectManager.Instance.PlayUIEffect("L3", new Vector2(0, 0));
        PlaySoundByName("catskill3");

        yield return new WaitForSeconds(2.0f);

        int buffedDamage = Mathf.RoundToInt(playerUnits[playerIndex].damage * 1f);
        int totalDamageDealt = 0;

        foreach (Unit enemy in enemyUnits)
        {
            if (enemy == null || enemy.isDead) continue;
            int damageDealt;
            bool isKilled = enemy.TakeDamage(buffedDamage, out damageDealt);
            totalDamageDealt += damageDealt;

            int idx = enemyUnits.IndexOf(enemy);
            if (idx >= 0 && idx < enemyHUDs.Count && enemyHUDs[idx] != null)
            {
                enemyHUDs[idx].SetHP(enemy.currentHP);
            }
        }

        dialogueText.text += $"\n總共造成 <b>{totalDamageDealt}</b> 點傷害！";
        yield return new WaitForSeconds(1.0f);
    }

    private IEnumerator CatSkill4Coroutine(int playerIndex, Unit target)
    {
        if (playerUnits[playerIndex] == null || playerUnits[playerIndex].currentHP <= 0)
        {
            skillCooldowns["catSkill_4_" + playerIndex] = catskill4CooldownTurns;
            yield break;
        }
        if (target == null || target.isDead || !playerUnits.Contains(target))
        {
            skillCooldowns["catSkill_4_" + playerIndex] = catskill4CooldownTurns;
            yield break;
        }

        skillCooldowns["catSkill_4_" + playerIndex] = catskill4CooldownTurns;
        dialogueText.text = $"<b>萊拉</b> 使出了 <b>打起精神來</b>！\n對 <b>{target.unitName}</b> 施展魔法，提升其攻擊力";

        int targetIndex = playerUnits.IndexOf(target);
        bool isTargetLeft = (targetIndex == 0);
        EffectManager.Instance.PlayUIEffectOneSide("L4", isTargetLeft);

        PlaySoundByName("catskill4_1");
        yield return new WaitForSeconds(0.5f);
        PlaySoundByName("catskill4_2");
        yield return new WaitForSeconds(1.0f);

        buffDebuffManager.ApplyDamageBuff(target, 2.0f, 2);
    }

    private IEnumerator CatSkill5Coroutine(int playerIndex)
    {
        skillCooldowns["catSkill_5_" + playerIndex] = catskill5CooldownTurns;
        dialogueText.text = $"<b>萊拉</b> 使出了 <b>痛痛飛走</b>～\n透過優美的歌聲振奮士氣，使我方全體承受傷害降低";
        EffectManager.Instance.PlayUIEffect("L5", new Vector2(0, 0));
        PlaySoundByName("catskill5");

        yield return new WaitForSeconds(2.0f);

        foreach (Unit player in playerUnits)
        {
            buffDebuffManager.ApplyFixedDamageReduction(player, 0.7f, 1);
        }
    }

    // ==================== 技能選擇 ====================
    public void Skill_1(int playerIndex, Unit target)
    {
        if (playerIndex != 0 || !ValidateSkill(playerIndex, "Skill_1") || !ValidateTarget(playerIndex, target)) return;

        AddSkillToQueue(playerIndex, "Skill_1", true, Skill1Coroutine(playerIndex, target));
        dialogueText.text = $"<b>吃我一擊</b>！\n甩出手上的球攻擊\nCD : 0   消耗AP : 1";

        RemoveSkillIcon(GetSkillCost("Skill_1"));
        UpdateAllSkillButtonStates();
    }

    public void Skill_2(int playerIndex)
    {
        if (playerIndex != 0 || !ValidateSkill(playerIndex, "Skill_2")) return;

        AddSkillToQueue(playerIndex, "Skill_2", false, Skill2Coroutine(playerIndex));
        dialogueText.text = $"<b>上吧，球球們</b>！\n手上的球分散甩出攻擊全部敵人\nCD : 2   消耗AP : 1";

        RemoveSkillIcon(GetSkillCost("Skill_2"));
        UpdateAllSkillButtonStates();
    }

    public void Skill_3(int playerIndex)
    {
        if (playerIndex != 0 || !ValidateSkill(playerIndex, "Skill_3")) return;

        AddSkillToQueue(playerIndex, "Skill_3", false, Skill3Coroutine(playerIndex));
        dialogueText.text = $"<b>舔舔舔</b>！\n將身上的毛整理好，使防禦力上升。\nCD : 1   消耗AP : 1";

        RemoveSkillIcon(GetSkillCost("Skill_3"));
        UpdateAllSkillButtonStates();
    }

    public void Skill_4(int playerIndex)
    {
        if (playerIndex != 0 || !ValidateSkill(playerIndex, "Skill_4")) return;

        AddSkillToQueue(playerIndex, "Skill_4", false, Skill4Coroutine(playerIndex));
        dialogueText.text = $"<b>大雞腿</b>～\n緊縮成一團，使敵人找不到縫隙攻擊。\nCD : 2   消耗AP : 2";
        buffDebuffManager.skill4BuffUnits.Add(playerUnits[playerIndex]);

        RemoveSkillIcon(GetSkillCost("Skill_4"));
        UpdateAllSkillButtonStates();
    }

    public void Skill_5(int playerIndex, Unit target)
    {
        if (!ValidateSkill(playerIndex, "Skill_5") || !ValidateTarget(playerIndex, target)) return;

        AddSkillToQueue(playerIndex, "Skill_5", true, Skill5Coroutine(playerIndex, target));
        dialogueText.text = $"<b>連環兔子踹</b>！\n 抱住敵人，使出最強攻擊兔子踹！\nCD : 2   消耗AP : 3";

        RemoveSkillIcon(GetSkillCost("Skill_5"));
        UpdateAllSkillButtonStates();
    }

    public void catSkill_1(int playerIndex, Unit target)
    {
        if (playerIndex != 1 || !ValidateSkill(playerIndex, "catSkill_1") || !ValidateTarget(playerIndex, target)) return;

        AddSkillToQueue(playerIndex, "catSkill_1", true, CatSkill1Coroutine(playerIndex, target));
        dialogueText.text = $"<b>三分球</b>！\n 撿起身邊的東西丟向敵人。\nCD : 0   消耗AP : 1";

        RemoveSkillIcon(GetSkillCost("catSkill_1"));
        UpdateAllSkillButtonStates();
    }

    public void catSkill_2(int playerIndex)
    {
        if (playerIndex != 1 || !ValidateSkill(playerIndex, "catSkill_2")) return;

        AddSkillToQueue(playerIndex, "catSkill_2", false, CatSkill2Coroutine(playerIndex));
        dialogueText.text = $"<b>小意思</b>！\n將傷口進行包紮。\nCD : 2   消耗AP : 1";

        RemoveSkillIcon(GetSkillCost("catSkill_2"));
        UpdateAllSkillButtonStates();
    }

    public void catSkill_3(int playerIndex)
    {
        if (!ValidateSkill(playerIndex, "catSkill_3")) return;

        AddSkillToQueue(playerIndex, "catSkill_3", false, CatSkill3Coroutine(playerIndex));
        dialogueText.text = $"<b>Do～Re～Mi～</b>\n 大聲歌唱，干擾全部敵人。\nCD : 1   消耗AP : 1";

        RemoveSkillIcon(GetSkillCost("catSkill_3"));
        UpdateAllSkillButtonStates();
    }

    public void catSkill_4(int playerIndex, Unit target)
    {
        if (!ValidateSkill(playerIndex, "catSkill_4") || !ValidateTarget(playerIndex, target)) return;

        AddSkillToQueue(playerIndex, "catSkill_4", true, CatSkill4Coroutine(playerIndex, target));
        dialogueText.text = $"<b>打起精神來</b>！\n 施展力量變強壯的魔法。\nCD : 2   消耗AP : 2";

        RemoveSkillIcon(GetSkillCost("catSkill_4"));
        UpdateAllSkillButtonStates();
    }

    public void catSkill_5(int playerIndex)
    {
        if (playerIndex != 1 || !ValidateSkill(playerIndex, "catSkill_5")) return;

        AddSkillToQueue(playerIndex, "catSkill_5", false, CatSkill5Coroutine(playerIndex));
        dialogueText.text = $"<b>痛痛飛走</b>～\n 優美的歌聲，振奮隊伍士氣。\nCD : 3   消耗AP : 3";

        RemoveSkillIcon(GetSkillCost("catSkill_5"));
        UpdateAllSkillButtonStates();
    }

    // ==================== Trigger 技能按鈕 ====================
    public void TriggerSkill1ForPlayer0()
    {
        ClearAllHighlights();
        if (battlesystem.state != BattleState.PLAYERTURN) return;
        if (!CanUseSkill(0)) return;

        string skillID = "Skill_1";
        if (selectedSkillNames[0].Contains(skillID))
        {
            CancelSkill(0, skillID);
            SetButtonDimmed(skill1Button, false);
            return;
        }

        if (GetTotalSelectedSkills() + GetSkillCost(skillID) > maxSkillPerTurn)
        {
            dialogueText.text = "AP不足！無法使用此技能。";
            return;
        }

        isWaitingForTarget = true;
        pendingSkill = skillID;
        pendingPlayerIndex = 0;

        if (playerUnits[0].equippedGear.Contains("穿戴式紙箱盔甲") || (EquipmentManager.Instance != null && EquipmentManager.Instance.HasEquipment("穿戴式紙箱盔甲")))
        {
            dialogueText.text = "因為穿戴式紙箱盔甲，只能使用技能1！\n請點擊一個敵人目標！";
        }
        else
        {
            dialogueText.text = $"請點擊一個敵人目標以使用 <b>{GetSkillDisplayName(skillID)}</b>！";
        }

        SetButtonDimmed(skill1Button, true);
    }

    public void TriggerSkill2ForPlayer0()
    {
        ClearAllHighlights();
        if (battlesystem.state != BattleState.PLAYERTURN) return;
        if (!CanUseSkill(0)) return;

        if (playerUnits[0].equippedGear.Contains("穿戴式紙箱盔甲") || (EquipmentManager.Instance != null && EquipmentManager.Instance.HasEquipment("穿戴式紙箱盔甲")))
        {
            dialogueText.text = "因為穿戴式紙箱盔甲，無法使用此技能！";
            return;
        }

        if (CheckSkillCooldownAndShowMessage(0, "Skill_2", "2")) return;

        string skillID = "Skill_2";
        if (selectedSkillNames[0].Contains(skillID))
        {
            CancelSkill(0, skillID);
            SetButtonDimmed(skill2Button, false);
            return;
        }

        if (GetTotalSelectedSkills() + GetSkillCost(skillID) > maxSkillPerTurn)
        {
            dialogueText.text = "AP不足！無法使用此技能。";
            return;
        }

        Skill_2(0);
        SetButtonDimmed(skill2Button, true);
    }

    public void TriggerSkill3ForPlayer0()
    {
        ClearAllHighlights();
        if (battlesystem.state != BattleState.PLAYERTURN) return;
        if (!CanUseSkill(0)) return;

        if (playerUnits[0].equippedGear.Contains("穿戴式紙箱盔甲") || (EquipmentManager.Instance != null && EquipmentManager.Instance.HasEquipment("穿戴式紙箱盔甲")))
        {
            dialogueText.text = "因為穿戴式紙箱盔甲，無法使用此技能！";
            return;
        }

        if (CheckSkillCooldownAndShowMessage(0, "Skill_3", "3")) return;

        string skillID = "Skill_3";
        if (selectedSkillNames[0].Contains(skillID))
        {
            CancelSkill(0, skillID);
            SetButtonDimmed(skill3Button, false);
            return;
        }

        if (GetTotalSelectedSkills() + GetSkillCost(skillID) > maxSkillPerTurn)
        {
            dialogueText.text = "AP不足！無法使用此技能。";
            return;
        }

        Skill_3(0);
        SetButtonDimmed(skill3Button, true);
    }

    public void TriggerSkill4ForPlayer0()
    {
        ClearAllHighlights();
        if (battlesystem.state != BattleState.PLAYERTURN) return;
        if (!CanUseSkill(0)) return;

        if (playerUnits[0].equippedGear.Contains("穿戴式紙箱盔甲") || (EquipmentManager.Instance != null && EquipmentManager.Instance.HasEquipment("穿戴式紙箱盔甲")))
        {
            dialogueText.text = "因為穿戴式紙箱盔甲，無法使用此技能！";
            return;
        }

        if (CheckSkillCooldownAndShowMessage(0, "Skill_4", "4")) return;

        string skillID = "Skill_4";
        if (selectedSkillNames[0].Contains(skillID))
        {
            CancelSkill(0, skillID);
            SetButtonDimmed(skill4Button, false);
            return;
        }

        if (GetTotalSelectedSkills() + GetSkillCost(skillID) > maxSkillPerTurn)
        {
            dialogueText.text = "AP不足！無法使用此技能。";
            return;
        }

        Skill_4(0);
        SetButtonDimmed(skill4Button, true);
    }

    public void TriggerSkill5ForPlayer0()
    {
        ClearAllHighlights();
        if (battlesystem.state != BattleState.PLAYERTURN) return;
        if (!CanUseSkill(0)) return;

        if (playerUnits[0].equippedGear.Contains("穿戴式紙箱盔甲") || (EquipmentManager.Instance != null && EquipmentManager.Instance.HasEquipment("穿戴式紙箱盔甲")))
        {
            dialogueText.text = "因為穿戴式紙箱盔甲，無法使用此技能！";
            return;
        }

        if (CheckSkillCooldownAndShowMessage(0, "Skill_5", "5")) return;

        string skillID = "Skill_5";
        if (selectedSkillNames[0].Contains(skillID))
        {
            CancelSkill(0, skillID);
            SetButtonDimmed(skill5Button, false);
            return;
        }

        if (GetTotalSelectedSkills() + GetSkillCost(skillID) > maxSkillPerTurn)
        {
            dialogueText.text = "AP不足！無法使用此技能。";
            return;
        }

        isWaitingForTarget = true;
        pendingSkill = skillID;
        pendingPlayerIndex = 0;
        dialogueText.text = $"請點擊一個敵人目標以使用 <b>{GetSkillDisplayName(skillID)}</b>！";
        SetButtonDimmed(skill5Button, true);
    }

    public void TriggerCatSkill1ForPlayer1()
    {
        ClearAllHighlights();
        if (battlesystem.state != BattleState.PLAYERTURN) return;
        if (!CanUseSkill(1)) return;

        string skillID = "catSkill_1";
        if (selectedSkillNames[1].Contains(skillID))
        {
            CancelSkill(1, skillID);
            SetButtonDimmed(catSkill1Button, false);
            return;
        }

        if (GetTotalSelectedSkills() + GetSkillCost(skillID) > maxSkillPerTurn)
        {
            dialogueText.text = "AP不足！無法使用此技能。";
            return;
        }

        isWaitingForTarget = true;
        pendingSkill = skillID;
        pendingPlayerIndex = 1;
        dialogueText.text = $"請點擊一個敵人目標以使用 <b>{GetSkillDisplayName(skillID)}</b>！";
        SetButtonDimmed(catSkill1Button, true);
    }

    public void TriggerCatSkill2ForPlayer1()
    {
        ClearAllHighlights();
        if (battlesystem.state != BattleState.PLAYERTURN) return;
        if (!CanUseSkill(1)) return;
        if (CheckSkillCooldownAndShowMessage(1, "catSkill_2", "2")) return;

        string skillID = "catSkill_2";
        if (selectedSkillNames[1].Contains(skillID))
        {
            CancelSkill(1, skillID);
            SetButtonDimmed(catSkill2Button, false);
            return;
        }

        if (GetTotalSelectedSkills() + GetSkillCost(skillID) > maxSkillPerTurn)
        {
            dialogueText.text = "AP不足！無法使用此技能。";
            return;
        }

        catSkill_2(1);
        SetButtonDimmed(catSkill2Button, true);
    }

    public void TriggerCatSkill3ForPlayer1()
    {
        ClearAllHighlights();
        if (battlesystem.state != BattleState.PLAYERTURN) return;
        if (!CanUseSkill(1)) return;
        if (CheckSkillCooldownAndShowMessage(1, "catSkill_3", "3")) return;

        string skillID = "catSkill_3";
        if (selectedSkillNames[1].Contains(skillID))
        {
            CancelSkill(1, skillID);
            SetButtonDimmed(catSkill3Button, false);
            return;
        }

        if (GetTotalSelectedSkills() + GetSkillCost(skillID) > maxSkillPerTurn)
        {
            dialogueText.text = "AP不足！無法使用此技能。";
            return;
        }

        catSkill_3(1);
        SetButtonDimmed(catSkill3Button, true);
    }

    public void TriggerCatSkill4ForPlayer1()
    {
        ClearAllHighlights();
        if (battlesystem.state != BattleState.PLAYERTURN) return;
        if (!CanUseSkill(1)) return;
        if (CheckSkillCooldownAndShowMessage(1, "catSkill_4", "4")) return;

        string skillID = "catSkill_4";
        if (selectedSkillNames[1].Contains(skillID))
        {
            CancelSkill(1, skillID);
            SetButtonDimmed(catSkill4Button, false);
            return;
        }

        if (GetTotalSelectedSkills() + GetSkillCost(skillID) > maxSkillPerTurn)
        {
            dialogueText.text = "AP不足！無法使用此技能。";
            return;
        }

        isWaitingForTarget = true;
        pendingSkill = skillID;
        pendingPlayerIndex = 1;
        dialogueText.text = $"請點擊一個玩家頭像以使用 <b>{GetSkillDisplayName(skillID)}</b>！";
        SetButtonDimmed(catSkill4Button, true);
    }

    public void TriggerCatSkill5ForPlayer1()
    {
        ClearAllHighlights();
        if (battlesystem.state != BattleState.PLAYERTURN) return;
        if (!CanUseSkill(1)) return;
        if (CheckSkillCooldownAndShowMessage(1, "catSkill_5", "5")) return;

        string skillID = "catSkill_5";
        if (selectedSkillNames[1].Contains(skillID))
        {
            CancelSkill(1, skillID);
            SetButtonDimmed(catSkill5Button, false);
            return;
        }

        if (GetTotalSelectedSkills() + GetSkillCost(skillID) > maxSkillPerTurn)
        {
            dialogueText.text = "AP不足！無法使用此技能。";
            return;
        }

        catSkill_5(1);
        SetButtonDimmed(catSkill5Button, true);
    }

    // ==================== 執行與其他功能 ====================
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

        StartCoroutine(ExecuteAllPlayerQueues());
    }

    // ==============================================================
    // ★ 修改：真正的「全域按順序」執行器
    // ==============================================================
    private IEnumerator ExecuteAllPlayerQueues()
    {
        int[] playerActionIndex = new int[2] { 0, 0 };

        // 依照你「點擊的順序」(發放的號碼牌) 依序播放
        foreach (int pIndex in executionOrderTracker)
        {
            int actionIdx = playerActionIndex[pIndex];

            // 雙重保險防呆
            if (actionIdx < skillQueues[pIndex].Count)
            {
                if (playerUnits[pIndex] != null && !playerUnits[pIndex].isDead)
                {
                    yield return StartCoroutine(skillQueues[pIndex][actionIdx]);
                }
                playerActionIndex[pIndex]++;
            }
        }

        // 執行完畢，把所有佇列跟號碼牌清空
        executionOrderTracker.Clear();
        for (int i = 0; i < 2; i++)
        {
            skillQueues[i].Clear();
            selectedSkillNames[i].Clear();
            skillNeedsTarget[i].Clear();
        }

        if (battlesystem != null)
        {
            battlesystem.OnPlayerConfirmSkillQueue();
        }
    }

    public void RemoveSkillIcon(int cost = 1)
    {
        for (int i = 0; i < cost; i++)
        {
            if (skillIcons.Count > 0)
            {
                Destroy(skillIcons[skillIcons.Count - 1]);
                skillIcons.RemoveAt(skillIcons.Count - 1);
            }
        }
    }

    public void AddSkillIcon(int cost = 1)
    {
        for (int i = 0; i < cost; i++)
        {
            if (skillIcons.Count < maxSkillPerTurn)
            {
                GameObject icon = Instantiate(skillIconPrefab, skillIconParent);
                icon.SetActive(true);
                skillIcons.Add(icon);
            }
        }
    }

    public void ReduceCooldown()
    {
        foreach (var key in new List<string>(skillCooldowns.Keys))
        {
            if (skillCooldowns[key] > 0)
                skillCooldowns[key]--;
        }
        UpdateAllSkillButtonStates();
    }

    private string GetSkillDisplayName(string skillKey)
    {
        if (skillDisplayNameMap.ContainsKey(skillKey))
        {
            return skillDisplayNameMap[skillKey];
        }
        return skillKey;
    }

    void ResetSkillIcons()
    {
        if (skillIconPrefab == null) return;
        if (skillIconParent == null) return;

        foreach (GameObject icon in skillIcons) Destroy(icon);
        skillIcons.Clear();

        for (int i = 0; i < maxSkillPerTurn; i++)
        {
            GameObject icon = Instantiate(skillIconPrefab, skillIconParent);
            icon.SetActive(true);
            skillIcons.Add(icon);
        }
    }

    public void InitSkillIcons()
    {
        if (EquipmentManager.Instance != null && EquipmentManager.Instance.HasEquipment("劍客靴子"))
        {
            maxSkillPerTurn = 5;
        }
        else
        {
            maxSkillPerTurn = 3;
        }

        executionOrderTracker.Clear(); // ★ 確保回合開始時號碼牌是空的
        ResetSkillIcons();
        ResetAllSkillButtonStates();
    }

    public void ClearSkillIcons()
    {
        foreach (GameObject icon in skillIcons) Destroy(icon);
        skillIcons.Clear();
    }

    public void PlaySoundByName(string soundName)
    {
        AudioClip clip = Resources.Load<AudioClip>("BGM/soundeffects/" + soundName);
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
        else
            Debug.LogWarning("音效找不到: " + soundName);
    }

    private bool CanUseSkill(int playerIndex)
    {
        if (buffDebuffManager == null) return true;
        Unit unit = playerUnits[playerIndex];
        if (unit == null) return false;

        if (unit.isDead || unit.currentHP <= 0)
        {
            if (dialogueText != null)
                dialogueText.text = $"<b>{unit.unitName}</b> 無法戰鬥！";
            return false;
        }

        if (buffDebuffManager.IsSkillDisabled(unit))
        {
            if (dialogueText != null)
                dialogueText.text = $"<b>{unit.unitName}</b> 被技能封印，無法使用技能！";
            return false;
        }
        return true;
    }

    public void ResetAllCooldowns()
    {
        if (skillCooldowns == null) return;
        List<string> keys = new List<string>(skillCooldowns.Keys);
        foreach (string key in keys)
        {
            skillCooldowns[key] = 0;
        }
        Debug.Log("所有玩家技能冷卻時間已重置為 0");
    }

    public void OnCanButtonClick() => itemuse.TriggerCanForPlayer(0);
    public void OnTunaButtonClick() => itemuse.TriggerTunaForPlayer(0);
    public void OnGrassButtonClick() => itemuse.TriggerGrassForPlayer(0);
    public void OnDryingButtonClick() => itemuse.TriggerDryingForPlayer(0);
}