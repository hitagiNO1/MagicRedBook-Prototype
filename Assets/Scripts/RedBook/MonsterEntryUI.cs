using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

[System.Serializable]
public class MonsterUIDisplay
{
    public Sprite monsterImage;
    public Image MosterIcon;
    public Button button;
}

public class MonsterEntryUI : MonoBehaviour
{
    public Image BicImage;               // 怪物圖像
    public Image SkillImage;
    public TextMeshProUGUI nameText;      // 名稱
    public TextMeshProUGUI descriptionText;  // 描述

    public GameObject page1;
    public GameObject page2;
    
    public List<MonsterUIDisplay> monsterUIList;

    public void AddMonster(MonsterData monster)
    {
        MonsterUIDisplay ui = monsterUIList[0];
        switch (monster.name)
        {
            case "Fish":
                ui = monsterUIList[0];
                ui.MosterIcon.sprite = ui.monsterImage;
                ui.button.interactable = true;
                break;

            case "Snake":
                ui = monsterUIList[1];
                ui.MosterIcon.sprite = ui.monsterImage;
                ui.button.interactable = true;
                break;
        }
    }
    public void Setup(MonsterData data)
    {
        BicImage.sprite = data.monsterPic;
        SkillImage.enabled = true;
        SkillImage.sprite = data.monsterskill;
        nameText.text = data.monsterName;
        descriptionText.text = data.description;
    }
    public void Next()
    {
        page1.SetActive(false);
        page2.SetActive(true);
    }
    public void Previous()
    {
        page1.SetActive(true);
        page2.SetActive(false);
    }
}
