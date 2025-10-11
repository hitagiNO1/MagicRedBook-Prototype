using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class StoryTriggerE : MonoBehaviour
{
    public GameObject InteractiveUI;         // 提示 按E進入
    bool scope = false;
    private bool IfIDLE = false;
    public GameScene currentScene;      //當前的劇情
    public GameController Gamescript;
    public List<GameScene> LimitScenes; //限制哪些故事沒播過就不能播此currentScene
    public float sec = 2f;              //刪除時間
    void Update()
    {
        if (scope && Input.GetKeyDown(KeyCode.E) && !IfIDLE)
        {
            // 如果 LimitScenes 是空的，允許進入對話場景，判斷如果劇情沒有條件限制則正常撥放
            if (LimitScenes.Count == 0)
            {
                InteractiveUI.SetActive(false);
                Gamescript.PlayScene(currentScene as StoryScene);
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
                        InteractiveUI.SetActive(false);
                        Gamescript.PlayScene(currentScene as StoryScene);
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
            InteractiveUI.SetActive(true);
            scope = true;
        }
    }
    void OnTriggerExit(Collider other)//檢測角色是否進入對話功能範圍
    {
        if (other.gameObject.tag == "Player")
        {
            InteractiveUI.SetActive(false);
            scope = false;
        }
    }
}
