using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    public string unitName;
    public int unitLevel;
    public int damage;
    public DefenseData defenseData;
    public int defense;
    public int maxHP;
    public int currentHP;
    public float critChance = 0.25f;
    public float critdmgplus = 1.5f;
    public BuffDebuffManager buffManager;
    public BattleHUD battleHUD; // 新增對 BattleHUD 的引用

    private void Start()
    {
        float reduction = defenseData.GetDamageReduction(defense);
        Debug.Log($"防禦 {defense}，減傷比例：{reduction}");
    }

    public bool TakeDamage(int dmg)
    {
        Debug.Log($"TakeDamage 開始時敵方防禦力 = {defense}");

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

        currentHP -= finalDMG;

        if (battleHUD != null)
        {
            battleHUD.SetHP(currentHP);
        }

        Debug.Log($"最終傷害：{finalDMG}，爆擊：{isCritical}，現在血量:{currentHP}");

        if (currentHP <= 0)
        {
            BattleSystem battleSystem = FindObjectOfType<BattleSystem>();
            if (battleSystem != null && battleSystem.enemyUnits.Contains(this))
            {
                int index = battleSystem.enemyUnits.IndexOf(this);
                battleSystem.DestroyEnemy(index); // 立即銷毀
            }
            return true;
        }
        return false;
    }
}