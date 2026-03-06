using UnityEngine;

[CreateAssetMenu(fileName = "NewEquipment", menuName = "Item/EquipmentData")]
public class EquipmentData : ScriptableObject
{
    [Header("裝備基本資訊")]
    public string equipmentName; // 必須跟你程式碼裡判斷的名字一樣 (例如: "膠帶拳套")
    [TextArea] public string description;
    public Sprite icon;

    // 之後如果有需要，可以把數值也寫在這邊，不用寫死在 EquipmentManager 裡
    // public int bonusDamage;
    // public int bonusDefense;
}