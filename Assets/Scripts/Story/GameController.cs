using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public static GameController Instance { get; private set; } //單例
    private StoryScene currentScene;
    public BottomBarController bottomBar;
    public StoryScene FirstScene;
    public bool IsDone = false;     //劇情是否結束
    private State state = State.IDLE;
    public Dictionary<GameScene, bool> ScenePlayedState = new Dictionary<GameScene, bool>(); //為已經播放過的故事存入字典紀錄


    public enum State
    {
        IDLE, ANIMATE, CHOOSE, STORYING, PAUSED
    }
    private void Awake()
    {
        //單例初始化
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // 場景裡如果有多個，砍掉多餘的
            return;
        }
        Instance = this;
        //DontDestroyOnLoad(gameObject); // 切換場景不會摧毀
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
            if (state == State.STORYING)
            {
                if (bottomBar.IsCompleted())
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
                else
                {
                    // 【新增】如果打字還沒完成，按空白鍵/滑鼠直接跳過打字效果
                    bottomBar.SkipTyping();
                }
            }
        }
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

        Debug.Log("現正播故事 : " + currentScene);

        // ==========================================
        // 🌟 新增：在劇情「剛開始」時，立刻通知 BGMManager 準備切換音樂
        // ==========================================
        if (BGMManager.Instance != null && scene != null)
        {
            BGMManager.Instance.NotifyStoryStarted(scene);
        }
    }

    private IEnumerator SwitchScene(StoryScene scene)
    {
        state = State.ANIMATE;
        currentScene = scene;

        yield return new WaitForSeconds(0.5f);

        if (scene is StoryScene)
        {
            bottomBar.ClearText();
            yield return new WaitForSeconds(1f);
            bottomBar.PlayScene(scene);
            state = State.STORYING;
        }
    }

    // 這是給其他腳本使用的公開方法，用來等劇情播放完後再執行某個行為
    public void WaitForStoryEndThen(System.Action callback)
    {
        StartCoroutine(WaitThenCall(callback));
    }

    private IEnumerator WaitThenCall(System.Action callback)
    {
        while (IsDone)
            yield return new WaitForSeconds(2f);

        callback?.Invoke();
    }
}