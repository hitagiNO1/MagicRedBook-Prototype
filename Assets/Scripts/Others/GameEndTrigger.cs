using UnityEngine;
using UnityEngine.SceneManagement;

public class GameEndTrigger : MonoBehaviour
{
    [Header("設定")]
    public string mainMenuSceneName = "MainMenu"; // 填入你主選單的場景名稱
    public string saveKey = "HasBeatenGame";      // 必須跟主選單的 Key 一模一樣

    // 當遊戲破關、準備回主選單時呼叫這個 Function
    private void Start()
    {
        FinishGameAndReturnToMenu();
    }
    public void FinishGameAndReturnToMenu()
    {
        // 1. 先把破關紀錄寫入硬碟 (設為 1)
        PlayerPrefs.SetInt(saveKey, 1);
        PlayerPrefs.Save(); // 強制儲存，避免意外遺失

        Debug.Log("已記錄破關！主選單開啟");
    }
}