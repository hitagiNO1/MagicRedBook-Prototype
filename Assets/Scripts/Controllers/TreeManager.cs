using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TreeManager : MonoBehaviour
{
    [Header("設定區")]
    [Tooltip("請把 8 個固定點位的 Transform 拖曳到這裡")]
    public Transform[] spawnPoints;

    [Tooltip("要出現的物件 Prefab")]
    public GameObject targetPrefab;

    [Tooltip("同時出現在場上的數量")]
    public int activeCount = 3;

    [Header("場景 UI 參照")]
    public Image sharedFadeImage;          // 在場景的 Manager 裡把黑幕 Image 拖進來
    // 用來記錄「目前沒有站人」的空閒點位
    private List<Transform> availablePoints = new List<Transform>();

    void Start()
    {
        // 防呆檢查
        if (spawnPoints.Length < activeCount)
        {
            Debug.LogError("點位數量必須大於或等於同時出現的物件數量！");
            return;
        }

        // 遊戲開始時，把所有點位都加入「空閒清單」
        availablePoints.AddRange(spawnPoints);

        // 初始生成 3 個物件
        for (int i = 0; i < activeCount; i++)
        {
            CreateAndPlaceObject();
        }
    }

    void CreateAndPlaceObject()
    {
        // 1. 從空閒清單中「隨機」挑一個點位
        int randomIndex = Random.Range(0, availablePoints.Count);
        Transform selectedPoint = availablePoints[randomIndex];

        // 2. 生成物件
        GameObject newObj = Instantiate(targetPrefab, selectedPoint.position, selectedPoint.rotation);

        // 3. ★ 關鍵修改 1：使用 GetComponentInChildren 去子物件層級抓取腳本
        Tree targetScript = newObj.GetComponentInChildren<Tree>();
        if (targetScript != null)
        {
            targetScript.Setup(this, selectedPoint, sharedFadeImage);
        }
        else
        {
            Debug.LogError("在 Prefab 及其子物件中都找不到 Tree 腳本！");
        }

        // 4. 因為這個點位站人了，把它從空閒清單移除
        availablePoints.RemoveAt(randomIndex);
    }

    // 當物件被點擊（互動）時，物件會呼叫這個方法
    // 當物件被點擊（互動）時，物件會呼叫這個方法
    public void RelocateObject(Tree objScript)
    {
        // 防呆：確保空閒清單裡還有位置可以去
        if (availablePoints.Count == 0)
        {
            Debug.LogWarning("沒有多餘的空閒點位可以移動了！");
            return;
        }

        // 1. 先從「目前的」空閒清單隨機找一個新點位 
        int randomIndex = Random.Range(0, availablePoints.Count);
        Transform newPoint = availablePoints[randomIndex];

        // 2. 將新抽到的點位從空閒清單中移除
        availablePoints.RemoveAt(randomIndex);

        // 3. 把這個物件「原本」的點位加回空閒清單
        availablePoints.Add(objScript.currentPoint);

        // 4. ★ 關鍵修改 2：抓取父物件 (整個 Prefab) 進行移動
        // 如果這個物件確實有父物件，就移動父物件；如果沒有，就移動自己
        Transform targetToMove = objScript.transform.parent != null ? objScript.transform.parent : objScript.transform;

        targetToMove.position = newPoint.position;
        targetToMove.rotation = newPoint.rotation;

        // 5. 更新該物件記錄的目前點位
        objScript.currentPoint = newPoint;
    }
}