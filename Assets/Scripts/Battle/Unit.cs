using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    public string unitName;
    public int unitLevel;
    public bool isPlayer;
    // --- 裝備相關變數 ---
    public string currentEquipment; // 當前裝備名稱

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

        if (currentHP <= 0 && !isDead)
        {
            isDead = true;
            BattleSystem battleSystem = FindObjectOfType<BattleSystem>();
            if (battleSystem != null && battleSystem.enemyUnits.Contains(this))
            {
                int index = battleSystem.enemyUnits.IndexOf(this);
                if (index >= 0)
                {
                    battleSystem.MarkEnemyAsDead(index);
                }
            }
            return true;
        }

        return false;
    }
}