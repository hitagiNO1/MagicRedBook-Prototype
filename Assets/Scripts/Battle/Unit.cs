using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    public string unitName;
    public int unitLevel;
    public bool isPlayer;
    // --- 裝備相關變數 ---
    public List<string> equippedGear = new List<string>();

    [Header("Base Stats (For Recovery)")]
    public int baseDamage;          // 紀錄初始攻擊力
    public int baseDefense;         // 紀錄初始防禦力
    // ------------------

    public int damage;
    public DefenseData defenseData;
    public int defense;
    public int maxHP;
    public int currentHP;
    public float critChance = 0.25f;
    public float critdmgplus = 1.5f;
    public BuffDebuffManager buffManager;
    public BattleHUD battleHUD;

    public bool isDead = false;

    [HideInInspector] public string originalUnitName;

    private void Awake()
    {
        if (string.IsNullOrEmpty(originalUnitName))
        {
            originalUnitName = gameObject.name
                .Replace("(Clone)", "")
                .Trim();
        }
        isDead = false;

        // 初始化時記錄原始數值
        baseDamage = damage;
        baseDefense = defense;
    }

    private void Start()
    {
        // 如果你的裝備是在進戰鬥前就穿好的，
        // 可以在這裡呼叫一次 EquipmentManager (選做)
        // EquipmentManager.Instance.ApplyPermanentEquipmentEffects(this);
    }

    /// <summary>
    /// 還原數值到初始狀態 (當需要更換裝備或清除效果時使用)
    /// </summary>
    public void ResetStatsToOuterBase()
    {
        damage = baseDamage;
        defense = baseDefense;
        // 如果有速度基礎值也可以在此還原
    }

    // TakeDamage 部分保持不變...
    public bool TakeDamage(int dmg, out int damageDealt)
    {
        int reducedDamage = buffManager != null ? buffManager.ApplyReductionIfApplicable(this, dmg) : dmg;
        bool isCritical = Random.value < critChance;
        float reduction = defenseData.GetDamageReduction(defense);

        int finalDMG;
        if (isCritical)
        {
            float critDMG = reducedDamage * critdmgplus;
            float reducedCritDMG = critDMG * (1 - reduction);
            finalDMG = Mathf.RoundToInt(reducedCritDMG);
        }
        else
        {
            float reducedDMG = reducedDamage * (1 - reduction);
            finalDMG = Mathf.RoundToInt(reducedDMG);
        }

        finalDMG = Mathf.Max(finalDMG, 0);
        damageDealt = finalDMG;

        currentHP -= finalDMG;
        currentHP = Mathf.Max(currentHP, 0);

        if (battleHUD != null)
        {
            battleHUD.SetHP(currentHP);
        }

        // ==========================================
        // ★ 修改重點：移除直接呼叫 MarkEnemyAsDead 的邏輯
        // ==========================================
        if (currentHP <= 0)
        {
            // 注意：這裡先不要把 isDead 設為 true！
            // 讓它保持 false，確保後續排隊的技能在執行時，不會被其他防呆機制擋下來。
            // isDead 的狀態標記，會在你剛寫好的 BattleSystem.ProcessAllDeaths() 裡面統一處理。

            return true; // 回傳 true 代表「這下攻擊足以致死」
        }

        return false;
    }
}