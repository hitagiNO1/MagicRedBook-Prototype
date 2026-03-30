using System.Collections.Generic;
using UnityEngine;
// 新增一個類別來儲存單一技能的資訊
[System.Serializable]
public class SkillInfo
{
    [TextArea] public string skillDesc;    // 技能敘述
}
[CreateAssetMenu(fileName = "NewMonster", menuName = "MonsterDex/Monster")]
public class MonsterData : ScriptableObject
{
    public string monsterName;          // 名稱
    [TextArea] public string description;   // 敘述
    public Sprite monsterPic;          // 怪物頭像或圖示
    public Sprite monsterskill;
    // 新增：這隻怪物的技能列表 (在 Unity 面板裡可以自由增減，建議最多加 4 個)
    public List<SkillInfo> skills = new List<SkillInfo>();
}
