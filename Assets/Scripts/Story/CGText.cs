using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Playables; // 引用 Timeline 命名空間
using UnityEngine.SceneManagement;
using static StoryScene;

public class CGText : MonoBehaviour
{
    [Header("UI Components")]
    public TextMeshProUGUI barText;
    public TextMeshProUGUI personNameText;
    public CGSprite characterDisplay;

    [Header("Timeline Settings")]
    public PlayableDirector director; // 請將掛在同一物件上的 PlayableDirector 拉進來
    public PlayableAsset FadeInTimeline;   // 淡入 (從黑變亮)
    public PlayableAsset TCG;              // 中間的背景動畫
    public PlayableAsset FadeOutTimeline;  // 淡出 (從亮變黑)

    [Header("Data")]
    public StoryScene currentScene;
    public string mainSceneName;
    private int sentenceIndex = -1;
    private Animator animator;
    private StoryScene.Sentence currentSentence;

    private enum State { PLAYING, COMPLETED }
    private State state = State.COMPLETED;

    private void Start()
    {
        animator = GetComponent<Animator>();

        // 防呆：如果忘記拉 Director，嘗試自動抓取
        if (director == null)
            director = GetComponent<PlayableDirector>();

        StartCoroutine(PlaySceneSequence());
    }

    private void Update()
    {
        // 修正：只有在場景為空時才不執行，移除 state == COMPLETED 的限制
        if (currentScene == null) return;

        // 玩家點擊：下一句或跳過
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            // 情況 A：字已經打完 (COMPLETED) -> 播放下一句
            if (IsCompleted())
            {
                if (!IsLastSentence())
                {
                    PlayNextSentence();
                }
                else
                {
                    // 對話全部講完，進入結束流程
                    StartCoroutine(EndSceneSequence());
                }
            }
            // 情況 B：字還在打 (PLAYING) -> (如果你想做「瞬間顯示全文字」的功能可寫在這)
            // else 
            // {
            //     StopAllCoroutines();
            //     barText.text = currentSentence.text;
            //     state = State.COMPLETED;
            // }
        }
    }

    // --- [核心] 1. 開場流程 ---
    private IEnumerator PlaySceneSequence()
    {
        // A. 播放淡入動畫 (如果有)
        if (FadeInTimeline != null)
        {
            director.playableAsset = FadeInTimeline;
            director.Play();

            // 關鍵：這行會讓程式「卡住」，直到淡入動畫播完才往下執行
            yield return new WaitUntil(() => director.state != PlayState.Playing);
        }

        // B. 接著播放 TCG (背景)
        if (TCG != null)
        {
            director.playableAsset = TCG;
            // 設定為循環播放 (這樣對話時背景會一直動)
            director.extrapolationMode = DirectorWrapMode.Loop;
            director.Play();
        }

        // C. 顯示對話框 UI
        animator.SetTrigger("Show");
        yield return new WaitForSeconds(0.5f); // 等 UI 浮現

        // D. 開始第一句對話
        sentenceIndex = -1;
        PlayNextSentence();
    }

    // --- [核心] 2. 結束流程 ---
    private IEnumerator EndSceneSequence()
    {
        // 鎖定狀態，避免重複點擊
        state = State.PLAYING;

        // A. 隱藏對話框
        animator.SetTrigger("Hide");
        ClearText();
        yield return new WaitForSeconds(0.5f); // 等 UI 縮回去

        Debug.Log("準備播放淡出...");

        // B. 播放淡出動畫 (如果有)
        if (FadeOutTimeline != null)
        {
            director.playableAsset = FadeOutTimeline;
            // 淡出通常不需要循環，設為 Hold (停在最後一幀全黑)
            director.extrapolationMode = DirectorWrapMode.Hold;
            director.Play();

            // 關鍵：等待淡出播完
            yield return new WaitUntil(() => director.state != PlayState.Playing);
        }

        Debug.Log("場景結束 (全黑)。");
        StartCoroutine(ReturnToMainScene());
    }

    private IEnumerator ReturnToMainScene()
    {
        // 1. 找到原本的主場景
        Scene mainScene = SceneManager.GetSceneByName(mainSceneName);

        if (mainScene.IsValid() && mainScene.isLoaded)
        {
            // 2. 把主場景的所有物件重新打開 (Resume)
            GameObject[] rootObjects = mainScene.GetRootGameObjects();
            foreach (GameObject obj in rootObjects)
            {
                obj.SetActive(true);
            }

            // 設定回主場景為 Active
            SceneManager.SetActiveScene(mainScene);
        }
        else
        {
            Debug.LogError("找不到主場景，可能已被卸載或是名稱錯誤！");
        }

        // 3. 卸載目前的小遊戲場景
        // 注意：這裡使用 gameObject.scene 可以自動抓取腳本所在的場景
        yield return SceneManager.UnloadSceneAsync(gameObject.scene);
    }
    // --- 以下為原本的對話邏輯 (無大幅更動) ---

    public void PlayNextSentence()
    {
        StartCoroutine(PlaySentenceCoroutine());
    }

    private IEnumerator PlaySentenceCoroutine()
    {
        if (currentScene == null || currentScene.sentences.Count == 0) yield break;

        sentenceIndex++;
        currentSentence = currentScene.sentences[sentenceIndex];

        // 處理 Action
        if (currentSentence.actions != null)
        {
            foreach (var action in currentSentence.actions)
                HandleAction(action);
        }

        // BeforeText Timeline (注意：這會暫時切換掉背景 TCG，如果是短特效建議用另一個 Director，若是單純切鏡頭則沒差)
        if (currentSentence.timline != null)
        {
            foreach (var timeline in currentSentence.timline)
            {
                if (timeline.timing == StoryScene.Sentence.TimeLine.TriggerTiming.BeforeText)
                {
                    director.Play(timeline.myPlayableAsset);
                    yield return new WaitUntil(() => director.state != PlayState.Playing);

                    // 特效播完後，如果要恢復背景 TCG，需要重新 Play TCG
                    // if (TCG != null) director.Play(TCG); 
                }
            }
        }

        // 顯示名字與顏色
        personNameText.text = currentSentence.speaker.speakerName;
        personNameText.color = currentSentence.speaker.textColor;

        // 打字
        yield return TypeText(currentSentence.text);

        state = State.COMPLETED;
    }

    private void HandleAction(StoryScene.Sentence.Action action)
    {
        if (characterDisplay == null) return;
        if (action.actionType == StoryScene.Sentence.Action.Type.NONE)
        {
            characterDisplay.ChangeSprite(action.speaker, action.spriteIndex);
        }
    }

    private IEnumerator TypeText(string text)
    {
        barText.text = "";
        state = State.PLAYING;
        for (int i = 0; i < text.Length; i++)
        {
            barText.text += text[i];
            yield return new WaitForSeconds(0.07f);
        }
        state = State.COMPLETED;
    }

    private void ClearText()
    {
        barText.text = "";
        personNameText.text = "";
    }

    private bool IsCompleted() => state == State.COMPLETED;
    private bool IsLastSentence() => sentenceIndex + 1 >= currentScene.sentences.Count;
}