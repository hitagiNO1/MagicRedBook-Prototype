using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class StoryTrigger1 : MonoBehaviour
{
    bool scope = false;
    private bool IfIDLE = false;
    public GameScene currentScene;      //當前的劇情
    public GameController Gamescript;
    public List<GameScene> LimitScenes; //限制哪些故事沒播過就不能播此currentScene
    public float sec = 2f;              //刪除時間

    void Update()
    {
        if (scope && !IfIDLE)
        {
            // 如果 LimitScenes 是空的，允許進入對話場景，判斷如果劇情沒有條件限制則正常撥放
            if (LimitScenes.Count == 0)
            {
                Gamescript.PlayScene(currentScene as StoryScene);

                // ================= 新增在這裡 =================
                EquipmentManager.Instance.AddEquipment("逗貓棒法杖");
                // ==============================================

                IfIDLE = true;
                StartCoroutine(Delet(sec));
            }
            else
            {
                // 遍歷 LimitScenes 列表
                foreach (GameScene scene in LimitScenes)
                {
                    // 檢查是否該場景已經執行過。
                    if (Gamescript.ScenePlayedState.ContainsKey(scene))
                    {
                        Gamescript.PlayScene(currentScene as StoryScene);

                        // ================= 新增在這裡 =================
                        EquipmentManager.Instance.AddEquipment("逗貓棒法杖");
                        // ==============================================

                        IfIDLE = true;
                        StartCoroutine(Delet(sec));
                    }
                }
            }

        }
        if (Gamescript.IsDone == false) IfIDLE = false;//檢查故事是否播完
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