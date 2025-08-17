using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class Enemyskill : MonoBehaviour
{
    public Unit playerUnit;
    public Unit enemyUnit;
    public TextMeshProUGUI dialogueText;
    public BattleHUD playerHUD;
    public BattleSystem battlesystem;
    public BuffDebuffManager buffDebuffManager;
    // Start is called before the first frame update


    public void Fishskill_1()
    {
        int buffedDamage = Mathf.RoundToInt(enemyUnit.damage * 1.2f);
        bool isDead = playerUnit.TakeDamage(buffedDamage);
        playerHUD.SetHP(playerUnit.currentHP);
        dialogueText.text = "³½¤e¬ð¨ë¡I";
        EffectManager.Instance.PlayUIEffect("test1", new Vector2(-751, -279), 1f, 1f);
    }

    public void Fishskill_2()
    {
        buffDebuffManager.ApplyDefenseDebuff(playerUnit,0.7f, 2);
        int buffedDamage = Mathf.RoundToInt(enemyUnit.damage * 1.1f);
        bool isDead = playerUnit.TakeDamage(buffedDamage);
        playerHUD.SetHP(playerUnit.currentHP);
        dialogueText.text = "¯«³½Â\§À¡I";
        EffectManager.Instance.PlayUIEffect("test2", new Vector2(-654, -61), 1f, 1f);
    }
}
