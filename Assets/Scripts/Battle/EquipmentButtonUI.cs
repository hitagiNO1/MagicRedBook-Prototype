using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class EquipButtonUI : MonoBehaviour
{
    [Header("裝備名稱 (例: 膠帶拳套)")]
    public string equipmentName;

    [Header("目標玩家 (0=布萊克, 1=萊菈)")]
    public int targetPlayerIndex = 0;

    private Button btn;

    private void Awake()
    {
        btn = GetComponent<Button>();
        btn.onClick.AddListener(OnEquipButtonClicked);
    }

    public void OnEquipButtonClicked()
    {
        SkillManager skillManager = FindObjectOfType<SkillManager>();
        if (skillManager == null) return;

        if (targetPlayerIndex < 0 || targetPlayerIndex >= skillManager.playerUnits.Count) return;

        Unit targetUnit = skillManager.playerUnits[targetPlayerIndex];

        // 判斷邏輯改成：檢查清單內有沒有這件裝備
        if (targetUnit.equippedGear.Contains(equipmentName))
        {
            EquipmentManager.Instance.UnequipItemFromUnit(targetUnit, equipmentName); // 脫下
        }
        else
        {
            EquipmentManager.Instance.EquipItemToUnit(targetUnit, equipmentName); // 穿上
        }
    }
}