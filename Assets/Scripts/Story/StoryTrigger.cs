using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class StoryTrigger : MonoBehaviour
{
    bool scope = false;
    private bool IfIDLE = false;
    public GameScene currentScene;      // 當前的劇情

    // 把 public 隱藏，因為我們要用程式自動抓，不需要在 Inspector 顯示
    [HideInInspector]
    public GameController Gamescript;

    public List<GameScene> LimitScenes; // 限制哪些故事沒播過就不能播此 currentScene
    public float sec = 2f;              // 刪除時間

    [Header("劇情進場驚嘆號動畫設定")]
    [HideInInspector] // 同樣隱藏，不用手動拖曳
    public Animator introAnimator;

    void Start()
    {
        // 1. 自動尋找 GameController (因為你有寫單例 Instance，直接抓就好！)
        Gamescript = GameController.Instance;

        // 2. 自動尋找驚嘆號物件並取得 Animator 
        GameObject introObj = GameObject.Find("TriggerStory");
        if (introObj != null)
        {
            introAnimator = introObj.GetComponent<Animator>();
        }
        else
        {
            Debug.LogWarning("找不到驚嘆號物件，請確認 GameObject.Find 裡的名稱是否正確。");
        }
    }

    void Update()
    {
        if (scope && !IfIDLE)
        {
            // 判斷是否可以播放劇情
            bool canPlay = false;

            // 如果沒有限制，直接允許播放
            if (LimitScenes.Count == 0)
            {
                canPlay = true;
            }
            else
            {
                // 如果有限制，檢查是否「所有」限制的劇情都已經播過了
                canPlay = true; // 先假設都可以
                foreach (GameScene scene in LimitScenes)
                {
                    // 只要有一個沒播過 (字典裡沒有，或者值是 false)，就不允許播放
                    if (!Gamescript.ScenePlayedState.ContainsKey(scene) || !Gamescript.ScenePlayedState[scene])
                    {
                        canPlay = false;
                        break; // 只要有一個沒達成，就可以跳出迴圈了
                    }
                }
            }

            // 如果條件達成，執行播放動畫與劇情
            if (canPlay)
            {
                if (introAnimator != null)
                {
                    introAnimator.SetTrigger("PlayIntro");
                }

                Gamescript.PlayScene(currentScene as StoryScene);
                IfIDLE = true;
                StartCoroutine(Delet(sec));
            }
        }

        // 檢查故事是否播完
        if (Gamescript != null && Gamescript.IsDone == false)
        {
            IfIDLE = false;
        }
    }

    private IEnumerator Delet(float sec)
    {
        yield return new WaitForSeconds(sec);
        Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other) // 檢測角色是否進入對話功能範圍
    {
        if (other.gameObject.CompareTag("Player")) // 建議用 CompareTag 效能比較好
        {
            scope = true;
        }
    }

    void OnTriggerExit(Collider other) // 檢測角色是否離開對話功能範圍
    {
        if (other.gameObject.CompareTag("Player"))
        {
            scope = false;
        }
    }
}