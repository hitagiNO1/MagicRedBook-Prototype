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

        // 3. 把管理員(自己)和點位資訊交給生成的物件
        Tree targetScript = newObj.GetComponent<Tree>();
        if (targetScript != null)
        {
            targetScript.Setup(this, selectedPoint, sharedFadeImage);
        }

        // 4. 因為這個點位站人了，把它從空閒清單移除
        availablePoints.RemoveAt(randomIndex);
    }

    // 當物件被點擊（互動）時，物件會呼叫這個方法
    public void RelocateObject(Tree objScript)
    {
        // 1. 釋放這個物件「原本」的點位（加回空閒清單）
        availablePoints.Add(objScript.currentPoint);

        // 2. 隨機找一個「新的」空閒點位
        int randomIndex = Random.Range(0, availablePoints.Count);
        Transform newPoint = availablePoints[randomIndex];

        // 3. 把物件瞬間移動到新點位 (模擬消失又出現在別處)
        objScript.transform.position = newPoint.position;
        objScript.transform.rotation = newPoint.rotation;

        // 4. 更新該物件記錄的目前點位
        objScript.currentPoint = newPoint;

        // 5. 將新點位從空閒清單中移除，避免下一個物件又跑來這
        availablePoints.RemoveAt(randomIndex);
    }
}