using System.Collections.Generic;
using UnityEngine;

public class MonsterDexManager : MonoBehaviour
{
    public List<MonsterData> allMonsters;          // 所有可被遇到的怪物（可省略）
    public List<MonsterData> discoveredMonsters;   // 玩家已遇到的怪物（資料庫）
    public MonsterEntryUI entryUI;

    public void DiscoverMonster(MonsterData newMonster)
    {
        if (!discoveredMonsters.Contains(newMonster))
        {
            discoveredMonsters.Add(newMonster);     // 加入清單
            entryUI.AddMonster(newMonster);             // 新增到 UI 畫面
            Debug.Log("新怪物加入紅書");
        }
    }
    /*
    public void RefreshDexUI()
    {
        foreach (Transform child in dexListParent)
        {
            Destroy(child.gameObject); // 先清空舊內容
        }

        foreach (MonsterData monster in discoveredMonsters)
        {
            entryUI.AddMonster(monster);
        }
    }
    */
}
