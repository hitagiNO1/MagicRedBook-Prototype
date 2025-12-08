using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
 /*   [Header("UI References")]
    public GameObject loadingBarObject; // 讀取條的父物件
    public Image loadingBarFill;        // 讀取條的填充 Image
    public GameObject buttonsObject;    // 按鈕的父物件 (用來隱藏按鈕)*/

    [Header("Scene Names")]
    public string persistentGameplaySceneName = "PersistentGameplay"; // 包含玩家、相機的場景名稱
    public string level1SceneName = "Room1";                          // 第一個關卡的場景名稱

   /* private void Awake()
    {
        // 遊戲開始時隱藏讀取條
        if (loadingBarObject != null)
            loadingBarObject.SetActive(false);
    }*/

    public void StartGame()
    {
       /* // 隱藏主選單按鈕
        if (buttonsObject != null)
            buttonsObject.SetActive(false);

        // 顯示讀取條
        if (loadingBarObject != null)
            loadingBarObject.SetActive(true);*/

        // 開始協程載入場景
        StartCoroutine(LoadGameScenes());
    }

    private IEnumerator LoadGameScenes()
    {
        // 追蹤所有載入操作的清單
        List<AsyncOperation> scenesToLoad = new List<AsyncOperation>();

        // 1. 載入 Persistent Gameplay 場景 (Single 模式，這會自動卸載 Main Menu)
        // 注意：這裡使用 Single 模式是因為我們要移除主選單場景
        scenesToLoad.Add(SceneManager.LoadSceneAsync(persistentGameplaySceneName, LoadSceneMode.Single));

        // 2. 載入第一個關卡 (Additive 模式，這樣它會與 Persistent 場景共存)
        scenesToLoad.Add(SceneManager.LoadSceneAsync(level1SceneName, LoadSceneMode.Additive));

        float progress = 0f;

        // 當還有場景未載入完成時
        // 注意：這裡簡化了進度計算，實際上你需要檢查所有操作是否都完成
        // 為了簡單起見，我們檢查最後一個操作是否完成，或者累加進度
        while (!scenesToLoad[0].isDone || !scenesToLoad[1].isDone)
        {
            float totalProgress = 0f;
            foreach (var operation in scenesToLoad)
            {
                totalProgress += operation.progress;
            }

            // 計算平均進度
            progress = totalProgress / scenesToLoad.Count;

           /* // 更新讀取條 UI
            if (loadingBarFill != null)
                loadingBarFill.fillAmount = progress;*/

            yield return null;
        }

        // 確保讀取條最後是滿的 (可選)
      /*  if (loadingBarFill != null)
            loadingBarFill.fillAmount = 1f;*/
    }
}