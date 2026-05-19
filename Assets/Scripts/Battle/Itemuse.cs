using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Itemuse : MonoBehaviour
{
    public RedBook redbook;
    public SkillManager skillManager;
    public BuffDebuffManager buffDebuffManager;
    public TextMeshProUGUI dialogueText;
    public List<BattleHUD> playerHUDs;
    public List<Unit> playerUnits;
    public ItemData can;
    public ItemData tuna;
    public ItemData grass;
    public ItemData drying;

    private float lastTunaClickTime = 0f;

    // ★ 修改 1：移除了 private Dictionary<string, int> itemInventory 
    // 從現在起，Itemuse 直接以 ItemData 為唯一真理！

    void Start()
    {
        // ★ 修改 2：不需再將數字寫入字典，完全依賴 ItemData
        Debug.Log("=== Itemuse 初始化完成，直接讀取 ItemData ===");
    }

    // ==================== 其他工具方法 (移到前面方便看) ====================

    // ★ 修改 3：所有檢查數量的方法，直接回傳 ItemData.amount
    public int GetItemCount(string itemName)
    {
        if (itemName == "Can" && can != null) return can.amount;
        if (itemName == "Tuna" && tuna != null) return tuna.amount;
        if (itemName == "Grass" && grass != null) return grass.amount;
        if (itemName == "Drying" && drying != null) return drying.amount;
        return 0;
    }

    public bool HasItem(string itemName)
    {
        return GetItemCount(itemName) > 0;
    }

    // ★ 修改 4：驗證物品是否可用，並將代號轉為中文顯示
    private bool ValidateItem(int playerIndex, string itemName)
    {
        if (!HasItem(itemName))
        {
            string displayName = itemName;
            if (itemName == "Can") displayName = "神聖罐罐";
            else if (itemName == "Tuna") displayName = "鮪魚肉泥";
            else if (itemName == "Grass") displayName = "貓草";
            else if (itemName == "Drying") displayName = "凍乾";

            dialogueText.text = $"沒有 <b>{displayName}</b> 可使用！";
            return false;
        }
        if (skillManager.GetTotalSelectedSkills() >= skillManager.maxSkillPerTurn)
        {
            dialogueText.text = "技能欄位已滿！";
            return false;
        }
        return true;
    }

    // 取消物品
    private void CancelItem(int playerIndex, string itemName)
    {
        if (skillManager.selectedSkillNames[playerIndex].Contains(itemName))
        {
            skillManager.RemoveSkillFromQueue(playerIndex, itemName);

            if (skillManager.selectedTargets.ContainsKey(playerIndex) && skillManager.selectedTargets[playerIndex] != null)
            {
                skillManager.HighlightTarget(skillManager.selectedTargets[playerIndex], false);
                skillManager.selectedTargets[playerIndex] = null;
            }

            string displayName = itemName;
            if (itemName == "Can") displayName = "神聖罐罐";
            else if (itemName == "Tuna") displayName = "鮪魚肉泥";
            else if (itemName == "Grass") displayName = "貓草";
            else if (itemName == "Drying") displayName = "凍乾";

            dialogueText.text = $"<b>{skillManager.playerUnits[playerIndex].unitName}</b> 取消了 <b>{displayName}</b>";

            skillManager.AddSkillIcon(skillManager.GetSkillCost(itemName));
            skillManager.UpdateAllSkillButtonStates();
        }
    }

    // ==================== 道具協程 ====================

    private IEnumerator CanCoroutine(int playerIndex, Unit target)
    {
        if (target == null || target.currentHP <= 0) yield break;

        skillManager.PlaySoundByName("can");

        int lostHP = target.maxHP - target.currentHP;
        int healAmount = Mathf.RoundToInt(lostHP * 0.6f);
        target.currentHP = Mathf.Min(target.currentHP + healAmount, target.maxHP);

        if (target.battleHUD != null) target.battleHUD.SetHP(target.currentHP);

        // ★ 修改 5：直接扣除 ItemData 數量即可，不用再更新字典
        if (can != null) can.amount--;

        int targetIndex = skillManager.playerUnits.IndexOf(target);
        bool isTargetLeft = (targetIndex == 0);
        EffectManager.Instance.PlayUIEffectOneSide("L2", isTargetLeft);

        dialogueText.text = $"<b>{skillManager.playerUnits[playerIndex].unitName}</b> 使用 <b>神聖罐罐</b> 恢復 <b>{target.unitName}</b> 的血量（+<b>{healAmount}</b>）";
        yield return new WaitForSeconds(1.8f);
    }

    private IEnumerator TunaCoroutine(int playerIndex)
    {
        skillManager.PlaySoundByName("heal");

        int totalHealedAmount = 0;
        int healedCount = 0;

        foreach (Unit player in skillManager.playerUnits)
        {
            if (player.currentHP > 0 && player.currentHP < player.maxHP)
            {
                int healAmount = Mathf.RoundToInt(player.maxHP * 0.3f);
                int actualHeal = Mathf.Min(healAmount, player.maxHP - player.currentHP);
                player.currentHP += actualHeal;
                totalHealedAmount += actualHeal;
                healedCount++;

                if (player.battleHUD != null) player.battleHUD.SetHP(player.currentHP);
            }
        }

        if (tuna != null) tuna.amount--; // ★ 扣除 ItemData 數量

        EffectManager.Instance.PlayUIEffect("L2", new Vector2(800, 0));
        EffectManager.Instance.PlayUIEffect("L2", new Vector2(-800, 0));

        if (healedCount > 0)
            dialogueText.text = $"<b>{skillManager.playerUnits[playerIndex].unitName}</b> 使用 <b>鮪魚肉泥</b> 恢復全體血量（共 +<b>{totalHealedAmount}</b> HP，治療了 <b>{healedCount}</b> 個單位）";
        else
            dialogueText.text = $"<b>{skillManager.playerUnits[playerIndex].unitName}</b> 使用 <b>鮪魚肉泥</b>，但全體已滿血，無效果";

        yield return new WaitForSeconds(2.0f);
    }

    private IEnumerator GrassCoroutine(int playerIndex, Unit target)
    {
        if (target == null || target.currentHP <= 0) yield break;

        skillManager.PlaySoundByName("catskill4_2");

        buffDebuffManager.ApplyCritChanceBuff(target, 0.5f, 2);
        buffDebuffManager.ApplyDefenseDebuff(target, 0.7f, 2);

        if (grass != null) grass.amount--; // ★ 扣除 ItemData 數量

        dialogueText.text = $"<b>{skillManager.playerUnits[playerIndex].unitName}</b> 使用 <b>貓草</b> 強化 <b>{target.unitName}</b> 的爆擊但削弱防禦";
        yield return new WaitForSeconds(1.8f);
    }

    private IEnumerator DryingCoroutine(int playerIndex, Unit target)
    {
        if (target == null || target.currentHP <= 0) yield break;

        skillManager.PlaySoundByName("catskill4_2");
        buffDebuffManager.ApplyDamageBuff(target, 1.3f, 2);

        if (drying != null) drying.amount--; // ★ 扣除 ItemData 數量

        dialogueText.text = $"<b>{skillManager.playerUnits[playerIndex].unitName}</b> 使用 <b>凍乾</b> 提升了 <b>{target.unitName}</b> 的攻擊力";
        yield return new WaitForSeconds(2.0f);
    }

    // ==================== 使用道具方法 ====================

    public void UseCan(int playerIndex, Unit target)
    {
        if (!ValidateItem(playerIndex, "Can") || !skillManager.ValidateTarget(playerIndex, target) || !skillManager.playerUnits.Contains(target)) return;
        skillManager.AddSkillToQueue(playerIndex, "Can", true, CanCoroutine(playerIndex, target));

        dialogueText.text = $"<b>{skillManager.playerUnits[playerIndex].unitName}</b> 選擇了 <b>神聖罐罐</b> 對 <b>{target.unitName}</b>\n恢復目標 「已損失血量」的 60%";
        skillManager.RemoveSkillIcon();
        skillManager.UpdateAllSkillButtonStates();
    }

    public void UseTuna(int playerIndex)
    {
        if (!ValidateItem(playerIndex, "Tuna")) return;
        skillManager.AddSkillToQueue(playerIndex, "Tuna", false, TunaCoroutine(playerIndex));

        skillManager.dialogueText.text = $"<b>{skillManager.playerUnits[playerIndex].unitName}</b> 選擇了 <b>鮪魚肉泥</b>\n恢復我方存活全體成員 「最大血量」的 30%";
        skillManager.RemoveSkillIcon(skillManager.GetSkillCost("Tuna"));
        skillManager.UpdateAllSkillButtonStates();
    }

    public void UseGrass(int playerIndex, Unit target)
    {
        if (!ValidateItem(playerIndex, "Grass") || !skillManager.ValidateTarget(playerIndex, target) || !skillManager.playerUnits.Contains(target)) return;
        skillManager.AddSkillToQueue(playerIndex, "Grass", true, GrassCoroutine(playerIndex, target));

        dialogueText.text = $"<b>{skillManager.playerUnits[playerIndex].unitName}</b> 選擇了 <b>貓草</b> 對 <b>{target.unitName}</b>\n爆擊提升為原本的 1.5 倍，但防禦下降 30%\n持續時間:2回合";
        skillManager.RemoveSkillIcon();
        skillManager.UpdateAllSkillButtonStates();
    }

    public void UseDrying(int playerIndex, Unit target)
    {
        if (!ValidateItem(playerIndex, "Drying") || !skillManager.ValidateTarget(playerIndex, target) || !skillManager.playerUnits.Contains(target)) return;
        skillManager.AddSkillToQueue(playerIndex, "Drying", true, DryingCoroutine(playerIndex, target));

        dialogueText.text = $"<b>{skillManager.playerUnits[playerIndex].unitName}</b> 選擇了 <b>凍乾</b> 對 <b>{target.unitName}</b>\n目標的攻擊力提升 30%\n持續時間：2 回合";
        skillManager.RemoveSkillIcon();
        skillManager.UpdateAllSkillButtonStates();
    }

    // ==================== UI 觸發方法 ====================

    public void TriggerCanForPlayer(int playerIndex)
    {
        if (skillManager.battlesystem.state != BattleState.PLAYERTURN) return;

        if (skillManager.selectedSkillNames[playerIndex].Contains("Can")) CancelItem(playerIndex, "Can");
        else if (HasItem("Can"))
        {
            skillManager.isWaitingForTarget = true;
            skillManager.pendingSkill = "Can";
            skillManager.pendingPlayerIndex = playerIndex;
            dialogueText.text = "請點擊一個玩家頭像以使用 <b>神聖罐罐</b>！";
        }
        else dialogueText.text = "沒有 <b>神聖罐罐</b> 可使用！";
    }

    public void TriggerTunaForPlayer(int playerIndex)
    {
        if (Time.time - lastTunaClickTime < 0.2f) return;
        lastTunaClickTime = Time.time;

        if (skillManager.battlesystem.state != BattleState.PLAYERTURN) return;

        if (skillManager.selectedSkillNames[playerIndex].Contains("Tuna"))
        {
            CancelItem(playerIndex, "Tuna");
            return;
        }

        if (!HasItem("Tuna"))
        {
            dialogueText.text = "沒有 <b>鮪魚肉泥</b> 可使用！";
            return;
        }
        UseTuna(playerIndex);
    }

    public void TriggerGrassForPlayer(int playerIndex)
    {
        if (skillManager.battlesystem.state != BattleState.PLAYERTURN) return;

        if (skillManager.selectedSkillNames[playerIndex].Contains("Grass")) CancelItem(playerIndex, "Grass");
        else if (HasItem("Grass"))
        {
            skillManager.isWaitingForTarget = true;
            skillManager.pendingSkill = "Grass";
            skillManager.pendingPlayerIndex = playerIndex;
            dialogueText.text = "請點擊一個玩家頭像以使用 <b>貓草</b>！";
        }
        else dialogueText.text = "沒有 <b>貓草</b> 可使用！";
    }

    public void TriggerDryingForPlayer(int playerIndex)
    {
        if (skillManager.battlesystem.state != BattleState.PLAYERTURN) return;

        if (skillManager.selectedSkillNames[playerIndex].Contains("Drying")) CancelItem(playerIndex, "Drying");
        else if (HasItem("Drying"))
        {
            skillManager.isWaitingForTarget = true;
            skillManager.pendingSkill = "Drying";
            skillManager.pendingPlayerIndex = playerIndex;
            dialogueText.text = "請點擊一個玩家頭像以使用 <b>凍乾</b>！";
        }
        else dialogueText.text = "沒有 <b>凍乾</b> 可使用！";
    }

    // ==================== 字典防呆輔助 (給其他腳本呼叫用) ====================

    public Dictionary<string, int> GetAllItemCounts()
    {
        Dictionary<string, int> dict = new Dictionary<string, int>();
        if (can != null) dict["Can"] = can.amount;
        if (tuna != null) dict["Tuna"] = tuna.amount;
        if (grass != null) dict["Grass"] = grass.amount;
        if (drying != null) dict["Drying"] = drying.amount;
        return dict;
    }

    public void AddItem(string itemName, int amount)
    {
        if (itemName == "Can" && can != null) can.amount += amount;
        else if (itemName == "Tuna" && tuna != null) tuna.amount += amount;
        else if (itemName == "Grass" && grass != null) grass.amount += amount;
        else if (itemName == "Drying" && drying != null) drying.amount += amount;
    }

    public void RemoveItem(string itemName, int amount)
    {
        if (itemName == "Can" && can != null) can.amount = Mathf.Max(0, can.amount - amount);
        else if (itemName == "Tuna" && tuna != null) tuna.amount = Mathf.Max(0, tuna.amount - amount);
        else if (itemName == "Grass" && grass != null) grass.amount = Mathf.Max(0, grass.amount - amount);
        else if (itemName == "Drying" && drying != null) drying.amount = Mathf.Max(0, drying.amount - amount);
    }

    public void redbookuse()
    {
        if (GameStateManager.Instance.IsState(GameState.Battle))
        {
            StartCoroutine(redbook.OpenBook());
            if (redbook.inventoryUI != null)
                redbook.inventoryUI.GetComponent<InventoryUI>().RefreshUI();
        }
    }
}