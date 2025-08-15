using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class mouseborn : MonoBehaviour
{
    public GameObject mouse;
    public GameObject C4;
    public Transform mouseRoot;
    public List<Holenumber> occupiedHoles = new List<Holenumber>(); // 儲存當前占用的位置
    public usercontrol usercontrol; // 引用 MouseClickHandler

    // Start 在第一幀之前調用
    void Start()
    {
        mouseRoot = transform.GetChild(1);
        // 查找 MouseClickHandler 組件
        usercontrol = FindObjectOfType<usercontrol>();
        if (usercontrol == null)
        {
            Debug.LogError("未找到 MouseClickHandler 組件！");
        }
        InvokeRepeating("Create", 0, 1.5f);
        InvokeRepeating("boom", 3f, 3f);
    }

    // Update 每幀調用一次
    void Update()
    {
        // 如果遊戲結束或勝利，取消生成並禁用腳本
        if (usercontrol != null && !usercontrol.IsGameActive())
        {
            CancelInvoke("Create");
            CancelInvoke("boom");
            enabled = false; // 禁用腳本以防止進一步更新
        }
    }

    // 當 GameObject 被停用時記錄日誌
    void OnDisable()
    {
        Debug.Log($"GameCanvas 在 {Time.time} 秒時被停用");
    }

    public void Create()
    {
        // 檢查遊戲是否活躍以及 GameObject 是否在層級中活躍
        if (usercontrol == null || !usercontrol.IsGameActive() || !gameObject.activeInHierarchy)
        {
            return;
        }

        Holenumber newHole = GetUniqueHole(); // 獲取不重複的位置
        GameObject go = Instantiate(mouse);
        go.AddComponent<mouseset>().GetHole = newHole;
        go.GetComponent<mouseset>().GetParent = mouseRoot;
        occupiedHoles.Add(newHole); // 將新位置加入占用列表
        StartCoroutine(RemoveHoleAfterDelay(newHole, 2f)); // 在物件銷毀後移除位置
        Destroy(go, 2f);
    }

    public void boom()
    {
        // 檢查遊戲是否活躍以及 GameObject 是否在層級中活躍
        if (usercontrol == null || !usercontrol.IsGameActive() || !gameObject.activeInHierarchy)
        {
            return;
        }

        Holenumber newHole = GetUniqueHole(); // 獲取不重複的位置
        GameObject go = Instantiate(C4);
        go.AddComponent<mouseset>().GetHole = newHole;
        go.GetComponent<mouseset>().GetParent = mouseRoot;
        occupiedHoles.Add(newHole); // 將新位置加入占用列表
        StartCoroutine(RemoveHoleAfterDelay(newHole, 5f)); // 在物件銷毀後移除位置
        Destroy(go, 5f);
    }

    // 獲取一個不重複的 Holenumber
    public Holenumber GetUniqueHole()
    {
        Holenumber newHole;
        do
        {
            newHole = (Holenumber)Random.Range(0, 9);
        } while (occupiedHoles.Contains(newHole)); // 如果位置已被占用，重新生成
        return newHole;
    }

    // 在物件銷毀後從占用列表中移除位置
    public IEnumerator RemoveHoleAfterDelay(Holenumber hole, float delay)
    {
        yield return new WaitForSeconds(delay);
        occupiedHoles.Remove(hole);
    }
}
