using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EquipmentSlotUI : MonoBehaviour
{
    [Header("UI 元件綁定")]
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI statusText; // 用來顯示 "穿上" 或 "脫下"
    public Button actionButton;

    private EquipmentData currentData;
    private int targetPlayerIndex;

    // 給外部 (紅書系統) 呼叫來初始化這個格子的方法
    public void SetupSlot(EquipmentData data, int playerIndex, bool isEquipped)
    {
        currentData = data;
        targetPlayerIndex = playerIndex;

        // 更新 UI 顯示
        nameText.text = data.equipmentName;
        iconImage.sprite = data.icon;

        // 根據是否已裝備，改變按鈕文字或顏色
        statusText.text = isEquipped ? "脫下" : "穿上";

        // 清除舊的事件，避免重複觸發
        actionButton.onClick.RemoveAllListeners();
        actionButton.onClick.AddListener(OnEquipButtonClicked);
    }

    private void OnEquipButtonClicked()
    {
        SkillManager skillManager = FindObjectOfType<SkillManager>();
        if (skillManager == null || currentData == null) return;

        Unit targetUnit = skillManager.playerUnits[targetPlayerIndex];

        // 沿用你原本的穿脫邏輯
        if (targetUnit.equippedGear.Contains(currentData.equipmentName))
        {
            EquipmentManager.Instance.UnequipItemFromUnit(targetUnit, currentData.equipmentName);
        }
        else
        {
            EquipmentManager.Instance.EquipItemToUnit(targetUnit, currentData.equipmentName);
        }

        // 動作執行完後，通知紅書介面重新整理，更新按鈕狀態
        InventoryUI inventoryUI = FindObjectOfType<InventoryUI>();
        if (inventoryUI != null)
        {
            inventoryUI.RefreshUI();
        }
    }
}