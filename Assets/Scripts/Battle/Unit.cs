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
    public BuffDebuffManager buffManager; // 引用 BuffDebuffManager

    private void Start()
    {
        float reduction = defenseData.GetDamageReduction(defense);
        Debug.Log($"防禦 {defense}，減傷比例：{reduction}");
    }

    public bool TakeDamage(int dmg)
    {
        Debug.Log($"TakeDamage 開始時敵方防禦力 = {defense}");

        // Step 1: 應用固定減傷效果（例如 catSkill_5 的減傷）
        int reducedDamage = buffManager != null ? buffManager.ApplyReductionIfApplicable(this, dmg) : dmg;

        // Step 2: 計算爆擊
        bool isCritical = Random.value < critChance; // 判斷是否爆擊
        float reduction = defenseData.GetDamageReduction(defense); // 計算防禦減傷比例
        int finalDMG;

        if (isCritical)
        {
            float critDMG = reducedDamage * critdmgplus; // 計算爆擊傷害（基於減傷後的傷害）
            float reducedCritDMG = critDMG * (1 - reduction); // 應用防禦減傷
            finalDMG = Mathf.RoundToInt(reducedCritDMG); // 轉為整數
        }
        else
        {
            float reducedDMG = reducedDamage * (1 - reduction); // 應用防禦減傷
            finalDMG = Mathf.RoundToInt(reducedDMG);
        }

        // Step 3: 扣除血量
        currentHP -= finalDMG;

        Debug.Log($"最終傷害：{finalDMG}，爆擊：{isCritical}，現在血量:{currentHP}");

        if (currentHP <= 0)
            return true;
        else
            return false;
    }
}