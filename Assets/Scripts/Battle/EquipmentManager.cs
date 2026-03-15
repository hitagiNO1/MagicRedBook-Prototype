using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance { get; private set; }

    [Header("玩家擁有的裝備清單 (可用於顯示在背包UI)")]
    public List<string> ownedEquipment = new List<string>();

    [Header("全隊使用中的裝備總覽")]
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

    public void AddEquipment(string itemName)
    {
        if (!ownedEquipment.Contains(itemName))
        {
            ownedEquipment.Add(itemName);
            Debug.Log($"獲得新裝備：{itemName}");
        }
    }

    public bool HasEquipment(string itemName)
    {
        return equippedItems.Contains(itemName);
    }

    // ================= 新增：幫指定單位穿上特定裝備 =================
    public void EquipItemToUnit(Unit unit, string itemName)
    {
        // ★ 新增檢查：如果在戰鬥中，就直接阻斷並返回
        if (GameStateManager.Instance != null && GameStateManager.Instance.IsState(GameState.Battle))
        {
            Debug.LogWarning("戰鬥中無法穿戴裝備！");
            // 如果你有全域的提示 UI，也可以在這裡呼叫，例如: ShowMessage("戰鬥中無法更換裝備");
            return;
        }

        if (unit == null || !ownedEquipment.Contains(itemName)) return;

        // 如果他已經穿著這件了，就不用再穿
        if (unit.equippedGear.Contains(itemName)) return;

        // 加入單位的裝備清單
        unit.equippedGear.Add(itemName);
        // 加入全域清單
        equippedItems.Add(itemName);

        // 重新計算屬性
        RecalculateUnitStats(unit);

        Debug.Log($"{unit.unitName} 裝備了 {itemName}！");
    }
    // ================= 新增：幫指定單位脫下裝備 =================
    public void UnequipItemFromUnit(Unit unit, string itemName)
    {
        // ★ 新增檢查：如果在戰鬥中，就直接阻斷並返回
        if (GameStateManager.Instance != null && GameStateManager.Instance.IsState(GameState.Battle))
        {
            Debug.LogWarning("戰鬥中無法脫下裝備！");
            return;
        }

        if (unit == null || !unit.equippedGear.Contains(itemName)) return;

        // 移除裝備
        unit.equippedGear.Remove(itemName);
        equippedItems.Remove(itemName);

        // 重新計算屬性
        RecalculateUnitStats(unit);

        Debug.Log($"{unit.unitName} 脫下了 {itemName}。");
    }



    // ================= 核心：重新計算單位的所有屬性 =================
    // 因為可以穿多件裝備，每次穿脫都要「先還原，再把所有穿著的裝備屬性加回去」
    private void RecalculateUnitStats(Unit unit)
    {
        // 1. 還原回最一開始的數值 (你在 Unit.cs 寫好的)
        unit.ResetStatsToOuterBase();

        // 2. 針對該單位目前穿著的每一件裝備，疊加屬性
        foreach (string equipment in unit.equippedGear)
        {
            if (equipment == "膠帶拳套")
            {
                unit.damage += 5;
            }
            if (equipment == "穿戴式紙箱盔甲")
            {
                unit.defense += 40;
            }
            if (equipment == "逗貓棒法杖")
            {
                unit.damage += 5;
            }
        }

        // 3. 更新地圖速度和技能按鈕狀態
        if (unit.isPlayer)
        {
            RefreshOverworldSpeed();
        }
        FindObjectOfType<SkillManager>()?.UpdateAllSkillButtonStates();
    }

    // ================= 這個已經整合進 RecalculateUnitStats 了，可以保留或刪除 =================
    // 為了保險起見，保留一個相容原本呼叫的方法，並將其導向重新計算
    public void ApplyPermanentEquipmentEffects(Unit unit)
    {
        RecalculateUnitStats(unit);
    }

    public void RefreshOverworldSpeed()
    {
        CharControlles playerController = FindObjectOfType<CharControlles>();

        if (playerController != null)
        {
            // 劍客鞋子的效果已移至戰鬥 AP 判定，不再影響地圖跑速
            playerController.SetSpeedBonus(0f);
        }
    }

    // ================= 修改：二段傷害判定改看 equippedGear 清單 =================
    public IEnumerator ExecuteTapeGlovesExtraDamage(int playerIndex, Unit target)
    {
        SkillManager skillManager = FindObjectOfType<SkillManager>();
        if (skillManager == null) yield break;

        Unit attackingUnit = skillManager.playerUnits[playerIndex];

        // 檢查攻擊者的裝備清單是否包含拳套
        if (attackingUnit.equippedGear.Contains("膠帶拳套") && target != null && !target.isDead)
        {
            yield return new WaitForSeconds(0.2f);

            int extraDamage = 10;
            target.currentHP -= extraDamage;
            if (target.currentHP < 0) target.currentHP = 0;

            if (target.battleHUD != null)
            {
                target.battleHUD.SetHP(target.currentHP);
            }

            Debug.Log($"膠帶拳套發動！對 {target.unitName} 造成額外 {extraDamage} 點傷害。");

            // [修正1] 使用已經宣告好的區域變數 skillManager
            if (skillManager.enemyUnits.Count == 1)
            {
                EffectManager.Instance.PlayUIEffect("B1", new Vector2(0, 0));
            }
            else
            {
                // [修正2] 加上 skillManager. 前綴
                int targetIndexForEffect = skillManager.enemyUnits.IndexOf(target);
                bool isTargetLeft = (targetIndexForEffect == 0);
                EffectManager.Instance.PlayUIEffectOneSide("B1", isTargetLeft);
            }
            skillManager.PlaySoundByName("softskill1");
            // [修正3] 使用 $ 符號進行字串插值，把變數包在字串內
            skillManager.dialogueText.text = $"<b>膠帶拳套</b>發動！\n對 <b>{target.unitName}</b> 造成額外 <b>{extraDamage}</b> 點傷害。";

            yield return new WaitForSeconds(1.5f); // 原本的等待

            if (target.currentHP <= 0)
            {
                FindObjectOfType<BattleSystem>()?.MarkEnemyAsDead(FindObjectOfType<BattleSystem>().enemyUnits.IndexOf(target));
            }

            yield return new WaitForSeconds(0.3f);
        }
    }
}