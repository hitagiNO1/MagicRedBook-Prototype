using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Playables; // 引用 Timeline 命名空間
using UnityEngine.SceneManagement; // 引用場景管理命名空間

public class FirstText : MonoBehaviour
{
    [Header("UI Components")]
    public TextMeshProUGUI barText;
    public Animator animator;

    [Header("Timeline Control")]
    public PlayableDirector director;

    [Header("Audio Control")] // 【新增】音效控制
    public AudioSource bgmSource; // 請將播放 BGM 的 AudioSource 拖曳到這裡
    public float fadeDuration = 2f; // 淡出時間，剛好配合場景結束的等待時間

    [Header("Data")]
    public StoryScene currentScene;
    public string mainSceneName;

    private int sentenceIndex = -1;
    private StoryScene.Sentence currentSentence;
    private enum State { PLAYING, COMPLETED }

    private State state = State.PLAYING;
    private bool isTyping = false; // 【新增】用來判斷現在是不是正在「打字中」

    private void Start()
    {
        if (animator == null) animator = GetComponent<Animator>();

        StartCoroutine(PlaySceneSequence());
    }

    private void Update()
    {
        // 玩家點擊：略過打字、下一句或結束場景
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            // 【新增】如果正在打字，直接略過並顯示整句話
            if (isTyping)
            {
                isTyping = false;
            }
            // 只有在狀態是 COMPLETED 時，才允許進入下一句或結束
            else if (IsCompleted())
            {
                if (!IsLastSentence())
                {
                    PlayNextSentence();
                }
                else
                {
                    StartCoroutine(EndSceneSequence());
                }
            }
        }
    }

    // --- [核心] 1. 開場流程 ---
    private IEnumerator PlaySceneSequence()
    {
        state = State.PLAYING;

        yield return new WaitForSeconds(2f); // 等漸入
        if (animator != null) animator.SetTrigger("Show");

        sentenceIndex = -1;
        PlayNextSentence();
    }

    // --- [核心] 2. 結束流程 ---
    private IEnumerator EndSceneSequence()
    {
        // 鎖定狀態，避免重複點擊
        state = State.PLAYING;

        if (animator != null) animator.SetTrigger("Hide");

        // 【新增】開始淡出 BGM
        if (bgmSource != null)
        {
            StartCoroutine(FadeOutBGM());
        }

        Debug.Log("場景結束，準備切換");

        // 將原本寫死的 2f 改用 fadeDuration，讓兩者時間一致
        yield return new WaitForSeconds(fadeDuration);

        ClearText();
        if (!string.IsNullOrEmpty(mainSceneName))
        {
            SceneManager.LoadScene(mainSceneName);
        }
        else
        {
            Debug.LogWarning("未設定 mainSceneName，無法切換場景！請在 Inspector 中檢查。");
        }
    }

    // 【新增】BGM淡出協程
    private IEnumerator FadeOutBGM()
    {
        float startVolume = bgmSource.volume;
        float timeElapsed = 0f;

        while (timeElapsed < fadeDuration)
        {
            timeElapsed += Time.deltaTime;
            // 讓音量隨著時間從 startVolume 遞減到 0
            bgmSource.volume = Mathf.Lerp(startVolume, 0f, timeElapsed / fadeDuration);
            yield return null;
        }

        bgmSource.volume = 0f;
        bgmSource.Stop();
    }

    // --- 以下對話邏輯 ---
    public void PlayNextSentence()
    {
        StartCoroutine(PlaySentenceCoroutine());
    }

    private IEnumerator PlaySentenceCoroutine()
    {
        state = State.PLAYING;

        if (currentScene == null || currentScene.sentences.Count == 0) yield break;

        sentenceIndex++;
        currentSentence = currentScene.sentences[sentenceIndex];

        if (currentSentence.timline != null && director != null)
        {
            foreach (var timeline in currentSentence.timline)
            {
                if (timeline.timing == StoryScene.Sentence.TimeLine.TriggerTiming.BeforeText)
                {
                    director.playableAsset = timeline.myPlayableAsset;
                    director.Play();
                    yield return new WaitUntil(() => director.state != PlayState.Playing);
                }
            }
        }

        // 打字
        yield return TypeText(currentSentence.text);
    }

    private IEnumerator TypeText(string text)
    {
        barText.text = "";
        isTyping = true; // 【新增】開啟打字狀態
        int wordIndex = 0;

        // 【修改】將原本的 for 迴圈改成 while，只要 isTyping 為 true 就繼續一個個字打
        while (isTyping && wordIndex < text.Length)
        {
            barText.text += text[wordIndex];
            yield return new WaitForSeconds(0.05f);
            wordIndex++;
        }

        // 【新增】無論是正常打完，還是被玩家點擊中斷(isTyping變false)，最後都直接把整句話完整顯示出來
        barText.text = text;
        isTyping = false;

        state = State.COMPLETED; // 解鎖狀態
    }

    private void ClearText()
    {
        barText.text = "";
    }

    private bool IsCompleted() => state == State.COMPLETED;
    private bool IsLastSentence() => sentenceIndex + 1 >= currentScene.sentences.Count;
}