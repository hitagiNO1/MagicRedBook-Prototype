using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RescueStoryManager : MonoBehaviour
{
    [Header("核心綁定")]
    public BattleSystem battleSystem;
    public Unit targetPlayer; // 需要被監聽血量的特定玩家角色

    [Header("觸發條件")]
    public int hpThreshold = 10;
    public int hpafter = 50;
    private bool hasTriggered = false;

    [Header("劇情與新角色設定")]
    public StoryScene storyToPlay;       // 劇情名稱
    public GameObject newAllyPrefab;     // 要加入的友方物件 (Hierarchy中的物件)

    // 將原本單一的 GameObject 改成 List，方便放入多個 UI 元素
    public List<GameObject> newAllyUIElements;

    void Update()
    {
        // 持續監聽：如果劇情還沒觸發、戰鬥系統存在，且處於戰鬥中
        if (!hasTriggered && battleSystem != null && battleSystem.state != BattleState.END)
        {
            // 檢查指定角色的血量是否低於閾值
            if (targetPlayer != null && targetPlayer.currentHP < hpThreshold)
            {
                ExecuteRescue();
            }
        }
    }

    private void ExecuteRescue()
    {
        hasTriggered = true;
        Debug.Log("RescueStoryManager：檢測到血量過低！強行介入戰鬥並觸發劇情。");

        // 1. 強制鎖血不死（把血量強行拉回閾值，並解除可能的死亡狀態）
        targetPlayer.currentHP = hpafter;
        targetPlayer.isDead = false;

        // 2. 強制中斷 BattleSystem 正在執行的所有回合與動作 (停止 EnemyTurn 等協程)
        battleSystem.StopAllCoroutines();

        // (保險機制) 如果敵人身上的攻擊協程還在跑，也把它們停掉，避免退出戰鬥瞬間還被打
        if (battleSystem.enemyPrefabs != null)
        {
            foreach (GameObject enemyGO in battleSystem.enemyPrefabs)
            {
                if (enemyGO != null)
                {
                    enemyGO.GetComponent<MonoBehaviour>().StopAllCoroutines();
                }
            }
        }

        // 3. 強制結束戰鬥 
        battleSystem.state = BattleState.RUN;
        battleSystem.EndBattle();

        // 呼叫協程來處理延遲與後續動作
        StartCoroutine(RescueSequenceRoutine());
    }

    // 負責處理延遲與後續機制的協程
    private IEnumerator RescueSequenceRoutine()
    {
        // ★ 延遲 1 秒
        yield return new WaitForSeconds(1f);

        // 4. 新增友方角色到 BattleSystem
        if (newAllyPrefab != null)
        {
            // 必須加入 playerPrefabs，否則你下一次呼叫 SetupBattle 時他會不見
            if (!battleSystem.playerPrefabs.Contains(newAllyPrefab))
            {
                battleSystem.playerPrefabs.Add(newAllyPrefab);
            }

            // 把在 Hierarchy 的物件丟進 playerUnits 中
            Unit newUnit = newAllyPrefab.GetComponent<Unit>();
            if (newUnit != null && !battleSystem.playerUnits.Contains(newUnit))
            {
                battleSystem.playerUnits.Add(newUnit);
            }
        }
        else
        {
            Debug.LogWarning("未設定 newAllyPrefab，無法增加友方角色！");
        }

        // 5. 迴圈開啟所有的 UI 元素
        if (newAllyUIElements != null && newAllyUIElements.Count > 0)
        {
            foreach (GameObject uiElement in newAllyUIElements)
            {
                if (uiElement != null)
                {
                    uiElement.SetActive(true);
                }
            }
        }

        // 6. 呼叫並播放劇情
        if (GameController.Instance != null)
        {
            GameController.Instance.PlayScene(storyToPlay);
        }
        else
        {
            Debug.LogError("呼叫劇情失敗：找不到 GameController.Instance！");
        }
    }
}