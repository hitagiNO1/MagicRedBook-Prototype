using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EquipmentSlot : MonoBehaviour
{
    [Header("UI 元件綁定")]
    public Image icon;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI Description;
    public Button actionButton;       // 穿脫按鈕
    public TextMeshProUGUI statusText; // 按鈕上的文字 (顯示"穿上"或"脫下")

    // 內部資料參照
    private EquipmentData data;
    private InventoryUI uiRef;
    private int targetPlayerIndex;
    private bool isEquipped;

    // 給 InventoryUI 呼叫的初始化方法 (對標你的 ItemSlot.Setup)
    public void Setup(EquipmentData equipData, InventoryUI ui, int playerIndex, bool currentlyEquipped)
    {
        data = equipData;
        uiRef = ui;
        targetPlayerIndex = playerIndex;
        isEquipped = currentlyEquipped;

        // 1. 更新基本 UI 資訊
        icon.sprite = data.icon;
        nameText.text = data.equipmentName;

        if (Description != null)
        {
            Description.text = data.description;
        }

        // 2. 更新按鈕文字狀態
        if (statusText != null)
        {
            statusText.text = isEquipped ? "脫下" : "穿上";
        }

        // 3. 綁定按鈕事件 (跟你的 ItemSlot 一樣，先清空再綁定)
        actionButton.onClick.RemoveAllListeners();
        actionButton.onClick.AddListener(OnEquipButtonClicked);
    }

    public void OnEquipButtonClicked()
    {
        if (data == null) return;

        SkillManager skillmanager = FindObjectOfType<SkillManager>();
        if (skillmanager == null)
        {
            Debug.LogError("找不到 SkillManager！");
            return;
        }

        // 取得要操作的目標玩家
        Unit targetUnit = skillmanager.playerUnits[targetPlayerIndex];

        // 執行穿脫邏輯
        if (isEquipped)
        {
            // 如果已經穿著，就呼叫脫下
            EquipmentManager.Instance.UnequipItemFromUnit(targetUnit, data.equipmentName);
        }
        else
        {
            // 如果還沒穿著，就呼叫穿上
            EquipmentManager.Instance.EquipItemToUnit(targetUnit, data.equipmentName);
        }

        // 執行完畢後，通知 InventoryUI 重新整理畫面，讓按鈕文字更新
        if (uiRef != null)
        {
            uiRef.RefreshUI();
        }
    }
}