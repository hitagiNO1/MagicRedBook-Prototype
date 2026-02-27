using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance { get; private set; }

    [Header("目前裝備清單")]
    // 這裡存放玩家目前穿戴的裝備名稱，你可以根據你的背包系統來對接
    public List<string> equippedItems = new List<string>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 檢查是否裝備了某件物品
    /// </summary>
    public bool HasEquipment(string itemName)
    {
        return equippedItems.Contains(itemName);
    }

    /// <summary>
    /// 戰鬥開始時呼叫：套用所有裝備的永久屬性（拳套、盔甲、法杖）
    /// </summary>
    public void ApplyPermanentEquipmentEffects(Unit unit)
    {
        if (unit == null) return;

        // --- 膠帶拳套：增加攻擊力 (例如 +5) ---
        if (HasEquipment("膠帶拳套"))
        {
            unit.damage += 5;
            Debug.Log($"{unit.unitName} 套用了膠帶拳套，攻擊力提升。");
        }

        // --- 穿戴式紙箱盔甲：增加防禦力 (例如 +3) ---
        if (HasEquipment("穿戴式紙箱盔甲"))
        {
            unit.defense += 3;
            Debug.Log($"{unit.unitName} 套用了紙箱盔甲，防禦力提升。");
        }

        // --- 逗貓棒法杖：增加額外傷害或魔力 (例如 +4) ---
        if (HasEquipment("逗貓棒法杖"))
        {
            unit.damage += 4;
            Debug.Log($"{unit.unitName} 套用了逗貓棒法杖，傷害提升。");
        }

        // 如果是玩家 Unit，我們也順便檢查並套用地圖移動速度
        if (unit.isPlayer)
        {
            RefreshOverworldSpeed();
        }
    }

    /// <summary>
    /// 專門處理地圖移動速度（劍客鞋子）
    /// 當玩家穿上或脫下鞋子時，呼叫此方法
    /// </summary>
    public void RefreshOverworldSpeed()
    {
        // 尋找場景中的主角控制器
        CharControlles playerController = FindObjectOfType<CharControlles>();

        if (playerController != null)
        {
            if (HasEquipment("劍客鞋子"))
            {
                playerController.SetSpeedBonus(2f); // 增加 2f 速度
            }
            else
            {
                playerController.SetSpeedBonus(0f); // 沒穿則重置
            }
        }
    }

    // 當你背包系統穿上裝備時，請呼叫此方法
    public void EquipItem(string itemName)
    {
        if (!equippedItems.Contains(itemName))
        {
            equippedItems.Add(itemName);
            RefreshOverworldSpeed(); // 穿上時立刻更新速度
        }
    }

    // 當你背包系統脫下裝備時，請呼叫此方法
    public void UnequipItem(string itemName)
    {
        if (equippedItems.Contains(itemName))
        {
            equippedItems.Remove(itemName);
            RefreshOverworldSpeed(); // 脫下時立刻更新速度
        }
    }
    public IEnumerator ExecuteTapeGlovesExtraDamage(int playerIndex, Unit target)
    {
        // 檢查是否裝備了膠帶拳套
        if (HasEquipment("膠帶拳套") && target != null && !target.isDead)
        {
            yield return new WaitForSeconds(0.2f); // 稍微延遲，讓視覺更有層次感

            int extraDamage = 3; // 設定額外傷害數值
            target.currentHP -= extraDamage;
            if (target.currentHP < 0) target.currentHP = 0;

            // 更新敵人的 HUD
            if (target.battleHUD != null)
            {
                target.battleHUD.SetHP(target.currentHP);
            }

            Debug.Log($"膠帶拳套發動！對 {target.unitName} 造成額外 {extraDamage} 點傷害。");

            // 如果敵人血量歸 0，觸發死亡邏輯
            if (target.currentHP <= 0)
            {
                // 呼叫 BattleSystem 標記死亡 (假設 BattleSystem 是單例或可獲取)
                FindObjectOfType<BattleSystem>()?.MarkEnemyAsDead(FindObjectOfType<BattleSystem>().enemyUnits.IndexOf(target));
            }

            yield return new WaitForSeconds(0.3f);
        }
    }
}