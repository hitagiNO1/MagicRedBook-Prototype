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
        // 加個監視器，確定按鈕真的有被按到
        Debug.Log($"1. 裝備按鈕被點擊了！目前點擊的是：{(data != null ? data.equipmentName : "未知裝備")}");

        // 基本防呆檢查
        if (data == null)
        {
            Debug.LogError("錯誤：裝備資料 (data) 是空的！");
            return;
        }

        if (uiRef == null)
        {
            Debug.LogError("錯誤：InventoryUI 參照 (uiRef) 是空的！請確認 InventoryUI 生成格子時有沒有把自己傳進來。");
            return;
        }

        // 紀錄目前的狀態，看是要穿還是要脫
        bool willEquip = !isEquipped;
        Debug.Log($"2. 準備呼叫確認面板，動作將會是：{(willEquip ? "穿上" : "脫下")}");

        // ★ 核心修改：不要在這裡直接穿脫了！
        // 把裝備資料、目標玩家、以及要做什麼動作，丟給 InventoryUI 去處理
        uiRef.PromptEquip(data, targetPlayerIndex, willEquip);
    }
}