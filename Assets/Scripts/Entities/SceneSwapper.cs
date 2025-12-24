using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneSwapper
{
    private static HashSet<int> activeObjectIDs = new HashSet<int>();

    // ★ 新增：專門用來記住那個 "Trigger" 物件
    private static GameObject specialRestoreObject;

    public static void RecordAndHideMainScene(GameObject excludeObject = null)
    {
        Scene currentScene = SceneManager.GetActiveScene();
        activeObjectIDs.Clear();
        specialRestoreObject = null; // 清空舊的

        // ★ 如果有傳入例外物件 (Trigger)，就把它存起來，等等還原時一定要打開它
        if (excludeObject != null)
        {
            specialRestoreObject = excludeObject;
        }

        GameObject[] rootObjects = currentScene.GetRootGameObjects();

        foreach (GameObject obj in rootObjects)
        {
            if (obj.activeSelf)
            {
                // 如果這個 Root 物件剛好就是 Trigger (或包含 Trigger)，我們先不關它
                // 但為了保險，這裡的邏輯主要處理 Root，
                // 你的 Trigger 如果是子物件，它的 Root 父物件會在這裡被關閉。

                // 為了避免重複，如果 obj 剛好就是 excludeObject，跳過
                if (excludeObject != null && obj == excludeObject)
                {
                    continue;
                }

                activeObjectIDs.Add(obj.GetInstanceID());
                obj.SetActive(false);
            }
        }
    }

    public static void RestoreMainScene(string sceneName)
    {
        Scene mainScene = SceneManager.GetSceneByName(sceneName);

        if (mainScene.IsValid() && mainScene.isLoaded)
        {
            // 1. 先把所有根物件 (父物件) 還原
            // 這樣才能確保子物件有機會顯示
            GameObject[] rootObjects = mainScene.GetRootGameObjects();
            foreach (GameObject obj in rootObjects)
            {
                if (activeObjectIDs.Contains(obj.GetInstanceID()))
                {
                    obj.SetActive(true);
                }
            }

            // 2. ★ 最後強制打開 Trigger 物件
            // 不管它是不是子物件，也不管它剛剛是不是被手動關掉了
            if (specialRestoreObject != null)
            {
                specialRestoreObject.SetActive(true);
                specialRestoreObject = null; // 用完丟掉
            }

            SceneManager.SetActiveScene(mainScene);
        }
    }
}