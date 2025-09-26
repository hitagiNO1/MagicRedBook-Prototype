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
        //Debug.Log($"防禦 {defense}，減傷比例：{reduction}");
    }

    public bool TakeDamage(int dmg)
    {
      
        // 應用 Buff/Debuff 減傷
        int reducedDamage = buffManager != null ? buffManager.ApplyReductionIfApplicable(this, dmg) : dmg;
     

        // 計算爆擊
        bool isCritical = Random.value < critChance;
       

        // 計算防禦減傷比例
        float reduction = defenseData.GetDamageReduction(defense);
      

        // 計算最終傷害
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
        // 新增：確保傷害不為負值
        finalDMG = Mathf.Max(finalDMG, 0);
        Debug.Log($"最終計算傷害={finalDMG}");

        // 扣血
        currentHP -= finalDMG;
        Debug.Log($"扣血後當前HP={currentHP}");

        // 更新 HUD
        if (battleHUD != null)
        {
            battleHUD.SetHP(currentHP);
          
        }
        else
        {
            Debug.LogWarning($"battleHUD 為 null，無法更新 HUD，但實際HP已更新為 {currentHP}");
        }

        // 檢查死亡並處理
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
