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
    public Dictionary<string, int> skillCooldowns = new Dictionary<string, int>();

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

    public void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (playerUnits.Count != 2) Debug.LogError("playerUnits count is not 2, check initialization");

        for (int i = 0; i < playerUnits.Count; i++)
        {
            Debug.Log($"Initializing player {i}: {playerUnits[i]?.unitName}");
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
        UpdateAllSkillButtonStates();
    }

    void Update()
    {
        if (isWaitingForTarget && Input.GetMouseButtonDown(0))
        {
            Debug.Log("Left mouse button clicked, processing target selection");
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

        if (Input.GetMouseButtonDown(1))
        {
            CancelLatestSkill();
        }
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
        dialogueText.text = $"{playerUnit.unitName} 取消了 {latestSkillName}";

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

        AddSkillIcon();
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
            dialogueText.text = $"技能 {displayName} 正在冷卻中，剩餘 {skillCooldowns[cooldownKey]} 回合！";
            return true;
        }
        return false;
    }

    public void SelectTarget(int playerIndex, Unit target)
    {
        if (target != null && (enemyUnits.Contains(target) || playerUnits.Contains(target)) && target.currentHP > 0)
        {
            selectedTargets[playerIndex] = target;
            dialogueText.text = $"{playerUnits[playerIndex].unitName} 已選擇目標：{target.unitName}";
            HighlightTarget(target, true);
            foreach (var unit in enemyUnits) if (unit != target && unit.currentHP > 0) HighlightTarget(unit, false);
            foreach (var unit in playerUnits) if (unit != target && unit.currentHP > 0) HighlightTarget(unit, false);
        }
        else
        {
            selectedTargets[playerIndex] = null;
            dialogueText.text = $"{playerUnits[playerIndex].unitName}：無效的目標，請點擊一個存活的目標！";
        }
    }

    public void HighlightTarget(Unit target, bool isHighlighted)
    {
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
        if (selectedTarget == null || selectedTarget.currentHP <= 0)
        {
            dialogueText.text = "請選擇一個存活的玩家目標！";
            return;
        }

        bool isWaitingForPlayerTarget = pendingSkill == "catSkill_4" || pendingSkill == "Can" || pendingSkill == "Grass" || pendingSkill == "Drying";
        if (!isWaitingForPlayerTarget || pendingPlayerIndex < 0) return;

        dialogueText.text = $"{playerUnits[pendingPlayerIndex].unitName} 選擇對 {selectedTarget.unitName} 使用 {pendingSkill}";
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

            dialogueText.text = $"{playerUnits[playerIndex].unitName} 取消了 {skillName}";
            AddSkillIcon();
            UpdateAllSkillButtonStates();
        }
    }

    public bool ValidateTarget(int playerIndex, Unit target)
    {
        if (target == null || target.currentHP <= 0)
        {
            dialogueText.text = $"{playerUnits[playerIndex].unitName}：請點擊一個有效的目標！";
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
        if (target == null || target.currentHP <= 0 || !enemyUnits.Contains(target)) yield break;

        PlaySoundByName("skill1");
        int buffedDamage = Mathf.RoundToInt(playerUnits[playerIndex].damage * 1.2f);
        PerformBasicAttack(playerIndex, target, buffedDamage);
        dialogueText.text = $"{playerUnits[playerIndex].unitName} 甩出手上的球攻擊 {target.unitName}";
        EffectManager.Instance.PlayUIEffect("B1", new Vector2(0, 0));

        yield return new WaitForSeconds(1.5f); // 請根據實際特效長度調整
    }

    private IEnumerator Skill2Coroutine(int playerIndex)
    {
        PlaySoundByName("skill2");
        int buffedDamage = Mathf.RoundToInt(playerUnits[playerIndex].damage * 1.5f);
        PerformMultiTargetAttack(playerIndex, buffedDamage);
        skillCooldowns["Skill_2_" + playerIndex] = skill2CooldownTurns;
        dialogueText.text = $"{playerUnits[playerIndex].unitName} 將手上的球分散甩出攻擊全部敵人";
        EffectManager.Instance.PlayUIEffect("B2", new Vector2(0, 0));

        yield return new WaitForSeconds(2.0f); // 請根據實際特效長度調整
    }

    private IEnumerator Skill3Coroutine(int playerIndex)
    {
        PlaySoundByName("skill3");
        buffDebuffManager.ApplyDefenseBuff(playerUnits[playerIndex], 1.5f, 1);
        skillCooldowns["Skill_3_" + playerIndex] = skill3CooldownTurns;
        dialogueText.text = $"{playerUnits[playerIndex].unitName} 將身上的毛整理好，使防禦力上升";
        EffectManager.Instance.PlayUIEffect("B3", new Vector2(-800, 0));

        yield return new WaitForSeconds(1.8f);
    }

    private IEnumerator Skill4Coroutine(int playerIndex)
    {
        PlaySoundByName("Skill_4");
        buffDebuffManager.ApplyDefenseBuff(playerUnits[playerIndex], 2.0f, 2);
        skillCooldowns["Skill_4_" + playerIndex] = skill4CooldownTurns;
        dialogueText.text = $"{playerUnits[playerIndex].unitName} 緊縮成一團，使敵人找不到縫隙攻擊";
        EffectManager.Instance.PlayUIEffect("B4", new Vector2(-800, 0));

        yield return new WaitForSeconds(2.0f);
    }

    private IEnumerator Skill5Coroutine(int playerIndex, Unit target)
    {
        if (playerUnits[playerIndex] == null || playerUnits[playerIndex].currentHP <= 0) yield break;
        if (target == null || target.currentHP <= 0 || !enemyUnits.Contains(target))
        {
            skillCooldowns["Skill_5_" + playerIndex] = skill5CooldownTurns;
            yield break;
        }

        PlaySoundByName("skill4-1");
        PlaySoundByName("skill4-2");
        buffDebuffManager.ApplyDefenseDebuff(target, 0.7f, 2);
        int buffedDamage = Mathf.RoundToInt(playerUnits[playerIndex].damage * 1.3f);
        PerformBasicAttack(playerIndex, target, buffedDamage);
        skillCooldowns["Skill_5_" + playerIndex] = skill5CooldownTurns;
        dialogueText.text = $"{playerUnits[playerIndex].unitName} 抱住敵方，使出最強攻擊兔子踹！";
        EffectManager.Instance.PlayUIEffect("B5", new Vector2(0, 0));

        yield return new WaitForSeconds(2.2f);
    }

    private IEnumerator CatSkill1Coroutine(int playerIndex, Unit target)
    {
        if (target == null || target.currentHP <= 0 || !enemyUnits.Contains(target)) yield break;

        int buffedDamage = Mathf.RoundToInt(playerUnits[playerIndex].damage * 1f);
        PerformBasicAttack(playerIndex, target, buffedDamage);
        dialogueText.text = $"{playerUnits[playerIndex].unitName} 撿起身邊的東西丟向 {target.unitName}";
        EffectManager.Instance.PlayUIEffect("L1", new Vector2(0, 0));

        yield return new WaitForSeconds(1.5f);
    }

    private IEnumerator CatSkill2Coroutine(int playerIndex)
    {
        buffDebuffManager.Heal(playerUnits[playerIndex], 0.75f);
        skillCooldowns["catSkill_2_" + playerIndex] = catskill2CooldownTurns;
        dialogueText.text = $"{playerUnits[playerIndex].unitName} 將傷口進行包紮";
        EffectManager.Instance.PlayUIEffect("L2", new Vector2(800, 0));
        EffectManager.Instance.PlayUIEffect("L2", new Vector2(-800, 0));

        yield return new WaitForSeconds(1.8f);
    }

    private IEnumerator CatSkill3Coroutine(int playerIndex)
    {
        if (playerUnits[playerIndex] == null || playerUnits[playerIndex].currentHP <= 0)
        {
            skillCooldowns["catSkill_3_" + playerIndex] = catskill3CooldownTurns;
            yield break;
        }

        int buffedDamage = Mathf.RoundToInt(playerUnits[playerIndex].damage * 1f);
        PerformMultiTargetAttack(playerIndex, buffedDamage);
        skillCooldowns["catSkill_3_" + playerIndex] = catskill3CooldownTurns;
        dialogueText.text = $"{playerUnits[playerIndex].unitName} 透過唱歌給全體敵人造成傷害";
        EffectManager.Instance.PlayUIEffect("L3", new Vector2(0, 0));

        yield return new WaitForSeconds(2.0f);
    }

    private IEnumerator CatSkill4Coroutine(int playerIndex, Unit target)
    {
        if (playerUnits[playerIndex] == null || playerUnits[playerIndex].currentHP <= 0)
        {
            skillCooldowns["catSkill_4_" + playerIndex] = catskill4CooldownTurns;
            yield break;
        }
        if (target == null || target.currentHP <= 0 || !playerUnits.Contains(target))
        {
            skillCooldowns["catSkill_4_" + playerIndex] = catskill4CooldownTurns;
            yield break;
        }

        buffDebuffManager.ApplyDamageBuff(target, 2.0f, 2);
        skillCooldowns["catSkill_4_" + playerIndex] = catskill4CooldownTurns;
        dialogueText.text = $"{playerUnits[playerIndex].unitName} 對 {target.unitName} 施展魔法，提升其攻擊力";
        int targetIndex = playerUnits.IndexOf(target);
        bool isTargetLeft = (targetIndex == 0);
        EffectManager.Instance.PlayUIEffectOneSide("L4", isTargetLeft);

        yield return new WaitForSeconds(2.0f);
    }

    private IEnumerator CatSkill5Coroutine(int playerIndex)
    {
        foreach (Unit player in playerUnits)
        {
            buffDebuffManager.ApplyFixedDamageReduction(player, 0.7f, 1);
        }
        skillCooldowns["catSkill_5_" + playerIndex] = catskill5CooldownTurns;
        dialogueText.text = $"{playerUnits[playerIndex].unitName} 透過歌聲振奮士氣，使我方全體承受傷害降低";
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

        dialogueText.text = $"{playerUnits[playerIndex].unitName} 選擇了技能 1 對 {target.unitName}";
        RemoveSkillIcon();
        UpdateAllSkillButtonStates();
    }

    public void Skill_2(int playerIndex)
    {
        if (playerIndex != 0 || !ValidateSkill(playerIndex, "Skill_2")) return;

        selectedSkillNames[playerIndex].Add("Skill_2");
        skillNeedsTarget[playerIndex].Add(false);
        skillQueues[playerIndex].Add(Skill2Coroutine(playerIndex));

        dialogueText.text = $"{playerUnits[playerIndex].unitName} 選擇了技能 2";
        RemoveSkillIcon();
        UpdateAllSkillButtonStates();
    }

    public void Skill_3(int playerIndex)
    {
        if (playerIndex != 0 || !ValidateSkill(playerIndex, "Skill_3")) return;

        selectedSkillNames[playerIndex].Add("Skill_3");
        skillNeedsTarget[playerIndex].Add(false);
        skillQueues[playerIndex].Add(Skill3Coroutine(playerIndex));

        dialogueText.text = $"{playerUnits[playerIndex].unitName} 選擇了技能 3";
        RemoveSkillIcon();
        UpdateAllSkillButtonStates();
    }

    public void Skill_4(int playerIndex)
    {
        if (playerIndex != 0 || !ValidateSkill(playerIndex, "Skill_4")) return;

        selectedSkillNames[playerIndex].Add("Skill_4");
        skillNeedsTarget[playerIndex].Add(false);
        skillQueues[playerIndex].Add(Skill4Coroutine(playerIndex));

        dialogueText.text = $"{playerUnits[playerIndex].unitName} 選擇了技能 4";
        RemoveSkillIcon();
        UpdateAllSkillButtonStates();
    }

    public void Skill_5(int playerIndex, Unit target)
    {
        if (!ValidateSkill(playerIndex, "Skill_5") || !ValidateTarget(playerIndex, target)) return;

        selectedSkillNames[playerIndex].Add("Skill_5");
        skillNeedsTarget[playerIndex].Add(true);
        skillQueues[playerIndex].Add(Skill5Coroutine(playerIndex, target));

        dialogueText.text = $"{playerUnits[playerIndex].unitName} 選擇了技能 5 對 {target.unitName}";
        RemoveSkillIcon();
        UpdateAllSkillButtonStates();
    }

    public void catSkill_1(int playerIndex, Unit target)
    {
        if (playerIndex != 1 || !ValidateSkill(playerIndex, "catSkill_1") || !ValidateTarget(playerIndex, target)) return;

        selectedSkillNames[playerIndex].Add("catSkill_1");
        skillNeedsTarget[playerIndex].Add(true);
        skillQueues[playerIndex].Add(CatSkill1Coroutine(playerIndex, target));

        dialogueText.text = $"{playerUnits[playerIndex].unitName} 選擇了技能 1 對 {target.unitName}";
        RemoveSkillIcon();
        UpdateAllSkillButtonStates();
    }

    public void catSkill_2(int playerIndex)
    {
        if (playerIndex != 1 || !ValidateSkill(playerIndex, "catSkill_2")) return;

        selectedSkillNames[playerIndex].Add("catSkill_2");
        skillNeedsTarget[playerIndex].Add(false);
        skillQueues[playerIndex].Add(CatSkill2Coroutine(playerIndex));

        dialogueText.text = $"{playerUnits[playerIndex].unitName} 選擇了技能 2";
        RemoveSkillIcon();
        UpdateAllSkillButtonStates();
    }

    public void catSkill_3(int playerIndex)
    {
        if (!ValidateSkill(playerIndex, "catSkill_3")) return;

        selectedSkillNames[playerIndex].Add("catSkill_3");
        skillNeedsTarget[playerIndex].Add(false);
        skillQueues[playerIndex].Add(CatSkill3Coroutine(playerIndex));

        dialogueText.text = $"{playerUnits[playerIndex].unitName} 選擇了技能 3";
        RemoveSkillIcon();
        UpdateAllSkillButtonStates();
    }

    public void catSkill_4(int playerIndex, Unit target)
    {
        if (!ValidateSkill(playerIndex, "catSkill_4") || !ValidateTarget(playerIndex, target)) return;

        selectedSkillNames[playerIndex].Add("catSkill_4");
        skillNeedsTarget[playerIndex].Add(true);
        skillQueues[playerIndex].Add(CatSkill4Coroutine(playerIndex, target));

        dialogueText.text = $"{playerUnits[playerIndex].unitName} 選擇了技能 4 對 {target.unitName}";
        RemoveSkillIcon();
        UpdateAllSkillButtonStates();
    }

    public void catSkill_5(int playerIndex)
    {
        if (playerIndex != 1 || !ValidateSkill(playerIndex, "catSkill_5")) return;

        selectedSkillNames[playerIndex].Add("catSkill_5");
        skillNeedsTarget[playerIndex].Add(false);
        skillQueues[playerIndex].Add(CatSkill5Coroutine(playerIndex));

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
            return;
        }
        if (GetTotalSelectedSkills() >= maxSkillPerTurn)
        {
            dialogueText.text = "已達技能數量上限！";
            return;
        }
        isWaitingForTarget = true;
        pendingSkill = "Skill_1";
        pendingPlayerIndex = 0;
        dialogueText.text = "請點擊一個敵人目標以使用技能 1！";
        SetButtonDimmed(skill1Button, true);
    }

    public void TriggerSkill2ForPlayer0()
    {
        ClearAllHighlights();
        if (battlesystem.state != BattleState.PLAYERTURN) return;
        if (CheckSkillCooldownAndShowMessage(0, "Skill_2", "2")) return;
        if (selectedSkillNames[0].Contains("Skill_2"))
        {
            CancelSkill(0, "Skill_2");
            SetButtonDimmed(skill2Button, false);
            return;
        }
        if (GetTotalSelectedSkills() >= maxSkillPerTurn)
        {
            dialogueText.text = "已達技能數量上限！";
            return;
        }
        Skill_2(0);
        SetButtonDimmed(skill2Button, true);
    }

    public void TriggerSkill3ForPlayer0()
    {
        ClearAllHighlights();
        if (battlesystem.state != BattleState.PLAYERTURN) return;
        if (CheckSkillCooldownAndShowMessage(0, "Skill_3", "3")) return;
        if (selectedSkillNames[0].Contains("Skill_3"))
        {
            CancelSkill(0, "Skill_3");
            SetButtonDimmed(skill3Button, false);
            return;
        }
        if (GetTotalSelectedSkills() >= maxSkillPerTurn)
        {
            dialogueText.text = "已達技能數量上限！";
            return;
        }
        Skill_3(0);
        SetButtonDimmed(skill3Button, true);
    }

    public void TriggerSkill4ForPlayer0()
    {
        ClearAllHighlights();
        if (battlesystem.state != BattleState.PLAYERTURN) return;
        if (CheckSkillCooldownAndShowMessage(0, "Skill_4", "4")) return;
        if (selectedSkillNames[0].Contains("Skill_4"))
        {
            CancelSkill(0, "Skill_4");
            SetButtonDimmed(skill4Button, false);
            return;
        }
        if (GetTotalSelectedSkills() >= maxSkillPerTurn)
        {
            dialogueText.text = "已達技能數量上限！";
            return;
        }
        Skill_4(0);
        SetButtonDimmed(skill4Button, true);
    }

    public void TriggerSkill5ForPlayer0()
    {
        ClearAllHighlights();
        if (battlesystem.state != BattleState.PLAYERTURN) return;
        if (CheckSkillCooldownAndShowMessage(0, "Skill_5", "5")) return;
        if (selectedSkillNames[0].Contains("Skill_5"))
        {
            CancelSkill(0, "Skill_5");
            SetButtonDimmed(skill5Button, false);
            return;
        }
        if (GetTotalSelectedSkills() >= maxSkillPerTurn)
        {
            dialogueText.text = "已達技能數量上限！";
            return;
        }
        isWaitingForTarget = true;
        pendingSkill = "Skill_5";
        pendingPlayerIndex = 0;
        dialogueText.text = "請點擊一個敵人目標以使用技能 5！";
        SetButtonDimmed(skill5Button, true);
    }

    public void TriggerCatSkill1ForPlayer1()
    {
        ClearAllHighlights();
        if (battlesystem.state != BattleState.PLAYERTURN) return;
        if (selectedSkillNames[1].Contains("catSkill_1"))
        {
            CancelSkill(1, "catSkill_1");
            SetButtonDimmed(catSkill1Button, false);
            return;
        }
        if (GetTotalSelectedSkills() >= maxSkillPerTurn)
        {
            dialogueText.text = "已達技能數量上限！";
            return;
        }
        isWaitingForTarget = true;
        pendingSkill = "catSkill_1";
        pendingPlayerIndex = 1;
        dialogueText.text = "請點擊一個敵人目標以使用貓技能 1！";
        SetButtonDimmed(catSkill1Button, true);
    }

    public void TriggerCatSkill2ForPlayer1()
    {
        ClearAllHighlights();
        if (battlesystem.state != BattleState.PLAYERTURN) return;
        if (CheckSkillCooldownAndShowMessage(1, "catSkill_2", "2")) return;
        if (selectedSkillNames[1].Contains("catSkill_2"))
        {
            CancelSkill(1, "catSkill_2");
            SetButtonDimmed(catSkill2Button, false);
            return;
        }
        if (GetTotalSelectedSkills() >= maxSkillPerTurn)
        {
            dialogueText.text = "已達技能數量上限！";
            return;
        }
        catSkill_2(1);
        SetButtonDimmed(catSkill2Button, true);
    }

    public void TriggerCatSkill3ForPlayer1()
    {
        ClearAllHighlights();
        if (battlesystem.state != BattleState.PLAYERTURN) return;
        if (CheckSkillCooldownAndShowMessage(1, "catSkill_3", "3")) return;
        if (selectedSkillNames[1].Contains("catSkill_3"))
        {
            CancelSkill(1, "catSkill_3");
            SetButtonDimmed(catSkill3Button, false);
            return;
        }
        if (GetTotalSelectedSkills() >= maxSkillPerTurn)
        {
            dialogueText.text = "已達技能數量上限！";
            return;
        }
        catSkill_3(1);
        SetButtonDimmed(catSkill3Button, true);
    }

    public void TriggerCatSkill4ForPlayer1()
    {
        ClearAllHighlights();
        if (battlesystem.state != BattleState.PLAYERTURN) return;
        if (CheckSkillCooldownAndShowMessage(1, "catSkill_4", "4")) return;
        if (selectedSkillNames[1].Contains("catSkill_4"))
        {
            CancelSkill(1, "catSkill_4");
            SetButtonDimmed(catSkill4Button, false);
            return;
        }
        if (GetTotalSelectedSkills() >= maxSkillPerTurn)
        {
            dialogueText.text = "已達技能數量上限！";
            return;
        }
        isWaitingForTarget = true;
        pendingSkill = "catSkill_4";
        pendingPlayerIndex = 1;
        dialogueText.text = "請點擊一個玩家頭像以使用貓技能 4！";
        SetButtonDimmed(catSkill4Button, true);
    }

    public void TriggerCatSkill5ForPlayer1()
    {
        ClearAllHighlights();
        if (battlesystem.state != BattleState.PLAYERTURN) return;
        if (CheckSkillCooldownAndShowMessage(1, "catSkill_5", "5")) return;
        if (selectedSkillNames[1].Contains("catSkill_5"))
        {
            CancelSkill(1, "catSkill_5");
            SetButtonDimmed(catSkill5Button, false);
            return;
        }
        if (GetTotalSelectedSkills() >= maxSkillPerTurn)
        {
            dialogueText.text = "已達技能數量上限！";
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

        executingPlayerCount = 0;
        for (int i = 0; i < playerUnits.Count; i++)
        {
            if (skillQueues[i].Count > 0)
            {
                executingPlayerCount++;
                StartCoroutine(ExecuteSkillQueueWithCallback(i));
            }
        }

        if (executingPlayerCount == 0)
        {
            battlesystem?.OnPlayerConfirmSkillQueue();
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
        var coroutines = new List<IEnumerator>(skillQueues[playerIndex]);

        foreach (var coroutine in coroutines)
        {
            if (playerUnits[playerIndex] == null || playerUnits[playerIndex].currentHP <= 0)
                yield break;

            yield return StartCoroutine(coroutine);
        }

        skillQueues[playerIndex].Clear();
        selectedSkillNames[playerIndex].Clear();
        skillNeedsTarget[playerIndex].Clear();
    }

    public int GetTotalSelectedSkills()
    {
        int total = 0;
        foreach (var queue in skillQueues.Values) total += queue.Count;
        return total;
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
                    enemyHUDs[targetIndex].SetHP(enemy.currentHP);
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
                battlesystem.DestroyEnemy(index);
        }
        if (AllEnemiesDead() && battlesystem != null)
        {
            battlesystem.state = BattleState.WON;
            battlesystem.EndBattle();
        }
    }

    void PerformBasicAttack(int playerIndex, Unit target, int damageOverride = -1)
    {
        if (target == null || target.currentHP <= 0 || !enemyUnits.Contains(target)) return;
        int actualDamage = damageOverride > 0 ? damageOverride : playerUnits[playerIndex].damage;
        bool isDead = target.TakeDamage(actualDamage);
        int targetIndex = enemyUnits.IndexOf(target);
        if (targetIndex >= 0 && targetIndex < enemyHUDs.Count && enemyHUDs[targetIndex] != null)
            enemyHUDs[targetIndex].SetHP(target.currentHP);
        if (isDead)
            StartCoroutine(DelayedDestroyEnemy(targetIndex));
    }

    private IEnumerator DelayedDestroyEnemy(int index)
    {
        yield return new WaitForEndOfFrame();
        if (battlesystem != null && index >= 0 && index < enemyUnits.Count)
            battlesystem.DestroyEnemy(index);
    }

    private void CleanSkillQueue(int playerIndex, Unit deadEnemy)
    {
        if (skillQueues.ContainsKey(playerIndex))
        {
            var newQueues = new List<IEnumerator>();
            var newNames = new List<string>();
            var newNeeds = new List<bool>();
            for (int i = 0; i < skillQueues[playerIndex].Count; i++)
            {
                string skillName = selectedSkillNames[playerIndex][i];
                if (!(skillName == "Skill_1" || skillName == "Skill_5" || skillName == "catSkill_1" || skillName == "catSkill_4" ||
                      skillName == "Can" || skillName == "Grass" || skillName == "Drying") || !selectedTargets.ContainsValue(deadEnemy))
                {
                    newQueues.Add(skillQueues[playerIndex][i]);
                    newNames.Add(skillName);
                    newNeeds.Add(skillNeedsTarget[playerIndex][i]);
                }
            }
            skillQueues[playerIndex] = newQueues;
            selectedSkillNames[playerIndex] = newNames;
            skillNeedsTarget[playerIndex] = newNeeds;
        }
    }

    private bool AllEnemiesDead()
    {
        foreach (Unit enemy in enemyUnits)
            if (enemy.currentHP > 0) return false;
        return true;
    }

    public void RemoveSkillIcon()
    {
        if (skillIcons.Count > 0)
        {
            Destroy(skillIcons[skillIcons.Count - 1]);
            skillIcons.RemoveAt(skillIcons.Count - 1);
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
                skillCooldowns[key]--;
        }
        UpdateAllSkillButtonStates();
    }

    void ResetSkillIcons()
    {
        foreach (GameObject icon in skillIcons) Destroy(icon);
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
        UpdateAllSkillButtonStates();
    }

    public void ClearSkillIcons()
    {
        foreach (GameObject icon in skillIcons) Destroy(icon);
        skillIcons.Clear();
    }

    public void PlaySoundByName(string soundName)
    {
        AudioClip clip = Resources.Load<AudioClip>("soundeffects/" + soundName);
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
        else
            Debug.LogWarning("音效找不到: " + soundName);
    }

    public void OnCanButtonClick() => itemuse.TriggerCanForPlayer(0);
    public void OnTunaButtonClick() => itemuse.TriggerTunaForPlayer(0);
    public void OnGrassButtonClick() => itemuse.TriggerGrassForPlayer(0);
    public void OnDryingButtonClick() => itemuse.TriggerDryingForPlayer(0);
}