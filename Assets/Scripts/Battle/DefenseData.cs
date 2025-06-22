using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DefenseData", menuName = "GameData/DefenseData")]
public class DefenseData : ScriptableObject
{
    [Serializable]
    public class DefenseEntry
    {
        public int defenseValue;  // 防禦力
        public float damageReduction;  // 減傷比例（0~1）
    }

    public List<DefenseEntry> defenseEntries = new List<DefenseEntry>();

    // 查詢對應的減傷
    public float GetDamageReduction(int defense)
    {
        var entry = defenseEntries.Find(e => e.defenseValue == defense);
        return entry != null ? entry.damageReduction : 0f;
    }
}