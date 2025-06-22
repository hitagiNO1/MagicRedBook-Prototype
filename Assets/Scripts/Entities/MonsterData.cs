using UnityEngine;

[CreateAssetMenu(fileName = "NewMonster", menuName = "MonsterDex/Monster")]
public class MonsterData : ScriptableObject
{
    public string monsterName;          // 名稱
    [TextArea] public string description;   // 敘述
    public Sprite monsterPic;          // 怪物頭像或圖示
    public Sprite monsterskill;
}
