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

    private void Start()
    {
       float reduction = defenseData.GetDamageReduction(defense);
        Debug.Log($"防禦 {defense}，減傷比例：{reduction}");
    }

    public bool TakeDamage(int dmg)
    {
        Debug.Log($"TakeDamage 開始時敵方防禦力 = {defense}");
        int reducedDamage = buffManager.ApplyReductionIfApplicable(this, dmg);
        bool isCritical = Random.value < critChance; // 判斷是否爆擊
        float reduction = defenseData.GetDamageReduction(defense); // 計算減傷
        int finalDMG;

        if (isCritical)
        {
            float critDMG = dmg * critdmgplus; // 計算爆擊傷害
            float reducedCritDMG = critDMG * (1 - reduction); // 計算爆擊後的減傷
            finalDMG = Mathf.RoundToInt(reducedCritDMG); // 轉為整數
        }
        else
        {
            float reducedDMG = dmg * (1 - reduction); // 計算普通減傷
            finalDMG = Mathf.RoundToInt(reducedDMG);
        }

        currentHP -= finalDMG;

        Debug.Log($"最終傷害：{finalDMG}，爆擊：{isCritical}，現在血量:{currentHP}");


        if (currentHP <= 0)
            return true;
        else
            return false;
    }
}
