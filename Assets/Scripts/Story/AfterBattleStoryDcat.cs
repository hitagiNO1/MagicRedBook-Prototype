using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AfterBattleStoryDcat : MonoBehaviour
{
    [Header("需要判定死亡的腳色清單")]
    public List<Unit> playerUnits;

    [Header("死亡時要播放的劇情")]
    public StoryScene storyToPlay;

    public GameObject Dcat;
    public GameObject DcatCAM;
    private bool hasTriggered = false;

    void Update()
    {
        // 如果已經觸發過，就不再執行
        if (hasTriggered) return;

        // 如果清單是空的，防呆保護
        if (playerUnits == null || playerUnits.Count == 0) return;

        // 預設所有人都死了，然後一個一個檢查
        bool allDead = true;
        foreach (var unit in playerUnits)
        {
            // 只要其中有一個 unit 還活著 (HP > 0 且 isDead 為 false)，就代表還沒全滅
            if (unit != null && unit.currentHP > 0 && !unit.isDead)
            {
                allDead = false;
                break; // 只要找到一個活人，就不需要繼續檢查其他人了
            }
        }

        // 如果確認所有指定的腳色都已經死亡
        if (allDead)
        {
            Dcat.SetActive(false);
            DcatCAM.SetActive(false); 
            // 這裡稍微幫你擴充了一下，因為腳色死亡通常發生在戰鬥中 (Battle)
            // 確保遊戲狀態符合你觸發劇情的條件
            if (GameStateManager.Instance.IsState(GameState.Playing) || GameStateManager.Instance.IsState(GameState.Battle))
            {
                GameController.Instance.PlayScene(storyToPlay);
                hasTriggered = true;
            }
        }
    }
}