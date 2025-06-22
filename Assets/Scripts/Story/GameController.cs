using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    private StoryScene currentScene;
    public BottomBarController bottomBar;
    public StoryScene FirstScene;
    public bool IsDone = false;     //劇情是否結束

    GameObject[] TalkBox;
    private State state = State.IDLE;
    public Dictionary<GameScene, bool> ScenePlayedState = new Dictionary<GameScene, bool>(); //為已經播放過的故事存入字典紀錄


    public enum State
    {
        IDLE, ANIMATE, CHOOSE, STORYING, PAUSED
    }

    void Start()
    {
        if (currentScene is StoryScene && state == State.STORYING)
        {
            bottomBar.PlayScene(currentScene);
        }
        PlayScene(FirstScene);
    }

    void Update()
    {
        if ((TimelineManager.Instance?.IsPlaying ?? false))
            return; // Timeline 播放時不處理空白鍵與滑鼠輸入
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            if (state == State.STORYING && bottomBar.IsCompleted()) 
            {
                
                
                if (bottomBar.IsLastSentence())
                {
                    state = State.IDLE;
                    GameStateManager.Instance.SetState(GameState.Playing);
                    bottomBar.Hide();
                    IsDone = false;
                    //故事播完會存入ScenePlayedState標示為true
                    if (!ScenePlayedState.ContainsKey(currentScene))
                    {
                        ScenePlayedState.Add(currentScene, true);
                    }
                }
                else
                {
                    bottomBar.PlayNextSentence();
                }
            }
        }
        /*檢查所有ScenePlayedState哪些已經播放
        foreach (var kvp in ScenePlayedState)
        {
            Debug.Log("Scene : " + kvp.Key + ", 已播放 : " + kvp.Value);
        }
        */
    }

    public void PlayScene(StoryScene scene)
    {
        StartCoroutine(SwitchScene(scene));
        IsDone = true;
        if (scene.StopMove)
        {
            GameStateManager.Instance.SetState(GameState.StoryNotMove);
        }
        else GameStateManager.Instance.SetState(GameState.Story);

        Debug.Log("現正播故事 : "+currentScene);

    }

    private IEnumerator SwitchScene(StoryScene scene)          
    {
        state = State.ANIMATE;
        currentScene = scene;
        yield return new WaitForSeconds(1f);
        if (scene is StoryScene)
        {
            bottomBar.ClearText();
            yield return new WaitForSeconds(1f);
            bottomBar.Show();
            bottomBar.PlayScene(scene);
            state = State.STORYING;
        }
    }

    // 這是給其他腳本使用的公開方法，用來等劇情播放完後再執行某個行為
    public void WaitForStoryEndThen(System.Action callback)
    {
        // 啟動協程，並把要執行的行為當作參數傳進去
        StartCoroutine(WaitThenCall(callback));
    }

    // 這是一個協程，會持續等待直到劇情播放完（IsDone 為 true）
    // 然後才執行 callback 裡的行為
    private IEnumerator WaitThenCall(System.Action callback)
    {
        // 當劇情還沒結束，就一直等待（每幀檢查一次）
        while (IsDone)
            yield return new WaitForSeconds(2f);  

        // 劇情播完後，執行你傳進來的動作（如果有設定的話）
        callback?.Invoke();
    }

}
