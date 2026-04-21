using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    [Header("===== 道具 (Items) 設定 =====")]
    public Transform contentPanel; // 道具的 UI 清單容器
    public GameObject itemSlotPrefab; // 道具的 UI 項目 prefab
    public GameObject confirmPanel; // 是否使用道具的提示面板
    public TextMeshProUGUI confirmText;

    [Header("===== 裝備 (Equipment) 設定 =====")]
    public Transform equipmentContentPanel;
    public GameObject equipmentSlotPrefab;
    public List<EquipmentData> equipmentDatabase;
    public int currentPlayerIndex = 0;


    private EquipmentData selectedEquipment;
    private bool isEquippingAction;
    private int selectedPlayerIndex;

    public void RefreshUI()
    {
        // 1. 清空舊的道具欄位
        foreach (Transform child in contentPanel)
        {
            Destroy(child.gameObject);
        }

        // 2. 如果裝備有獨立的 Panel，也把它清空 (避免重複刪除)
        if (equipmentContentPanel != null && equipmentContentPanel != contentPanel)
        {
            foreach (Transform child in equipmentContentPanel)
            {
                Destroy(child.gameObject);
            }
        }

        // 3. 產生新的「道具」 UI 欄位
        if (InventoryManager.Instance != null && InventoryManager.Instance.items != null)
        {
            foreach (ItemData item in InventoryManager.Instance.items)
            {
                if (item.amount >= 1)
                {
                    GameObject slot = Instantiate(itemSlotPrefab, contentPanel);
                    slot.GetComponent<ItemSlot>().Setup(item, this);
                }
            }
        }

        // 4. 產生新的「裝備」 UI 欄位
        if (EquipmentManager.Instance != null && equipmentDatabase != null)
        {
            SkillManager skillManager = FindObjectOfType<SkillManager>();
            if (skillManager != null && skillManager.playerUnits != null && skillManager.playerUnits.Count > currentPlayerIndex)
            {
                Unit currentUnit = skillManager.playerUnits[currentPlayerIndex];

                // 決定裝備要生成在哪裡 (如果沒設定裝備專屬 Panel，就放在道具一起)
                Transform targetPanel = equipmentContentPanel != null ? equipmentContentPanel : contentPanel;

                // 遍歷玩家目前擁有的裝備字串清單
                foreach (string gearName in EquipmentManager.Instance.ownedEquipment)
                {
                    // 從資料庫找出對應的 ScriptableObject 資料
                    EquipmentData gearData = equipmentDatabase.Find(x => x.equipmentName == gearName);

                    if (gearData != null)
                    {
                        // 產生裝備 Prefab
                        GameObject eqSlot = Instantiate(equipmentSlotPrefab, targetPanel);
                        EquipmentSlot slotUI = eqSlot.GetComponent<EquipmentSlot>(); // 抓取我們剛剛寫的 EquipmentSlot

                        // 判斷當前角色是否已經穿著這件裝備
                        bool isEquipped = currentUnit.equippedGear.Contains(gearName);

                        // 初始化裝備格子，傳入 (資料, UI腳本參照, 玩家索引, 是否已裝備)
                        if (slotUI != null)
                        {
                            slotUI.Setup(gearData, this, currentPlayerIndex, isEquipped);
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"【裝備系統】在 equipmentDatabase 中找不到名為 '{gearName}' 的資料！請檢查是否有把該裝備拖入清單。");
                    }
                }
            }
        }
    }

    // ==================== 道具使用邏輯 (保留原本的) ====================

    // 顯示是否使用道具的確認面板
    public void UseItemDirectly(ItemData item)
    {
        if (item != null && item.amount > 0)
        {
            Debug.Log($"立刻使用了 {item.itemName}，效果：{item.description}");
            item.amount -= 1;

            // ★ 這裡可以呼叫你 Itemuse.cs 裡面的具體邏輯
            // 例如：如果是補血藥水就立刻加血等

            RefreshUI(); // 扣除數量後直接重整畫面
        }
    }
    public void PromptEquip(EquipmentData equipData, int playerIndex, bool isEquipping)
    {
        selectedEquipment = equipData;
        isEquippingAction = isEquipping;
        selectedPlayerIndex = playerIndex;

        string actionText = isEquipping ? "穿上" : "脫下";
        SkillManager skillManager = FindObjectOfType<SkillManager>();
        string playerName = skillManager != null ? skillManager.playerUnits[playerIndex].unitName : "玩家";

        confirmText.text = $"要讓 <b>{playerName}</b> {actionText} <b>{equipData.equipmentName}</b> 嗎？";
        confirmPanel.SetActive(true);
    }
    // 點擊使用道具
    public void ConfirmUse()
    {
        if (selectedEquipment != null)
        {
            SkillManager skillmanager = FindObjectOfType<SkillManager>();
            if (skillmanager != null)
            {
                Unit targetUnit = skillmanager.playerUnits[selectedPlayerIndex];
                if (isEquippingAction)
                {
                    EquipmentManager.Instance.EquipItemToUnit(targetUnit, selectedEquipment.equipmentName);
                }
                else
                {
                    EquipmentManager.Instance.UnequipItemFromUnit(targetUnit, selectedEquipment.equipmentName);
                }
            }
        }

        // 隱藏面板並重新整理
        selectedEquipment = null;
        confirmPanel.SetActive(false);
        RefreshUI();
    }

    // 取消使用
    public void CancelUse()
    {
        selectedEquipment = null;
        confirmPanel.SetActive(false);
    }
    // ==================== 裝備切換邏輯 ====================

    // 如果紅書介面上有按鈕可以切換布萊克(0)或萊菈(1)，把按鈕的 OnClick 綁定這個方法
    public void SwitchPlayerEquipmentView(int playerIndex)
    {
        currentPlayerIndex = playerIndex;
        RefreshUI(); // 切換玩家後，馬上刷新畫面來顯示該玩家的裝備狀態
    }
}