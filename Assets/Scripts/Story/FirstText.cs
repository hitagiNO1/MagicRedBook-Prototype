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

    [Header("Data")]
    public StoryScene currentScene;
    public string mainSceneName;

    private int sentenceIndex = -1;
    private StoryScene.Sentence currentSentence;
    private enum State { PLAYING, COMPLETED }

    // 【修復】初始狀態設為 PLAYING，防止遊戲剛開始(漸入期間)的誤觸
    private State state = State.PLAYING;

    private void Start()
    {
        // 確保 Animator 有被指派
        if (animator == null) animator = GetComponent<Animator>();

        StartCoroutine(PlaySceneSequence());
    }

    private void Update()
    {
        // 玩家點擊：下一句或跳過
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
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
        }
    }

    // --- [核心] 1. 開場流程 ---
    private IEnumerator PlaySceneSequence()
    {
        state = State.PLAYING; // 鎖定狀態

        yield return new WaitForSeconds(2f); // 等漸入
        if (animator != null) animator.SetTrigger("Show");
        // 開始第一句對話
        sentenceIndex = -1;
        PlayNextSentence();
    }

    // --- [核心] 2. 結束流程 ---
    private IEnumerator EndSceneSequence()
    {
        // 鎖定狀態，避免重複點擊
        state = State.PLAYING;

        if (animator != null) animator.SetTrigger("Hide");

        Debug.Log("場景結束，準備切換");
        yield return new WaitForSeconds(4f);
        ClearText();
        // 檢查是否有填寫場景名稱，避免報錯
        if (!string.IsNullOrEmpty(mainSceneName))
        {
            SceneManager.LoadScene(mainSceneName);
        }
        else
        {
            Debug.LogWarning("未設定 mainSceneName，無法切換場景！請在 Inspector 中檢查。");
        }
    }

    // --- 以下對話邏輯 ---
    public void PlayNextSentence()
    {
        StartCoroutine(PlaySentenceCoroutine());
    }

    private IEnumerator PlaySentenceCoroutine()
    {
        // 【關鍵修復】一進入協程立刻鎖定狀態，防止 Timeline 播放期間玩家連點
        state = State.PLAYING;

        if (currentScene == null || currentScene.sentences.Count == 0) yield break;

        sentenceIndex++;
        currentSentence = currentScene.sentences[sentenceIndex];

        // 確保 director 存在，並正確替換 PlayableAsset 後播放
        if (currentSentence.timline != null && director != null)
        {
            foreach (var timeline in currentSentence.timline)
            {
                if (timeline.timing == StoryScene.Sentence.TimeLine.TriggerTiming.BeforeText)
                {
                    // 將當前句子的 Timeline 資源指派給 Director 並播放
                    director.playableAsset = timeline.myPlayableAsset;
                    director.Play();

                    // 等待 Timeline 播放完畢再繼續執行打字機效果
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

        for (int i = 0; i < text.Length; i++)
        {
            barText.text += text[i];
            yield return new WaitForSeconds(0.05f);
        }

        // 【修復】文字全部打完後，才解鎖狀態，允許玩家點擊下一句
        state = State.COMPLETED;
    }

    private void ClearText()
    {
        barText.text = "";
    }

    private bool IsCompleted() => state == State.COMPLETED;
    private bool IsLastSentence() => sentenceIndex + 1 >= currentScene.sentences.Count;
}