using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Playables; // 引用 Timeline 命名空間
using UnityEngine.SceneManagement;
using static StoryScene;

public class CGTextEND : MonoBehaviour
{
    [Header("UI Components")]
    public TextMeshProUGUI barText;
    public TextMeshProUGUI personNameText;
    public CGSprite characterDisplay;

    [Header("Timeline Settings")]
    public PlayableDirector director; // 請將掛在同一物件上的 PlayableDirector 拉進來
    public PlayableAsset TCG;              // 中間的背景動畫
    public PlayableAsset FadeOutTimeline;  // 淡出 (從亮變黑)

    // --- ★ 新增：音樂設定 ---
    [Header("Audio Settings")]
    public AudioSource bgmSource;          // 掛載播放 BGM 的 AudioSource
    public float bgmFadeDuration = 2.0f;   // 淡入淡出時間 (預設 1 秒)
    private float originalBgmVolume = 1.0f;// 紀錄原本設定的音量

    [Header("Data")]
    public StoryScene currentScene;
    public string mainSceneName;
    public float waitAfterSceneEnd = 3.5f;
    private int sentenceIndex = -1;
    private Animator animator;
    private StoryScene.Sentence currentSentence;

    private enum State { PLAYING, COMPLETED }
    private State state = State.COMPLETED;
    private bool isSkipping = false;

    private void Awake()
    {
        // ★ 在 Awake 時先記錄 Inspector 中設定的音量，並將音量歸零準備淡入
        if (bgmSource != null)
        {
            originalBgmVolume = bgmSource.volume;
            bgmSource.volume = 0f;
        }
    }

    private void Start()
    {
        animator = GetComponent<Animator>();

        // 防呆：如果忘記拉 Director，嘗試自動抓取
        if (director == null)
            director = GetComponent<PlayableDirector>();

        // ★ 剛進入場景時，啟動 BGM 淡入
        if (bgmSource != null)
        {
            if (!bgmSource.isPlaying) bgmSource.Play();
            StartCoroutine(FadeBGM(originalBgmVolume, bgmFadeDuration));
        }

        StartCoroutine(PlaySceneSequence());
    }

    private void Update()
    {
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
            // 情況 B：字還在打 (PLAYING) -> 觸發跳過打字
            else
            {
                isSkipping = true;
            }
        }
    }

    // --- ★ 新增：共用的 BGM 淡入淡出協程 ---
    private IEnumerator FadeBGM(float targetVolume, float duration)
    {
        if (bgmSource == null) yield break;

        float startVolume = bgmSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            // 根據時間比例，平滑改變音量
            bgmSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
            yield return null;
        }

        bgmSource.volume = targetVolume;
    }

    // --- [核心] 1. 開場流程 ---
    private IEnumerator PlaySceneSequence()
    {
        // 播放 TCG (背景)
        if (TCG != null)
        {
            director.playableAsset = TCG;
            // 設定為循環播放 (這樣對話時背景會一直動)
            director.extrapolationMode = DirectorWrapMode.Hold;
            director.Play();
        }
        yield return new WaitForSeconds(1.5f); // 等 UI 浮現
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

        // ★ 準備結束場景時，啟動 BGM 淡出到 0
        if (bgmSource != null)
        {
            StartCoroutine(FadeBGM(0f, bgmFadeDuration));
        }

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
            yield return new WaitForSeconds(waitAfterSceneEnd);

        }

        Debug.Log("場景結束 (全黑)。");
        StartCoroutine(ReturnToMainScene());
    }

    private IEnumerator ReturnToMainScene()
    {
        if (mainSceneName == null)
        {
            Debug.LogError("主場景名稱未設定！");
            yield break;
        }
        else
        {
            SceneManager.LoadScene(mainSceneName);
            yield return null; // 等待一幀確保場景切換完成
        }
    }

    // --- 以下對話邏輯 --- 

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
        isSkipping = false; // 每次開始打字前，重置跳過開關

        for (int i = 0; i < text.Length; i++)
        {
            // 如果玩家按下了跳過
            if (isSkipping)
            {
                barText.text = text; // 直接顯示完整文字
                break;               // 打斷迴圈，不再逐字等待
            }

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