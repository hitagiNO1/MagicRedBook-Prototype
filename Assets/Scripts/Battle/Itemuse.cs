using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Itemuse : MonoBehaviour
{
    public RedBook redbook;
    public SkillManager skillManager; // 引用 SkillManager
    public BuffDebuffManager buffDebuffManager; // 引用 BuffDebuffManager 以執行效果
    public TextMeshProUGUI dialogueText; // 用於顯示訊息
    public List<BattleHUD> playerHUDs;
    public List<Unit> playerUnits;
    public ItemData can;
    public ItemData tuna;
    public ItemData grass;
    public ItemData drying;

    private float lastTunaClickTime = 0f;

    // 物品庫存管理
    private Dictionary<string, int> itemInventory = new Dictionary<string, int>(); // 物品庫存

    void Start()
    {
        // 從 ItemData 讀取道具數量並初始化庫存
        if (can != null)
        {
            itemInventory["Can"] = can.amount;
        }
        else
        {
            Debug.LogWarning("Can ItemData 未指定！");
        }
        if (tuna != null)
        {
            itemInventory["Tuna"] = tuna.amount;
        }
        else
        {
            Debug.LogWarning("Tuna ItemData 未指定！");
        }
        if (grass != null)
        {
            itemInventory["Grass"] = grass.amount;
        }
        else
        {
            Debug.LogWarning("Grass ItemData 未指定！");
        }
        if (drying != null)
        {
            itemInventory["Drying"] = drying.amount;
        }
        else
        {
            Debug.LogWarning("Drying ItemData 未指定！");
        }

        Debug.Log("=== 物品庫存初始化完成 ===");
        foreach (var item in itemInventory)
        {
            Debug.Log($"{item.Key}: {item.Value} 個");
        }
    }

    // 驗證物品是否可用
    private bool ValidateItem(int playerIndex, string itemName)
    {
        if (!itemInventory.ContainsKey(itemName) || itemInventory[itemName] <= 0)
        {
            dialogueText.text = $"沒有 <b>{itemName}</b> 可使用！";
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
    // 取消物品
    private void CancelItem(int playerIndex, string itemName)
    {
        if (skillManager.selectedSkillNames[playerIndex].Contains(itemName))
        {
            int index = skillManager.selectedSkillNames[playerIndex].IndexOf(itemName);
            skillManager.selectedSkillNames[playerIndex].RemoveAt(index);
            skillManager.skillQueues[playerIndex].RemoveAt(index);
            skillManager.skillNeedsTarget[playerIndex].RemoveAt(index);

            if (skillManager.selectedTargets.ContainsKey(playerIndex) && skillManager.selectedTargets[playerIndex] != null)
            {
                skillManager.HighlightTarget(skillManager.selectedTargets[playerIndex], false);
                skillManager.selectedTargets[playerIndex] = null;
            }

            // --- 新增：將代號翻譯成中文 ---
            string displayName = itemName;
            if (itemName == "Can") displayName = "神聖罐罐";
            else if (itemName == "Tuna") displayName = "鮪魚肉泥";
            else if (itemName == "Grass") displayName = "貓草";
            else if (itemName == "Drying") displayName = "凍乾";

            dialogueText.text = $"<b>{skillManager.playerUnits[playerIndex].unitName}</b> 取消了 <b>{displayName}</b>";
            // -----------------------------

            skillManager.AddSkillIcon(skillManager.GetSkillCost(itemName)); // 順便幫你補上 AP 退還
            skillManager.UpdateAllSkillButtonStates();
        }
    }

    // ==================== 道具協程 ====================

    private IEnumerator CanCoroutine(int playerIndex, Unit target)
    {
        if (target == null || target.currentHP <= 0) yield break;

        int lostHP = target.maxHP - target.currentHP;
        int healAmount = Mathf.RoundToInt(lostHP * 0.6f);
        target.currentHP = Mathf.Min(target.currentHP + healAmount, target.maxHP);

        if (target.battleHUD != null)
        {
            target.battleHUD.SetHP(target.currentHP);
        }

        can.amount--;
        itemInventory["Can"] = can.amount;

        int targetIndex = skillManager.playerUnits.IndexOf(target);
        bool isTargetLeft = (targetIndex == 0);
        EffectManager.Instance.PlayUIEffectOneSide("L2", isTargetLeft);

        dialogueText.text = $"<b>{skillManager.playerUnits[playerIndex].unitName}</b> 使用 <b>神聖罐罐</b> 恢復 <b>{target.unitName}</b> 的血量（+<b>{healAmount}</b>）";

        yield return new WaitForSeconds(1.8f); // 調整特效時間
    }

    private IEnumerator TunaCoroutine(int playerIndex)
    {
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

                if (player.battleHUD != null)
                {
                    player.battleHUD.SetHP(player.currentHP);
                }
            }
        }

        tuna.amount--;
        itemInventory["Tuna"] = tuna.amount;

        EffectManager.Instance.PlayUIEffect("L2", new Vector2(800, 0));
        EffectManager.Instance.PlayUIEffect("L2", new Vector2(-800, 0));

        if (healedCount > 0)
        {
            dialogueText.text = $"<b>{skillManager.playerUnits[playerIndex].unitName}</b> 使用 <b>鮪魚肉泥</b> 恢復全體血量（共 +<b>{totalHealedAmount}</b> HP，治療了 <b>{healedCount}</b> 個單位）";
        }
        else
        {
            dialogueText.text = $"<b>{skillManager.playerUnits[playerIndex].unitName}</b> 使用 <b>鮪魚肉泥</b>，但全體已滿血，無效果";
        }

        yield return new WaitForSeconds(2.0f);
    }

    private IEnumerator GrassCoroutine(int playerIndex, Unit target)
    {
        if (target == null || target.currentHP <= 0) yield break;

        buffDebuffManager.ApplyCritChanceBuff(target, 0.5f, 2);
        buffDebuffManager.ApplyDefenseDebuff(target, 0.7f, 2);

        grass.amount--;
        itemInventory["Grass"] = grass.amount;

        dialogueText.text = $"<b>{skillManager.playerUnits[playerIndex].unitName}</b> 使用 <b>貓草</b> 強化 <b>{target.unitName}</b> 的爆擊但削弱防禦";

        // 可加入特效
        // EffectManager.Instance.PlayUIEffect("GrassEffect", ...);

        yield return new WaitForSeconds(1.8f);
    }

    private IEnumerator DryingCoroutine(int playerIndex, Unit target)
    {
        if (target == null || target.currentHP <= 0) yield break;

        // ★ 將倍率 4.0f 改為 1.3f (代表提升現有攻擊力 30%)
        buffDebuffManager.ApplyDamageBuff(target, 1.3f, 2);

        drying.amount--;
        itemInventory["Drying"] = drying.amount;

        // ★ 順便修改對話文字，讓敘述更準確
        dialogueText.text = $"<b>{skillManager.playerUnits[playerIndex].unitName}</b> 使用 <b>凍乾</b> 提升了 <b>{target.unitName}</b> 的攻擊力";

        // 可加入特效
        // EffectManager.Instance.PlayUIEffect("DryingEffect", ...);

        yield return new WaitForSeconds(2.0f);
    }

    // ==================== 使用道具方法 ====================

    public void UseCan(int playerIndex, Unit target)
    {
        if (!ValidateItem(playerIndex, "Can") || !skillManager.ValidateTarget(playerIndex, target) || !skillManager.playerUnits.Contains(target))
        {
            return;
        }

        skillManager.selectedSkillNames[playerIndex].Add("Can");
        skillManager.skillNeedsTarget[playerIndex].Add(true);
        skillManager.skillQueues[playerIndex].Add(CanCoroutine(playerIndex, target));

        dialogueText.text = $"<b>{skillManager.playerUnits[playerIndex].unitName}</b> 選擇了 <b>神聖罐罐</b> 對 <b>{target.unitName}</b>\n"+
                            $"恢復目標 「已損失血量」的 60%";
        skillManager.RemoveSkillIcon();
        skillManager.UpdateAllSkillButtonStates();
    }

    public void UseTuna(int playerIndex)
    {
        if (!ValidateItem(playerIndex, "Tuna")) return;

        skillManager.selectedSkillNames[playerIndex].Add("Tuna");
        skillManager.skillNeedsTarget[playerIndex].Add(false);
        skillManager.skillQueues[playerIndex].Add(TunaCoroutine(playerIndex));

        // 在 dialogueText 前面加上 skillManager. 確保抓到正確的文字框
        skillManager.dialogueText.text = $"<b>{skillManager.playerUnits[playerIndex].unitName}</b> 選擇了 <b>鮪魚肉泥</b>\n恢復我方存活全體成員 「最大血量」的 30%";

        skillManager.RemoveSkillIcon(skillManager.GetSkillCost("Tuna")); // 順便幫你套用最新版的 AP 消耗寫法
        skillManager.UpdateAllSkillButtonStates();
    }

    public void UseGrass(int playerIndex, Unit target)
    {
        if (!ValidateItem(playerIndex, "Grass") || !skillManager.ValidateTarget(playerIndex, target) || !skillManager.playerUnits.Contains(target))
        {
            return;
        }

        skillManager.selectedSkillNames[playerIndex].Add("Grass");
        skillManager.skillNeedsTarget[playerIndex].Add(true);
        skillManager.skillQueues[playerIndex].Add(GrassCoroutine(playerIndex, target));

        dialogueText.text = $"<b>{skillManager.playerUnits[playerIndex].unitName}</b> 選擇了 <b>貓草</b> 對 <b>{target.unitName}</b>\n"+
                            $"爆擊提升為原本的 1.5 倍，但防禦下降 30%\n" +
                            $"持續時間:2回合";
        skillManager.RemoveSkillIcon();
        skillManager.UpdateAllSkillButtonStates();
    }

    public void UseDrying(int playerIndex, Unit target)
    {
        if (!ValidateItem(playerIndex, "Drying") || !skillManager.ValidateTarget(playerIndex, target) || !skillManager.playerUnits.Contains(target))
        {
            return;
        }

        skillManager.selectedSkillNames[playerIndex].Add("Drying");
        skillManager.skillNeedsTarget[playerIndex].Add(true);
        skillManager.skillQueues[playerIndex].Add(DryingCoroutine(playerIndex, target));

        dialogueText.text = $"<b>{skillManager.playerUnits[playerIndex].unitName}</b> 選擇了 <b>凍乾</b> 對 <b>{target.unitName}</b>\n"+
                            $"目標的攻擊力提升 30%\n"+
                            $"持續時間：2 回合";
        skillManager.RemoveSkillIcon();
        skillManager.UpdateAllSkillButtonStates();
    }

    // ==================== UI 觸發方法 ====================

    public void TriggerCanForPlayer(int playerIndex)
    {
        if (skillManager.battlesystem.state != BattleState.PLAYERTURN) return;

        if (skillManager.selectedSkillNames[playerIndex].Contains("Can"))
        {
            CancelItem(playerIndex, "Can");
        }
        else if (HasItem("Can"))
        {
            skillManager.isWaitingForTarget = true;
            skillManager.pendingSkill = "Can";
            skillManager.pendingPlayerIndex = playerIndex;
            dialogueText.text = "請點擊一個玩家頭像以使用 神聖罐罐！";
        }
        else
        {
            dialogueText.text = "沒有 <b>神聖罐罐</b> 可使用！";
        }
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

        if (skillManager.selectedSkillNames[playerIndex].Contains("Grass"))
        {
            CancelItem(playerIndex, "Grass");
        }
        else if (HasItem("Grass"))
        {
            skillManager.isWaitingForTarget = true;
            skillManager.pendingSkill = "Grass";
            skillManager.pendingPlayerIndex = playerIndex;
            dialogueText.text = "請點擊一個玩家頭像以使用 <b>貓草</b>！";
        }
        else
        {
            dialogueText.text = "沒有 <b>貓草</b> 可使用！";
        }
    }

    public void TriggerDryingForPlayer(int playerIndex)
    {
        if (skillManager.battlesystem.state != BattleState.PLAYERTURN) return;

        if (skillManager.selectedSkillNames[playerIndex].Contains("Drying"))
        {
            CancelItem(playerIndex, "Drying");
        }
        else if (HasItem("Drying"))
        {
            skillManager.isWaitingForTarget = true;
            skillManager.pendingSkill = "Drying";
            skillManager.pendingPlayerIndex = playerIndex;
            dialogueText.text = "請點擊一個玩家頭像以使用 <b>凍乾</b>！";
        }
        else
        {
            dialogueText.text = "沒有 <b>凍乾</b> 可使用！";
        }
    }

    // ==================== 其他工具方法 ====================

    public int GetItemCount(string itemName)
    {
        return itemInventory.ContainsKey(itemName) ? itemInventory[itemName] : 0;
    }

    public bool HasItem(string itemName)
    {
        return itemInventory.ContainsKey(itemName) && itemInventory[itemName] > 0;
    }

    public Dictionary<string, int> GetAllItemCounts()
    {
        return new Dictionary<string, int>(itemInventory);
    }

    public void AddItem(string itemName, int amount)
    {
        if (itemInventory.ContainsKey(itemName))
        {
            itemInventory[itemName] += amount;
        }
        else
        {
            itemInventory[itemName] = amount;
        }
        Debug.Log($"獲得 {amount} 個 {itemName}，當前數量：{itemInventory[itemName]}");
    }

    public void RemoveItem(string itemName, int amount)
    {
        if (itemInventory.ContainsKey(itemName))
        {
            itemInventory[itemName] = Mathf.Max(0, itemInventory[itemName] - amount);
            Debug.Log($"移除 {amount} 個 {itemName}，剩餘數量：{itemInventory[itemName]}");
        }
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