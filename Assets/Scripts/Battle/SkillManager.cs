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

    // 改為儲存 IEnumerator 協程
    public Dictionary<int, List<IEnumerator>> skillQueues = new Dictionary<int, List<IEnumerator>>();
    public Dictionary<int, List<string>> selectedSkillNames = new Dictionary<int, List<string>>();
    public Dictionary<int, List<bool>> skillNeedsTarget = new Dictionary<int, List<bool>>();
    public int maxSkillPerTurn = 3;
    public Dictionary<int, Unit> selectedTargets = new Dictionary<int, Unit>();

    public bool isWaitingForTarget = false;
    public string pendingSkill = null;
    public int pendingPlayerIndex = -1;
    public static SkillManager Instance { get; private set; }
    // 正在執行的玩家計數器
    private int executingPlayerCount = 0;

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
    int damageDealt;

    public void Start()
    {
        audioSource = GetComponent<AudioSource>();

        // 不依賴 playerUnits.Count，直接寫死初始化 2 個玩家的資料空間
        for (int i = 0; i < 2; i++)
        {
            Debug.Log($"Initializing player dictionary for index {i}");
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

                // 只接受未死亡的敵人
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

    // ==================== 新增：技能 AP 消耗判定 ====================
    public int GetSkillCost(string skillName)
    {
        // 定義四技能耗費 2 AP，五技能耗費 3 AP
        if (skillName == "Skill_4" || skillName == "catSkill_4") return 2;
        if (skillName == "Skill_5" || skillName == "catSkill_5") return 3;

        // 其他技能與道具預設耗費 1 AP
        return 1;
    }

    // 將原本計算「技能數量」的方法，改為計算「總共消耗了多少 AP」
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
    // ==============================================================

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

        int latestPlayerIndex = -1;
        int latestSkillIndex = -1;
        string latestSkillName = null;

        for (int i = 0; i < playerUnits.Count; i++)
        {
            if (selectedSkillNames[i].Count > 0)
            {
                int lastIdx = selectedSkillNames[i].Count - 1;
                if (latestPlayerIndex == -1 || lastIdx > latestSkillIndex)
                {
                    latestPlayerIndex = i;
                    latestSkillIndex = lastIdx;
                    latestSkillName = selectedSkillNames[i][lastIdx];
                }
            }
        }

        if (latestPlayerIndex == -1)
        {
            dialogueText.text = "沒有可取消的技能";
            return;
        }

        Unit playerUnit = playerUnits[latestPlayerIndex];
        string readableName = latestSkillName;
        if (skillDisplayNameMap.ContainsKey(latestSkillName))
        {
            readableName = skillDisplayNameMap[latestSkillName];
        }
        dialogueText.text = $"<b>{playerUnit.unitName}</b> 取消了 <b>{readableName}</b>";

        skillQueues[latestPlayerIndex].RemoveAt(latestSkillIndex);
        selectedSkillNames[latestPlayerIndex].RemoveAt(latestSkillIndex);
        skillNeedsTarget[latestPlayerIndex].RemoveAt(latestSkillIndex);

        if (selectedTargets.ContainsKey(latestPlayerIndex) && selectedTargets[latestPlayerIndex] != null)
        {
            HighlightTarget(selectedTargets[latestPlayerIndex], false);
            selectedTargets[latestPlayerIndex] = null;
        }

        Button buttonToRestore = GetSkillButton(latestPlayerIndex, latestSkillName);
        if (buttonToRestore != null) SetButtonDimmed(buttonToRestore, false);

        // 取消時，根據技能消耗把 AP 圖示加回來
        AddSkillIcon(GetSkillCost(latestSkillName));
        UpdateAllSkillButtonStates();
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
            button.interactable = false;
        }
        else
        {
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            colors.selectedColor = Color.white;
            button.interactable = true;
        }
        button.colors = colors;
    }

    private void SetButtonCooldownDimmed(Button button, bool inCooldown)
    {
        if (button == null) return;
        ColorBlock colors = button.colors;
        if (inCooldown)
        {
            colors.normalColor = new Color(0.5f, 0.5f, 0.5f, 1f);
            colors.highlightedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            colors.pressedColor = new Color(0.6f, 0.6f, 0.6f, 1f);
            colors.selectedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
            button.interactable = true;
        }
        else
        {
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            colors.selectedColor = Color.white;
        }
        button.colors = colors;
    }

    public void UpdateAllSkillButtonStates()
    {
        if (playerUnits == null || playerUnits.Count < 2) return;
        if (buffDebuffManager == null) return;

        try
        {
            SetButtonDimmed(skill1Button, selectedSkillNames[0].Contains("Skill_1"));
            SetButtonCooldownDimmed(skill2Button, skillCooldowns["Skill_2_0"] > 0);
            SetButtonDimmed(skill2Button, selectedSkillNames[0].Contains("Skill_2"));
            SetButtonCooldownDimmed(skill3Button, skillCooldowns["Skill_3_0"] > 0);
            SetButtonDimmed(skill3Button, selectedSkillNames[0].Contains("Skill_3"));
            SetButtonCooldownDimmed(skill4Button, skillCooldowns["Skill_4_0"] > 0);
            SetButtonDimmed(skill4Button, selectedSkillNames[0].Contains("Skill_4"));
            SetButtonCooldownDimmed(skill5Button, skillCooldowns["Skill_5_0"] > 0);
            SetButtonDimmed(skill5Button, selectedSkillNames[0].Contains("Skill_5"));

            SetButtonDimmed(catSkill1Button, selectedSkillNames[1].Contains("catSkill_1"));
            SetButtonCooldownDimmed(catSkill2Button, skillCooldowns["catSkill_2_1"] > 0);
            SetButtonDimmed(catSkill2Button, selectedSkillNames[1].Contains("catSkill_2"));
            SetButtonCooldownDimmed(catSkill3Button, skillCooldowns["catSkill_3_1"] > 0);
            SetButtonDimmed(catSkill3Button, selectedSkillNames[1].Contains("catSkill_3"));
            SetButtonCooldownDimmed(catSkill4Button, skillCooldowns["catSkill_4_1"] > 0);
            SetButtonDimmed(catSkill4Button, selectedSkillNames[1].Contains("catSkill_4"));
            SetButtonCooldownDimmed(catSkill5Button, skillCooldowns["catSkill_5_1"] > 0);
            SetButtonDimmed(catSkill5Button, selectedSkillNames[1].Contains("catSkill_5"));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"按鈕狀態更新時發生崩潰！錯誤原因: {e.Message}");
        }

        // ★ 升級檢查：同時檢查單位身上，與全域的裝備管理器
        bool p0Disabled = buffDebuffManager.IsSkillDisabled(playerUnits[0]);
        bool p1Disabled = buffDebuffManager.IsSkillDisabled(playerUnits[1]);
        bool p0OnlySkill1 = playerUnits[0].equippedGear.Contains("穿戴式紙箱盔甲") || (EquipmentManager.Instance != null && EquipmentManager.Instance.HasEquipment("穿戴式紙箱盔甲"));
        bool p1OnlySkill1 = playerUnits[1].equippedGear.Contains("穿戴式紙箱盔甲") || (EquipmentManager.Instance != null && EquipmentManager.Instance.HasEquipment("穿戴式紙箱盔甲"));

        // ★ 直接使用 SetButtonDimmed 讓按鈕徹底變灰並無法點擊
        if (p0Disabled)
        {
            SetButtonDimmed(skill1Button, true);
            SetButtonDimmed(skill2Button, true);
            SetButtonDimmed(skill3Button, true);
            SetButtonDimmed(skill4Button, true);
            SetButtonDimmed(skill5Button, true);
        }
        else if (p0OnlySkill1)
        {
            SetButtonDimmed(skill2Button, true);
            SetButtonDimmed(skill3Button, true);
            SetButtonDimmed(skill4Button, true);
            SetButtonDimmed(skill5Button, true);
        }

        if (p1Disabled)
        {
            SetButtonDimmed(catSkill1Button, true);
            SetButtonDimmed(catSkill2Button, true);
            SetButtonDimmed(catSkill3Button, true);
            SetButtonDimmed(catSkill4Button, true);
            SetButtonDimmed(catSkill5Button, true);
        }
        else if (p1OnlySkill1)
        {
            SetButtonDimmed(catSkill2Button, true);
            SetButtonDimmed(catSkill3Button, true);
            SetButtonDimmed(catSkill4Button, true);
            SetButtonDimmed(catSkill5Button, true);
        }
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

    public void CancelSkill(int playerIndex, string skillName)
    {
        if (selectedSkillNames[playerIndex].Contains(skillName))
        {
            int index = selectedSkillNames[playerIndex].IndexOf(skillName);
            selectedSkillNames[playerIndex].RemoveAt(index);
            skillQueues[playerIndex].RemoveAt(index);
            skillNeedsTarget[playerIndex].RemoveAt(index);

            if (selectedTargets.ContainsKey(playerIndex) && selectedTargets[playerIndex] != null)
            {
                HighlightTarget(selectedTargets[playerIndex], false);
                selectedTargets[playerIndex] = null;
            }

            dialogueText.text = $"<b>{playerUnits[playerIndex].unitName}</b> 取消了 <b>{skillName}</b>";

            // 取消時，根據技能消耗把 AP 圖示加回來
            AddSkillIcon(GetSkillCost(skillName));
            UpdateAllSkillButtonStates();
        }
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

        // 修改：檢查消耗的 AP 是否大於上限
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

    // ==================== 技能協程 ====================
    private IEnumerator Skill1Coroutine(int playerIndex, Unit target)
    {
        if (target == null || target.isDead || !enemyUnits.Contains(target)) yield break;
        PlaySoundByName("skill1");
        int buffedDamage = Mathf.RoundToInt(playerUnits[playerIndex].damage * 1.2f);
        int damageDealt;
        bool isKilled = target.TakeDamage(buffedDamage, out damageDealt);

        int targetIndex = enemyUnits.IndexOf(target);
        if (targetIndex >= 0 && targetIndex < enemyHUDs.Count && enemyHUDs[targetIndex] != null)
        {
            enemyHUDs[targetIndex].SetHP(target.currentHP);
        }
        yield return new WaitForSeconds(1.5f);

        dialogueText.text = $"<b>{playerUnits[playerIndex].unitName}</b> 使出 <b>吃我一擊！</b>\n" +
                            $"甩出手上的球攻擊了 <b>{target.unitName}</b>\n" +
                            $"<b>{target.unitName}</b> 受到了 <b>{damageDealt}</b> 點傷害！";

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

        yield return new WaitForSeconds(1.5f);
        if (EquipmentManager.Instance != null)
        {
            yield return StartCoroutine(EquipmentManager.Instance.ExecuteTapeGlovesExtraDamage(playerIndex, target));
        }
    }

    private IEnumerator Skill2Coroutine(int playerIndex)
    {
        PlaySoundByName("skill2");
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

        dialogueText.text = $"<b>布萊克</b> 使出了 <b>上吧，球球們</b>！\n用手上的球分散甩出攻擊了全部敵人\n" +
                           $"總共造成 <b>{totalDamageDealt}</b> 點傷害！";

        skillCooldowns["Skill_2_" + playerIndex] = skill2CooldownTurns;
        EffectManager.Instance.PlayUIEffect("B2", new Vector2(0, 0));
        yield return new WaitForSeconds(1.5f);
    }

    private IEnumerator Skill3Coroutine(int playerIndex)
    {
        PlaySoundByName("skill3");
        buffDebuffManager.ApplyDefenseBuff(playerUnits[playerIndex], 1.5f, 1);
        skillCooldowns["Skill_3_" + playerIndex] = skill3CooldownTurns;
        dialogueText.text = $"<b>布萊克</b> 使出了 <b>舔舔舔</b>！\n將身上的毛整理好，使防禦力上升。";
        EffectManager.Instance.PlayUIEffect("B3", new Vector2(-802, -362));
        yield return new WaitForSeconds(1.5f);
    }

    private IEnumerator Skill4Coroutine(int playerIndex)
    {
        PlaySoundByName("Skill5");
        buffDebuffManager.ApplyDefenseBuff(playerUnits[playerIndex], 2.0f, 2);
        skillCooldowns["Skill_4_" + playerIndex] = skill4CooldownTurns;
        dialogueText.text = $"<b>布萊克</b> 使出了 <b>大雞腿</b>～\n緊縮成一團，使敵人找不到縫隙攻擊。";
        EffectManager.Instance.PlayUIEffect("B4", new Vector2(-802, -362));
        yield return new WaitForSeconds(1.5f);
    }

    private IEnumerator Skill5Coroutine(int playerIndex, Unit target)
    {
        if (playerUnits[playerIndex] == null || playerUnits[playerIndex].currentHP <= 0) yield break;

        if (target == null || target.isDead || !enemyUnits.Contains(target))
        {
            skillCooldowns["Skill_5_" + playerIndex] = skill5CooldownTurns;
            yield break;
        }

        PlaySoundByName("skill4-1");
        PlaySoundByName("skill4-2");
        buffDebuffManager.ApplyDefenseDebuff(target, 0.7f, 2);

        int buffedDamage = Mathf.RoundToInt(playerUnits[playerIndex].damage * 1.3f);
        int damageDealt;
        bool isKilled = target.TakeDamage(buffedDamage, out damageDealt);

        int targetIndex = enemyUnits.IndexOf(target);
        if (targetIndex >= 0 && targetIndex < enemyHUDs.Count && enemyHUDs[targetIndex] != null)
        {
            enemyHUDs[targetIndex].SetHP(target.currentHP);
        }

        dialogueText.text = $"<b>布萊克</b> 使出了 <b>連環兔子踹</b>！\n抱住 <b>{target.unitName}</b> ，使出最強攻擊兔子踹！\n" +
                           $"<b>{target.unitName}</b> 受到了 <b>{damageDealt}</b> 點傷害！";

        skillCooldowns["Skill_5_" + playerIndex] = skill5CooldownTurns;

        if (enemyUnits.Count == 1)
        {
            EffectManager.Instance.PlayUIEffect("B5", new Vector2(0, 0));
        }
        else
        {
            bool isTargetLeft = (targetIndex == 0);
            EffectManager.Instance.PlayUIEffectOneSide("B5", isTargetLeft);
        }

        yield return new WaitForSeconds(1.5f);
        if (EquipmentManager.Instance != null)
        {
            yield return StartCoroutine(EquipmentManager.Instance.ExecuteTapeGlovesExtraDamage(playerIndex, target));
        }
    }

    private IEnumerator CatSkill1Coroutine(int playerIndex, Unit target)
    {
        if (target == null || target.isDead || !enemyUnits.Contains(target)) yield break;
        PlaySoundByName("skill1");
        int buffedDamage = Mathf.RoundToInt(playerUnits[playerIndex].damage * 1f);
        int damageDealt;
        bool isKilled = target.TakeDamage(buffedDamage, out damageDealt);

        int targetIndex = enemyUnits.IndexOf(target);
        if (targetIndex >= 0 && targetIndex < enemyHUDs.Count && enemyHUDs[targetIndex] != null)
        {
            enemyHUDs[targetIndex].SetHP(target.currentHP);
        }

        dialogueText.text = $"<b>萊拉</b> 使出了 <b>三分球</b>！\n撿起身邊的東西丟向 <b>{target.unitName}</b>！\n" +
                           $"<b>{target.unitName}</b> 受到了 <b>{damageDealt}</b> 點傷害！";

        EffectManager.Instance.PlayUIEffect("L1", new Vector2(0, 0));
        yield return new WaitForSeconds(1.5f);
    }

    private IEnumerator CatSkill2Coroutine(int playerIndex)
    {
        foreach (Unit unit in playerUnits)
        {
            if (unit != null && !unit.isDead)
            {
                buffDebuffManager.Heal(unit, 0.75f);
                int idx = playerUnits.IndexOf(unit);
                if (idx >= 0 && idx < playerHUDs.Count && playerHUDs[idx] != null)
                {
                    playerHUDs[idx].SetHP(unit.currentHP);
                    PlaySoundByName("catskill2");
                }
            }
        }

        skillCooldowns["catSkill_2_" + playerIndex] = catskill2CooldownTurns;
        dialogueText.text = $"<b>萊拉</b> 使出了 <b>小意思</b>！\n將傷口進行包紮";
        EffectManager.Instance.PlayUIEffect("L2", new Vector2(800, 0));
        EffectManager.Instance.PlayUIEffect("L2", new Vector2(-800, 0));
        yield return new WaitForSeconds(2.5f);
    }

    private IEnumerator CatSkill3Coroutine(int playerIndex)
    {
        if (playerUnits[playerIndex] == null || playerUnits[playerIndex].currentHP <= 0)
        {
            skillCooldowns["catSkill_3_" + playerIndex] = catskill3CooldownTurns;
            yield break;
        }

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

        dialogueText.text = $"<b>萊拉</b> 使出了 <b>Do～Re～Mi～</b>\n透過唱歌給全體敵人造成傷害！\n" +
                           $"總共造成 <b>{totalDamageDealt}</b> 點傷害！";

        skillCooldowns["catSkill_3_" + playerIndex] = catskill3CooldownTurns;
        EffectManager.Instance.PlayUIEffect("L3", new Vector2(0, 0));
        yield return new WaitForSeconds(2f);
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
        PlaySoundByName("catskill4_1");
        PlaySoundByName("catskill4_2");
        buffDebuffManager.ApplyDamageBuff(target, 2.0f, 2);
        skillCooldowns["catSkill_4_" + playerIndex] = catskill4CooldownTurns;
        dialogueText.text = $"<b>萊拉</b> 使出了 <b>打起精神來</b>！\n對 <b>{target.unitName}</b> 施展魔法，提升其攻擊力";
        int targetIndex = playerUnits.IndexOf(target);
        bool isTargetLeft = (targetIndex == 0);
        EffectManager.Instance.PlayUIEffectOneSide("L4", isTargetLeft);
        yield return new WaitForSeconds(1.5f);
    }

    private IEnumerator CatSkill5Coroutine(int playerIndex)
    {
        foreach (Unit player in playerUnits)
        {
            PlaySoundByName("catskill5");
            buffDebuffManager.ApplyFixedDamageReduction(player, 0.7f, 1);
        }
        skillCooldowns["catSkill_5_" + playerIndex] = catskill5CooldownTurns;
        dialogueText.text = $"<b>萊拉</b> 使出了 <b>痛痛飛走</b>～\n透過優美的歌聲振奮士氣，使我方全體承受傷害降低";
        EffectManager.Instance.PlayUIEffect("L5", new Vector2(0, 0));
        yield return new WaitForSeconds(2.0f);
    }

    // ==================== 技能選擇 ====================
    public void Skill_1(int playerIndex, Unit target)
    {
        if (playerIndex != 0 || !ValidateSkill(playerIndex, "Skill_1") || !ValidateTarget(playerIndex, target)) return;

        selectedSkillNames[playerIndex].Add("Skill_1");
        skillNeedsTarget[playerIndex].Add(true);
        skillQueues[playerIndex].Add(Skill1Coroutine(playerIndex, target));
        dialogueText.text = $"<b>吃我一擊</b>！\n甩出手上的球攻擊\nCD : 0    消耗AP : 1";

        RemoveSkillIcon(GetSkillCost("Skill_1"));
        UpdateAllSkillButtonStates();
    }

    public void Skill_2(int playerIndex)
    {
        if (playerIndex != 0 || !ValidateSkill(playerIndex, "Skill_2")) return;

        selectedSkillNames[playerIndex].Add("Skill_2");
        skillNeedsTarget[playerIndex].Add(false);
        skillQueues[playerIndex].Add(Skill2Coroutine(playerIndex));
        dialogueText.text = $"<b>上吧，球球們</b>！\n手上的球分散甩出攻擊全部敵人\nCD : 2    消耗AP : 1";

        RemoveSkillIcon(GetSkillCost("Skill_2"));
        UpdateAllSkillButtonStates();
    }

    public void Skill_3(int playerIndex)
    {
        if (playerIndex != 0 || !ValidateSkill(playerIndex, "Skill_3")) return;

        selectedSkillNames[playerIndex].Add("Skill_3");
        skillNeedsTarget[playerIndex].Add(false);
        skillQueues[playerIndex].Add(Skill3Coroutine(playerIndex));
        dialogueText.text = $"<b>舔舔舔</b>！\n將身上的毛整理好，使防禦力上升。\nCD : 1    消耗AP : 1";

        RemoveSkillIcon(GetSkillCost("Skill_3"));
        UpdateAllSkillButtonStates();
    }

    public void Skill_4(int playerIndex)
    {
        if (playerIndex != 0 || !ValidateSkill(playerIndex, "Skill_4")) return;

        selectedSkillNames[playerIndex].Add("Skill_4");
        skillNeedsTarget[playerIndex].Add(false);
        skillQueues[playerIndex].Add(Skill4Coroutine(playerIndex));
        dialogueText.text = $"<b>大雞腿</b>～\n緊縮成一團，使敵人找不到縫隙攻擊。\nCD : 2    消耗AP : 2";
        buffDebuffManager.skill4BuffUnits.Add(playerUnits[playerIndex]);

        RemoveSkillIcon(GetSkillCost("Skill_4"));
        UpdateAllSkillButtonStates();
    }

    public void Skill_5(int playerIndex, Unit target)
    {
        if (!ValidateSkill(playerIndex, "Skill_5") || !ValidateTarget(playerIndex, target)) return;

        selectedSkillNames[playerIndex].Add("Skill_5");
        skillNeedsTarget[playerIndex].Add(true);
        skillQueues[playerIndex].Add(Skill5Coroutine(playerIndex, target));
        dialogueText.text = $"<b>連環兔子踹</b>！\n 抱住敵人，使出最強攻擊兔子踹！\nCD : 2    消耗AP : 3";

        RemoveSkillIcon(GetSkillCost("Skill_5"));
        UpdateAllSkillButtonStates();
    }

    public void catSkill_1(int playerIndex, Unit target)
    {
        if (playerIndex != 1 || !ValidateSkill(playerIndex, "catSkill_1") || !ValidateTarget(playerIndex, target)) return;

        selectedSkillNames[playerIndex].Add("catSkill_1");
        skillNeedsTarget[playerIndex].Add(true);
        skillQueues[playerIndex].Add(CatSkill1Coroutine(playerIndex, target));
        dialogueText.text = $"<b>三分球</b>！\n 撿起身邊的東西丟向敵人。\nCD : 0    消耗AP : 1";

        RemoveSkillIcon(GetSkillCost("catSkill_1"));
        UpdateAllSkillButtonStates();
    }

    public void catSkill_2(int playerIndex)
    {
        if (playerIndex != 1 || !ValidateSkill(playerIndex, "catSkill_2")) return;

        selectedSkillNames[playerIndex].Add("catSkill_2");
        skillNeedsTarget[playerIndex].Add(false);
        skillQueues[playerIndex].Add(CatSkill2Coroutine(playerIndex));
        dialogueText.text = $"<b>小意思</b>！\n將傷口進行包紮。\nCD : 2    消耗AP : 1";

        RemoveSkillIcon(GetSkillCost("catSkill_2"));
        UpdateAllSkillButtonStates();
    }

    public void catSkill_3(int playerIndex)
    {
        if (!ValidateSkill(playerIndex, "catSkill_3")) return;

        selectedSkillNames[playerIndex].Add("catSkill_3");
        skillNeedsTarget[playerIndex].Add(false);
        skillQueues[playerIndex].Add(CatSkill3Coroutine(playerIndex));
        dialogueText.text = $"<b>Do～Re～Mi～</b>\n 大聲歌唱，干擾全部敵人。\nCD : 1    消耗AP : 1";

        RemoveSkillIcon(GetSkillCost("catSkill_3"));
        UpdateAllSkillButtonStates();
    }

    public void catSkill_4(int playerIndex, Unit target)
    {
        if (!ValidateSkill(playerIndex, "catSkill_4") || !ValidateTarget(playerIndex, target)) return;

        selectedSkillNames[playerIndex].Add("catSkill_4");
        skillNeedsTarget[playerIndex].Add(true);
        skillQueues[playerIndex].Add(CatSkill4Coroutine(playerIndex, target));
        dialogueText.text = $"<b>打起精神來</b>！\n 施展力量變強壯的魔法。\nCD : 2    消耗AP : 2";

        RemoveSkillIcon(GetSkillCost("catSkill_4"));
        UpdateAllSkillButtonStates();
    }

    public void catSkill_5(int playerIndex)
    {
        if (playerIndex != 1 || !ValidateSkill(playerIndex, "catSkill_5")) return;

        selectedSkillNames[playerIndex].Add("catSkill_5");
        skillNeedsTarget[playerIndex].Add(false);
        skillQueues[playerIndex].Add(CatSkill5Coroutine(playerIndex));
        dialogueText.text = $"<b>痛痛飛走</b>～\n 優美的歌聲，振奮隊伍士氣。\nCD : 3    消耗AP : 3";

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

        // 升級檢查
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

        // ★ 絕對阻斷：如果穿了盔甲，按了直接擋下來！
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

        // ★ 絕對阻斷
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

        // ★ 絕對阻斷
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

        // ★ 絕對阻斷
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

    private IEnumerator ExecuteAllPlayerQueues()
    {
        executingPlayerCount = 0;

        if (skillQueues[0].Count > 0)
        {
            executingPlayerCount++;
            yield return StartCoroutine(ExecuteSkillQueueWithCallback(0));
        }

        if (skillQueues[1].Count > 0)
        {
            executingPlayerCount++;
            yield return StartCoroutine(ExecuteSkillQueueWithCallback(1));
        }

        if (executingPlayerCount <= 0 && battlesystem != null)
        {
            battlesystem.OnPlayerConfirmSkillQueue();
        }
    }

    IEnumerator ExecuteSkillQueueWithCallback(int playerIndex)
    {
        yield return StartCoroutine(ExecuteSkillQueue(playerIndex));
        executingPlayerCount--;
        if (executingPlayerCount <= 0 && battlesystem != null)
        {
            battlesystem.OnPlayerConfirmSkillQueue();
        }
    }

    IEnumerator ExecuteSkillQueue(int playerIndex)
    {

        if (!CanUseSkill(playerIndex))
        {
            skillQueues[playerIndex].Clear();
            selectedSkillNames[playerIndex].Clear();
            skillNeedsTarget[playerIndex].Clear();
            yield break;
        }

        var coroutines = new List<IEnumerator>(skillQueues[playerIndex]);
        foreach (var coroutine in coroutines)
        {
            if (playerUnits[playerIndex] == null || playerUnits[playerIndex].isDead)
                yield break;

            yield return StartCoroutine(coroutine);
        }

        skillQueues[playerIndex].Clear();
        selectedSkillNames[playerIndex].Clear();
        skillNeedsTarget[playerIndex].Clear();
    }

    void PerformMultiTargetAttack(int playerIndex, int damage)
    {
        if (enemyUnits == null || enemyUnits.Count == 0) return;

        foreach (Unit enemy in enemyUnits)
        {
            if (enemy != null && !enemy.isDead)
            {
                int damageDealt;
                bool isKilled = enemy.TakeDamage(damage, out damageDealt);

                int targetIndex = enemyUnits.IndexOf(enemy);
                if (targetIndex >= 0 && targetIndex < enemyHUDs.Count && enemyHUDs[targetIndex] != null)
                    enemyHUDs[targetIndex].SetHP(enemy.currentHP);
            }
        }
    }

    void PerformBasicAttack(int playerIndex, Unit target, int damageOverride = -1)
    {
        if (target == null || target.isDead || !enemyUnits.Contains(target)) return;

        int actualDamage = damageOverride > 0 ? damageOverride : playerUnits[playerIndex].damage;

        int damageDealt;
        bool isKilled = target.TakeDamage(actualDamage, out damageDealt);

        int targetIndex = enemyUnits.IndexOf(target);
        if (targetIndex >= 0 && targetIndex < enemyHUDs.Count && enemyHUDs[targetIndex] != null)
            enemyHUDs[targetIndex].SetHP(target.currentHP);
    }

    // ==================== AP 圖示控制 ====================
    // 新增 cost 參數，消耗多少 AP 就刪掉多少顆寶石圖示
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

    // 新增 cost 參數，取消技能時退回對應數量的 AP 圖示
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
    // =====================================================

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
        return skillKey; // 如果找不到，就回傳原始 ID
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
            icon.SetActive(true); // 確保生成的圖示沒有被隱藏
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

        if (buffDebuffManager.IsSkillDisabled(unit))
        {
            if (dialogueText != null)
                dialogueText.text = $"<b>{unit.unitName}</b> 被技能封印，無法使用技能！";
            return false;
        }
        return true;
    }

    public void OnCanButtonClick() => itemuse.TriggerCanForPlayer(0);
    public void OnTunaButtonClick() => itemuse.TriggerTunaForPlayer(0);
    public void OnGrassButtonClick() => itemuse.TriggerGrassForPlayer(0);
    public void OnDryingButtonClick() => itemuse.TriggerDryingForPlayer(0);
}