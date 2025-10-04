using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class StoryTrigger2 : MonoBehaviour
{
    bool scope = false;
    private bool isTriggered = false;
    public float sec = 2f;              //刪除時間

    [Header("條件：這些劇情都要播完")]
    public List<GameScene> requiredScenes;

    [Header("條件 未完成時 劇情")]
    public StoryScene sceneA;

    [Header("條件 完成時 劇情")]
    public StoryScene sceneB;

    void Update()
    {
        if (scope && !isTriggered)
        {
            if (AllScenesPlayed())
            {
                // 所有條件劇情播完 → 播放 B
                GameController.Instance.PlayScene(sceneB);
                StartCoroutine(Delet(sec));
            }
            else
            {
                // 尚未全部播完 → 播放 A
                GameController.Instance.PlayScene(sceneA);
            }
            isTriggered = true; // 確保只執行一次
        }

    }

    /// <summary>
    /// 檢查所有指定劇情是否都播完
    /// </summary>
    private bool AllScenesPlayed()
    {
        foreach (GameScene scene in requiredScenes)
        {
            bool played;
            // 沒有紀錄，或紀錄為 false → 表示沒播完
            if (!GameController.Instance.ScenePlayedState.TryGetValue(scene, out played) || !played)
            {
                return false;
            }
        }
        return true;
    }
    private IEnumerator Delet(float sec)
    {
        yield return new WaitForSeconds(sec);
        Destroy(gameObject);
    }

        void OnTriggerEnter(Collider other)//檢測角色是否進入對話功能範圍
    {
        if (other.gameObject.tag == "Player")
        {
            scope = true;
        }
    }
    void OnTriggerExit(Collider other)//檢測角色是否進入對話功能範圍
    {
        if (other.gameObject.tag == "Player")
        {
            scope = false;
        }
    }
}
