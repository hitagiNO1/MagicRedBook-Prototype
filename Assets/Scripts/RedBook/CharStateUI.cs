using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharStateUI : MonoBehaviour
{
    //public TMP_Text levelText;
    public Slider hpSlider;
    public Unit Unit;

    public void SetHUD()
    {
        //levelText.text = "Lv" + Unit.unitLevel;
        hpSlider.maxValue = Unit.maxHP;
        hpSlider.value = Unit.currentHP;
    }
}
