using System.Collections;
using TMPro;
using UnityEngine;
using static StoryScene;

public class CGText : MonoBehaviour
{
    public TextMeshProUGUI barText;
    public TextMeshProUGUI personNameText;
    public StoryScene currentScene;

    private int sentenceIndex = -1;
    private Animator animator;
    private StoryScene.Sentence currentSentence;

    private enum State { PLAYING, COMPLETED }
    private State state = State.COMPLETED;

    private void Start()
    {
        animator = GetComponent<Animator>();
        StartCoroutine(PlayScene());
    }

    private void Update()
    {
        // 玩家點擊：下一句或跳過
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            if (!IsCompleted()) return;

            if (IsCompleted() && currentScene != null)
            {
                if (!IsLastSentence())
                {
                    PlayNextSentence();
                }
                else
                {
                    animator.SetTrigger("Hide");
                    Debug.Log("對話已結束。");
                    ClearText();
                }
            }
        }
    }

    private IEnumerator PlayScene()
    {
        // 顯示對話框動畫
        animator.SetTrigger("Show");
        yield return new WaitForSeconds(0.5f);

        sentenceIndex = -1;
        PlayNextSentence();
    }

    public void PlayNextSentence()
    {
        StartCoroutine(PlaySentenceCoroutine());
    }

    private IEnumerator PlaySentenceCoroutine()
    {
        if (currentScene == null || currentScene.sentences.Count == 0)
        {
            Debug.LogError("目前場景或句子為空！");
            yield break;
        }

        sentenceIndex++;
        if (sentenceIndex >= currentScene.sentences.Count)
            yield break;

        currentSentence = currentScene.sentences[sentenceIndex];

        // ✅ 先播放 BeforeText Timeline，並「等待它結束」再開始打字
        if (currentSentence.timline != null)
        {
            foreach (var timeline in currentSentence.timline)
            {
                if (timeline.timing == StoryScene.Sentence.TimeLine.TriggerTiming.BeforeText)
                {
                    TimelineManager.Instance.PlayCG(timeline.myPlayableAsset);
                    // 等待 Timeline 播放完畢
                    yield return new WaitUntil(() => !TimelineManager.Instance.IsPlaying);
                }
            }
        }

        // 顯示說話者
        personNameText.text = currentSentence.speaker.speakerName;
        personNameText.color = currentSentence.speaker.textColor;

        // ✅ 播放 AfterText Timeline（與打字同時進行，不等待）
        if (currentSentence.timline != null)
        {
            foreach (var timeline in currentSentence.timline)
            {
                if (timeline.timing == StoryScene.Sentence.TimeLine.TriggerTiming.AfterText)
                {
                    TimelineManager.Instance.PlayCG(timeline.myPlayableAsset);
                }
            }
        }

        // ✅ 開始打字
        yield return TypeText(currentSentence.text);

        if (currentSentence.timline.Exists(t => t.timing == StoryScene.Sentence.TimeLine.TriggerTiming.AfterClick))
        {
            yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0));

            foreach (var timeline in currentSentence.timline)
            {
                if (timeline.animation && timeline.myPlayableAsset != null &&
                    timeline.timing == StoryScene.Sentence.TimeLine.TriggerTiming.AfterClick)
                {
                    TimelineManager.Instance.PlayCG(timeline.myPlayableAsset);
                    yield return new WaitUntil(() => !TimelineManager.Instance.IsPlaying);
                }
            }
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
