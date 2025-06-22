using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TalkingBox : MonoBehaviour
{
    bool talkbox = false;
    public bool IfIDLE = false;
    public bool HasFrontStory = false;  //是否為條件型劇情
    public GameScene currentScene;      //當前的劇情
    public GameScene FailScene;         //尚未達成條件的劇情
    public GameScene SuccessfulScene;   //達成條件的劇情
    public GameController Gamescript;
    public BottomBarController BottomBarscript;
    public List<GameScene> LimitScenes; //限制哪些故事沒播過就不能播此currentScene
    void Update()
    {
        SelectStory();
    }

    public GameScene SelectStory()
    {
        // 檢查是否按下 F 鍵、進入碰撞框範圍和 IDLE 狀態
        if (Input.GetKeyDown(KeyCode.F) && talkbox && !IfIDLE)
        {
            // 如果 LimitScenes 是空的，允許進入對話場景，判斷如果劇情沒有條件限制則正常撥放
            if (LimitScenes.Count == 0)
            {
                Gamescript.PlayScene(currentScene as StoryScene);
                IfIDLE = true;
                return currentScene;
            }
            if (HasFrontStory == false)     //用於控制是否先播放當前劇情currentScene
            {
                // 遍歷 LimitScenes 列表
                foreach (GameScene scene in LimitScenes)
                {
                    // 檢查是否該場景已經執行過。
                    if (Gamescript.ScenePlayedState.ContainsKey(scene))
                    {
                        if (SuccessfulScene == null)
                        {
                            Gamescript.PlayScene(currentScene as StoryScene);
                        }
                        else
                        {
                            // 如果該場景已經執行過，進入條件成功的場景並標記為 IDLE
                            Gamescript.PlayScene(SuccessfulScene as StoryScene);
                        }
                        IfIDLE = true;
                        return currentScene;
                    }
                    else
                    {
                        Gamescript.PlayScene(FailScene as StoryScene); //如果達成條件則播放FailScene劇情
                    }

                }
            }
            else
            {
                Gamescript.PlayScene(currentScene as StoryScene);       //播放前置劇情
                HasFrontStory = false;      //設為FAIL已進入上面程式驗證
            }
        }
        if (Gamescript.IsDone == false) IfIDLE = false;//檢查故事是否播完
        return currentScene;
    }

    void OnTriggerEnter(Collider other)//檢測角色是否進入對話功能範圍
    {
        if (other.gameObject.tag == "Player")
        {
            talkbox = true;
        }
    }
    void OnTriggerExit(Collider other)//檢測角色是否進入對話功能範圍
    {
        if (other.gameObject.tag == "Player")
        {
            talkbox = false;
        }
    }
}
