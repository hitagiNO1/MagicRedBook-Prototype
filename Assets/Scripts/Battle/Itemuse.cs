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

    // 物品庫存管理
    private Dictionary<string, int> itemInventory = new Dictionary<string, int>(); // 物品庫存

    void Start()
    {
        // 從 ItemData 讀取道具數量並初始化庫存
        if (can != null)
        {
            itemInventory["Can"] = can.amount;
            Debug.Log($"初始化 Can 數量：{can.amount}");
        }
        else
        {
            Debug.LogWarning("Can ItemData 未指定！");
        }

        if (tuna != null)
        {
            itemInventory["Tuna"] = tuna.amount;
            Debug.Log($"初始化 Tuna 數量：{tuna.amount}");
        }
        else
        {
            Debug.LogWarning("Tuna ItemData 未指定！");
        }

        if (grass != null)
        {
            itemInventory["Grass"] = grass.amount;
            Debug.Log($"初始化 Grass 數量：{grass.amount}");
        }
        else
        {
            Debug.LogWarning("Grass ItemData 未指定！");
        }

        if (drying != null)
        {
            itemInventory["Drying"] = drying.amount;
            Debug.Log($"初始化 Drying 數量：{drying.amount}");
        }
        else
        {
            Debug.LogWarning("Drying ItemData 未指定！");
        }

        // 驗證所有物品庫存是否正確初始化
        Debug.Log("=== 物品庫存初始化完成 ===");
        foreach (var item in itemInventory)
        {
            Debug.Log($"{item.Key}: {item.Value} 個");
        }
    }



    // 驗證物品是否可用（類似 ValidateSkill）
    private bool ValidateItem(int playerIndex, string itemName)
    {
        if (!itemInventory.ContainsKey(itemName) || itemInventory[itemName] <= 0)
        {
            dialogueText.text = $"沒有 {itemName} 可使用！";
            Debug.LogWarning($"No {itemName} left in inventory!");
            return false;
        }
        if (skillManager.GetTotalSelectedSkills() >= skillManager.maxSkillPerTurn)
        {
            dialogueText.text = "技能欄位已滿！";
            return false;
        }
        if (skillManager.selectedSkillNames[playerIndex].Contains(itemName))
        {
            CancelItem(playerIndex, itemName);
            return false;
        }
        return true;
    }

    // 取消物品（類似 CancelSkill）
    private void CancelItem(int playerIndex, string itemName)
    {
        if (skillManager.selectedSkillNames[playerIndex].Contains(itemName))
        {
            int index = skillManager.selectedSkillNames[playerIndex].IndexOf(itemName);
            skillManager.selectedSkillNames[playerIndex].RemoveAt(index);
            skillManager.skillQueues[playerIndex].RemoveAt(index);
            if (skillManager.selectedTargets.ContainsKey(playerIndex) && skillManager.selectedTargets[playerIndex] != null)
            {
                skillManager.HighlightTarget(skillManager.selectedTargets[playerIndex], false);
                skillManager.selectedTargets[playerIndex] = null;
            }
            dialogueText.text = $"{skillManager.playerUnits[playerIndex].unitName} 取消了 {itemName}";
            skillManager.AddSkillIcon(); // 恢復圖示
            Debug.Log($"Player {playerIndex} cancelled item: {itemName}");
        }
    }

    // 使用 Can（選擇一名玩家單位回復已損失血量的60%血量）
    public void UseCan(int playerIndex, Unit target)
    {
        if (!ValidateItem(playerIndex, "Can") || !skillManager.ValidateTarget(playerIndex, target) || !skillManager.playerUnits.Contains(target))
        {
            return;
        }

        skillManager.selectedSkillNames[playerIndex].Add("Can");
        skillManager.skillQueues[playerIndex].Add(() =>
        {
            if (target.currentHP > 0)
            {
                // 執行治療：恢復已損失血量的 60%
                int lostHP = target.maxHP - target.currentHP;
                int healAmount = Mathf.RoundToInt(lostHP * 0.6f);
                target.currentHP = Mathf.Min(target.currentHP + healAmount, target.maxHP);

                // === 關鍵：更新血條 UI ===
                if (target.battleHUD != null)
                {
                    target.battleHUD.SetHP(target.currentHP);
                }
                else
                {
                    Debug.LogWarning($"目標 {target.unitName} 沒有綁定 battleHUD！");
                }

                // 減少物品庫存
                itemInventory["Can"]--;

                dialogueText.text = $"{skillManager.playerUnits[playerIndex].unitName} 使用 神聖罐罐 恢復 {target.unitName} 的血量（+{healAmount}）";
                Debug.Log($"使用 神聖罐罐，恢復 {healAmount} HP，剩餘數量：{itemInventory["Can"]}");
            }
        });

        dialogueText.text = $"{skillManager.playerUnits[playerIndex].unitName} 選擇了 神聖罐罐 對 {target.unitName}";
        skillManager.RemoveSkillIcon();
    }

    // 使用 Tuna（回復我方全體單位自身最大生命30%的血量）
    public void UseTuna(int playerIndex)
    {
        if (!ValidateItem(playerIndex, "Tuna")) return;

        skillManager.selectedSkillNames[playerIndex].Add("Tuna");
        skillManager.skillQueues[playerIndex].Add(() =>
        {
            int totalHealedAmount = 0;   // 統計總共恢復多少血
            int healedCount = 0;         // 統計治療了幾個單位

            foreach (Unit player in skillManager.playerUnits)
            {
                if (player.currentHP > 0 && player.currentHP < player.maxHP) // 只治療有損血的單位（可選）
                {
                    int healAmount = Mathf.RoundToInt(player.maxHP * 0.3f); // 30% 最大生命
                    int actualHeal = Mathf.Min(healAmount, player.maxHP - player.currentHP); // 實際恢復量（不會超過滿血）

                    player.currentHP += actualHeal;
                    totalHealedAmount += actualHeal;
                    healedCount++;

                    // 更新血條 UI
                    if (player.battleHUD != null)
                    {
                        player.battleHUD.SetHP(player.currentHP);
                    }
                }
            }

            // 減少物品庫存
            itemInventory["Tuna"]--;

            // 更清楚的對話顯示
            if (healedCount > 0)
            {
                dialogueText.text = $"{skillManager.playerUnits[playerIndex].unitName} 使用 Tuna 恢復全體血量（共 +{totalHealedAmount} HP，治療了 {healedCount} 個單位）";
                Debug.Log($"使用 Tuna，總恢復 {totalHealedAmount} HP，治療 {healedCount} 個單位，剩餘數量：{itemInventory["Tuna"]}");
            }
            else
            {
                dialogueText.text = $"{skillManager.playerUnits[playerIndex].unitName} 使用 Tuna，但全體已滿血，無效果";
                Debug.Log($"使用 Tuna，但無單位需要治療，剩餘數量：{itemInventory["Tuna"]}");
            }
        });

        dialogueText.text = $"{skillManager.playerUnits[playerIndex].unitName} 選擇了 Tuna";
        skillManager.RemoveSkillIcon();
    }

    // 使用 Grass（選擇一名玩家單位提升自身爆擊率50%並減少自身30%的防禦力，持續兩回合）
    public void UseGrass(int playerIndex, Unit target)
    {
        if (!ValidateItem(playerIndex, "Grass") || !skillManager.ValidateTarget(playerIndex, target) || !skillManager.playerUnits.Contains(target))
        {
            return;
        }

        skillManager.selectedSkillNames[playerIndex].Add("Grass");
        skillManager.skillQueues[playerIndex].Add(() =>
        {
            if (target.currentHP > 0)
            {
                // 提升爆擊率 50%
                buffDebuffManager.ApplyCritChanceBuff(target, 0.5f, 2);

                // 減少防禦力 30%
                buffDebuffManager.ApplyDefenseDebuff(target, 0.7f, 2);

                itemInventory["Grass"]--; // 減少庫存
                dialogueText.text = $"{skillManager.playerUnits[playerIndex].unitName} 使用 Grass 強化 {target.unitName} 的爆擊但削弱防禦";
                Debug.Log($"使用 Grass，剩餘數量：{itemInventory["Grass"]}");
                
            }
        });
        dialogueText.text = $"{skillManager.playerUnits[playerIndex].unitName} 選擇了 Grass 對 {target.unitName}";
        skillManager.RemoveSkillIcon();
    }

    // 使用 Drying（選擇一名玩家單位提升自身攻擊力300%，持續兩回合）
    public void UseDrying(int playerIndex, Unit target)
    {
        if (!ValidateItem(playerIndex, "Drying") || !skillManager.ValidateTarget(playerIndex, target) || !skillManager.playerUnits.Contains(target))
        {
            return;
        }

        skillManager.selectedSkillNames[playerIndex].Add("Drying");
        skillManager.skillQueues[playerIndex].Add(() =>
        {
            if (target.currentHP > 0)
            {
                // 提升攻擊力 300%（即 x4）
                buffDebuffManager.ApplyDamageBuff(target, 4.0f, 2);

                itemInventory["Drying"]--; // 減少庫存
                dialogueText.text = $"{skillManager.playerUnits[playerIndex].unitName} 使用 Drying 大幅提升 {target.unitName} 的攻擊力";
                Debug.Log($"使用 Drying，剩餘數量：{itemInventory["Drying"]}");
               
            }
        });
        dialogueText.text = $"{skillManager.playerUnits[playerIndex].unitName} 選擇了 Drying 對 {target.unitName}";
        skillManager.RemoveSkillIcon();
    }

    // UI 觸發方法：Can，需要等待目標
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
            dialogueText.text = "請點擊一個玩家頭像以使用 Can！";
        }
        else
        {
            dialogueText.text = "沒有 Can 可使用！";
        }
    }

    // UI 觸發方法：Tuna，無需目標
    public void TriggerTunaForPlayer(int playerIndex)
    {
        if (skillManager.battlesystem.state != BattleState.PLAYERTURN) return;
        if (skillManager.selectedSkillNames[playerIndex].Contains("Tuna"))
        {
            CancelItem(playerIndex, "Tuna");
        }
        else if (HasItem("Tuna"))
        {
            UseTuna(playerIndex);
        }
        else
        {
            dialogueText.text = "沒有 Tuna 可使用！";
        }
    }

    // UI 觸發方法：Grass，需要等待目標
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
            dialogueText.text = "請點擊一個玩家頭像以使用 Grass！";
        }
        else
        {
            dialogueText.text = "沒有 Grass 可使用！";
        }
    }

    // UI 觸發方法：Drying，需要等待目標
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
            dialogueText.text = "請點擊一個玩家頭像以使用 Drying！";
        }
        else
        {
            dialogueText.text = "沒有 Drying 可使用！";
        }
    }

    // 獲取物品庫存數量
    public int GetItemCount(string itemName)
    {
        return itemInventory.ContainsKey(itemName) ? itemInventory[itemName] : 0;
    }

    // 檢查物品是否可用
    public bool HasItem(string itemName)
    {
        return itemInventory.ContainsKey(itemName) && itemInventory[itemName] > 0;
    }

    // 獲取所有物品的庫存狀態（用於 UI 顯示）
    public Dictionary<string, int> GetAllItemCounts()
    {
        return new Dictionary<string, int>(itemInventory);
    }

    // 添加物品（用於遊戲中獲得新物品）
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

    // 移除物品（用於測試或特殊情況）
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
            // 打開紅書 
            StartCoroutine(redbook.OpenBook());
            // 更新道具UI
            if (redbook.inventoryUI != null)
                redbook.inventoryUI.GetComponent<InventoryUI>().RefreshUI();
        }
    }
}