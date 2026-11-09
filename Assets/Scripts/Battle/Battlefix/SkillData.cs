using UnityEngine;

[CreateAssetMenu(fileName = "New Skill", menuName = "Game/Skill")]
public class SkillData : ScriptableObject
{
    [Header("基本設定")]
    public string skillName = "新技能";
    [TextArea]
    public string description = "技能描述";
    public int cooldownTurns = 0;
    [Header("傷害設定")]
    public float damageMultiplier = 1.0f; // 1.0 代表 100% 普攻傷害，1.5 代表 150%
    public bool isMagic = false;          // 未來可以擴充魔法傷害判定

    [Header("狀態效果 (Buff/Debuff) 附加")]
    public bool applyStatusEffect = false;

    // 如果勾選了 applyStatusEffect，就會讀取以下設定
    public StatType targetStat;           // 要影響哪個屬性 (例如 Attack)
    public ModifierType modifierType;     // 修改方式 (例如 PercentAdd)
    public float effectValue;             // 數值 (例如 0.2 代表 20%)
    public int durationTurns;             // 持續回合數

    // 目標是誰？(自己還是敵人)
    public bool applyToSelf = false;
}