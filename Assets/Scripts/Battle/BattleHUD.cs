using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // 一定要加這行！

public class BattleHUD : MonoBehaviour
{
    public Slider hpSlider;
    public TextMeshProUGUI nameText; // 新增：名字文字

    public void SetHUD(Unit unit)
    {
        // 設定血條
        hpSlider.maxValue = unit.maxHP;
        hpSlider.value = unit.currentHP;

        // 設定名字
        if (nameText != null)
        {
            nameText.text = unit.unitName;
        }
        else
        {
            Debug.LogWarning("BattleHUD: nameText 未指定！");
        }
    }

    public void SetHP(int hp)
    {
        hpSlider.value = hp;
    }
}