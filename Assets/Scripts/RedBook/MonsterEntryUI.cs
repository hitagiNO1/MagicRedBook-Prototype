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
    public Image buttonimg;
    public GameObject SkillBut;
}

public class MonsterEntryUI : MonoBehaviour
{
    public Image BicImage;
    public Image SkillImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;

    public GameObject page1;
    public List<MonsterUIDisplay> monsterUIList;

    // 1. AddMonster 只負責「解鎖」怪物列表的按鈕
    public void AddMonster(MonsterData monster)
    {
        MonsterUIDisplay ui = monsterUIList[0];

        switch (monster.name)
        {
            case "Fish": ui = monsterUIList[0]; break;
            case "Snake": ui = monsterUIList[1]; break;
            case "Clock": ui = monsterUIList[2]; break;
            case "Soft": ui = monsterUIList[3]; break;
            case "Death": ui = monsterUIList[4]; break;
        }

        ui.MosterIcon.sprite = ui.monsterImage;
        ui.button.interactable = true;
        ui.buttonimg.enabled = true;
    }

    // 2. Setup 負責處理玩家「點選」怪物後的畫面切換
    public void Setup(MonsterData data)
    {
        // 先關閉所有怪物的 SkillBut，確保畫面乾淨
        foreach (var uiItem in monsterUIList)
        {
            if (uiItem.SkillBut != null)
            {
                uiItem.SkillBut.SetActive(false);
            }
        }

        // 找到玩家現在點擊的這隻怪物 UI
        MonsterUIDisplay activeUI = monsterUIList[0];
        switch (data.name)
        {
            case "Fish": activeUI = monsterUIList[0]; break;
            case "Snake": activeUI = monsterUIList[1]; break;
            case "Clock": activeUI = monsterUIList[2]; break;
            case "Soft": activeUI = monsterUIList[3]; break;
            case "Death": activeUI = monsterUIList[4]; break;
        }

        // 在這裡打開「當前這隻」怪物的技能面板，並綁定按鈕
        if (activeUI.SkillBut != null)
        {
            activeUI.SkillBut.SetActive(true);
            BindSkillButtons(activeUI.SkillBut, data);
        }

        // 更新右側的詳細資料
        BicImage.sprite = data.monsterPic;
        SkillImage.enabled = true;
        SkillImage.sprite = data.monsterskill;

        // 【重要】切換怪物時，預設顯示純怪物名稱
        nameText.text = data.monsterName;

        // 預設顯示怪物敘述
        descriptionText.text = data.description;
    }

    // 3. 自動抓取並綁定按鈕的邏輯
    // 3. 自動抓取並綁定按鈕的邏輯
    private void BindSkillButtons(GameObject skillButObj, MonsterData data)
    {
        Button[] buttons = skillButObj.GetComponentsInChildren<Button>(true);

        // 準備好用來替換的中文數字
        string[] skillNumbers = { "一", "二", "三", "四" };

        for (int i = 0; i < buttons.Length; i++)
        {
            if (i < data.skills.Count)
            {
                buttons[i].gameObject.SetActive(true);
                buttons[i].onClick.RemoveAllListeners();

                int index = i;

                buttons[i].onClick.AddListener(() =>
                {
                    //Debug.Log($"點擊了技能: {data.skills[index].skillDesc}");

                    // 1. 更新描述文本
                    descriptionText.text = data.skills[index].skillDesc;

                    // 2. 【核心修改】：根據按鈕的順序 (index)，顯示對應的技能一、二、三
                    // index 是從 0 開始的，所以 index 0 會對應到 skillNumbers[0] 也就是 "一"
                    if (index < skillNumbers.Length)
                    {
                        nameText.text = $"{data.monsterName}一技能{skillNumbers[index]}";
                    }
                });
            }
            else
            {
                buttons[i].gameObject.SetActive(false);
            }
        }
    }
}