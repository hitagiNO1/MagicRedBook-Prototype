using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BottomBarController : MonoBehaviour
{
    public TextMeshProUGUI barText;
    public TextMeshProUGUI personNameText;
    private int sentenceIndex = -1;
    private StoryScene currentScene;
    private State state = State.COMPLETED;
    private Animator animator;
    private bool isHidden = false;

    public Vector2 Lcoords;
    public Vector2 Rcoords;

    public Sprite LBar;
    public Sprite RBar;
    public Sprite MidBar;

    private Image image;
    public Image LBack;
    public Image RBack;
    private int leftCharacters = 0;
    private int rightCharacters = 0;

    public Dictionary<Speaker, SpriteController> sprites;
    public GameObject spritesPrefab;
    bool playedTimeline = false;

    private Coroutine cleanupCoroutine;
    private Coroutine playCoroutine;
    private bool isTyping = false;

    private enum State
    {
        PLAYING, COMPLETED
    }

    private void Start()
    {
        sprites = new Dictionary<Speaker, SpriteController>();
        animator = GetComponent<Animator>();
        image = GetComponent<Image>();
    }

    public void Hide()
    {
        if (!isHidden)
        {
            isHidden = true;

            if (playCoroutine != null)
            {
                StopCoroutine(playCoroutine);
                playCoroutine = null;
            }

            // 【重點修改 1】在隱藏前，先抓取「當前最後一句話」是否帶有 AfterClick 的 Timeline
            List<StoryScene.Sentence.TimeLine> afterClickTimelines = new List<StoryScene.Sentence.TimeLine>();
            if (currentScene != null && sentenceIndex >= 0 && sentenceIndex < currentScene.sentences.Count)
            {
                afterClickTimelines = currentScene.sentences[sentenceIndex].timline
                    .FindAll(t => t.timing == StoryScene.Sentence.TimeLine.TriggerTiming.AfterClick && t.animation && t.myPlayableAsset != null);
            }

            // 觸發 UI 漸出隱藏動畫
            if (leftCharacters > 0) animator.SetTrigger("LheadHide");
            if (rightCharacters > 0) animator.SetTrigger("RheadHide");
            animator.SetTrigger("Hide");

            foreach (var pair in sprites)
            {
                if (pair.Value != null) pair.Value.Disappear();
            }

            leftCharacters = 0;
            rightCharacters = 0;

            // 開啟隱藏後的協程處理 (負責播 TL 和 清理殘留)
            if (cleanupCoroutine != null) StopCoroutine(cleanupCoroutine);
            cleanupCoroutine = StartCoroutine(HideAndCleanupCoroutine(afterClickTimelines));
        }
    }

    private IEnumerator HideAndCleanupCoroutine(List<StoryScene.Sentence.TimeLine> timelines)
    {
        // 【重點修改 2】給對話框一個漸出的時間。假設 Hide 動畫約 0.3 秒，你可以依據你的動畫長度修改這個數值。
        // 這樣 TL 動畫就會在對話框「漸出得差不多時」才開始播放。
        yield return new WaitForSeconds(0.3f);

        // 播放最後一句的 AfterClick 動畫
        if (timelines != null && timelines.Count > 0)
        {
            foreach (var timeline in timelines)
            {
                TimelineManager.Instance.PlayTimeline(timeline.myPlayableAsset);
                yield return new WaitUntil(() => !TimelineManager.Instance.IsPlaying);
            }
        }

        // 正常等待 2.5 秒，確保其他角色的 Disappear 動畫播完
        yield return new WaitForSeconds(2.5f);

        if (!isHidden) yield break;

        // 強制清理
        if (spritesPrefab != null)
        {
            foreach (Transform child in spritesPrefab.transform)
            {
                if (child != null) Destroy(child.gameObject);
            }
        }

        sprites.Clear();
    }

    public void Show()
    {
        animator.SetTrigger("Show");
        isHidden = false;
    }

    public void ClearText()
    {
        barText.text = "";
    }

    public void PlayScene(StoryScene scene)
    {
        currentScene = scene;
        sentenceIndex = -1;
        leftCharacters = 0;
        rightCharacters = 0;
        sprites.Clear();
        PlayNextSentence();
    }

    public void PlayNextSentence()
    {
        if (playCoroutine != null) StopCoroutine(playCoroutine);

        // 【重點修改 3】檢查「剛讀完的這句話」是否有 AfterClick 動畫。
        // 如果有，就先播放 TL，播完再真的進入下一句。
        if (currentScene != null && sentenceIndex >= 0 && sentenceIndex < currentScene.sentences.Count)
        {
            var sentence = currentScene.sentences[sentenceIndex];
            var afterClickTimelines = sentence.timline.FindAll(t => t.timing == StoryScene.Sentence.TimeLine.TriggerTiming.AfterClick && t.animation && t.myPlayableAsset != null);

            if (afterClickTimelines.Count > 0)
            {
                playCoroutine = StartCoroutine(HandleAfterClickThenNext(afterClickTimelines));
                return;
            }
        }

        StartActualNextSentence();
    }

    private IEnumerator HandleAfterClickThenNext(List<StoryScene.Sentence.TimeLine> timelines)
    {
        state = State.PLAYING; // 鎖定狀態，防止這期間玩家狂點

        foreach (var timeline in timelines)
        {
            TimelineManager.Instance.PlayTimeline(timeline.myPlayableAsset);
            yield return new WaitUntil(() => !TimelineManager.Instance.IsPlaying);
        }

        StartActualNextSentence(); // TL播完，正式進入下一句
    }

    private void StartActualNextSentence()
    {
        state = State.PLAYING;
        playCoroutine = StartCoroutine(PlaySentenceCoroutine());
    }

    private IEnumerator PlaySentenceCoroutine()
    {
        if (currentScene == null || currentScene.sentences.Count == 0) yield break;

        sentenceIndex++;
        if (sentenceIndex >= currentScene.sentences.Count) yield break;

        var sentence = currentScene.sentences[sentenceIndex];
        bool hasTimeline = false;

        foreach (var timeline in sentence.timline)
        {
            if (timeline.animation && timeline.myPlayableAsset != null &&
                timeline.timing == StoryScene.Sentence.TimeLine.TriggerTiming.BeforeText)
            {
                hasTimeline = true;
                TimelineManager.Instance.PlayTimeline(timeline.myPlayableAsset);
                yield return new WaitUntil(() => !TimelineManager.Instance.IsPlaying);
            }
        }

        if (sentenceIndex == 0)
        {
            if (hasTimeline) yield return new WaitForSeconds(0.3f);
            Show();
        }

        personNameText.text = sentence.speaker.speakerName;
        personNameText.color = sentence.speaker.textColor;
        if (sentence.speaker.speakerName == "旁白" || sentence.speaker.speakerName == "？？？" || sentence.speaker.speakerName == " ")
        {
            image.sprite = MidBar;
        }

        foreach (var action in sentence.actions)
        {
            if (!action.Right)
            {
                image.sprite = LBar;
                LBack.sprite = action.speaker.BackColor;
            }
            else
            {
                image.sprite = RBar;
                RBack.sprite = action.speaker.BackColor;
            }
        }

        ActSpeakers();

        yield return TypeText(sentence.text);

        foreach (var timeline in sentence.timline)
        {
            if (timeline.animation && timeline.myPlayableAsset != null &&
                timeline.timing == StoryScene.Sentence.TimeLine.TriggerTiming.AfterText)
            {
                TimelineManager.Instance.PlayTimeline(timeline.myPlayableAsset);
                yield return new WaitUntil(() => !TimelineManager.Instance.IsPlaying);
            }
        }

        // 【重點修改 4】直接將狀態設為 COMPLETED。
        // 不在這裡等點擊了！交給 GameManager 判斷，GameManager 呼叫 Hide/PlayNext 時，就會自動觸發上面的攔截機制。
        state = State.COMPLETED;
    }

    public bool IsCompleted() { return state == State.COMPLETED; }
    public bool IsLastSentence() { return sentenceIndex + 1 == currentScene.sentences.Count; }

    private IEnumerator TypeText(string text)
    {
        barText.text = "";
        int wordIndex = 0;
        isTyping = true;

        while (isTyping && wordIndex < text.Length)
        {
            barText.text += text[wordIndex];
            yield return new WaitForSeconds(0.05f);
            wordIndex++;
        }

        barText.text = text;
        isTyping = false;
    }

    public void SkipTyping()
    {
        if (isTyping) isTyping = false;
    }

    private void ActSpeakers()
    {
        List<StoryScene.Sentence.Action> actions = currentScene.sentences[sentenceIndex].actions;
        for (int i = 0; i < actions.Count; i++) ActSpeaker(actions[i]);
    }

    private void ActSpeaker(StoryScene.Sentence.Action action)
    {
        SpriteController controller = null;

        switch (action.actionType)
        {
            case StoryScene.Sentence.Action.Type.APPEAR:
                if (!sprites.ContainsKey(action.speaker))
                {
                    controller = Instantiate(action.speaker.prefab.gameObject, spritesPrefab.transform)
                        .GetComponent<SpriteController>();
                    sprites.Add(action.speaker, controller);
                }
                else controller = sprites[action.speaker];

                controller.Setup(action.speaker.sprites[action.spriteIndex]);

                if (action.Right == false)
                {
                    controller.Appear(Lcoords);
                    if (leftCharacters == 0) animator.SetTrigger("LheadShow");
                    leftCharacters++;
                }
                else
                {
                    controller.Appear(Rcoords);
                    if (rightCharacters == 0) animator.SetTrigger("RheadShow");
                    rightCharacters++;
                }
                return;

            case StoryScene.Sentence.Action.Type.MOVE:
                if (sprites.ContainsKey(action.speaker))
                {
                    controller = sprites[action.speaker];
                    if (action.Right == false) controller.Move(Lcoords);
                    else controller.Move(Rcoords);
                }
                break;

            case StoryScene.Sentence.Action.Type.DISAPPEAR:
                if (sprites.ContainsKey(action.speaker))
                {
                    controller = sprites[action.speaker];
                    controller.Disappear();
                    sprites.Remove(action.speaker);

                    if (action.Right == false)
                    {
                        leftCharacters--;
                        animator.SetTrigger("LheadHide");
                    }
                    else
                    {
                        rightCharacters--;
                        animator.SetTrigger("RheadHide");
                    }
                }
                break;

            case StoryScene.Sentence.Action.Type.SHOW:
                if (sprites.ContainsKey(action.speaker))
                {
                    controller = sprites[action.speaker];
                    controller.Show();
                }
                else
                {
                    controller = Instantiate(action.speaker.prefab.gameObject, spritesPrefab.transform)
                        .GetComponent<SpriteController>();
                    controller.Setup(action.speaker.sprites[action.spriteIndex]);
                    sprites.Add(action.speaker, controller);
                    controller.Appear(action.Right ? Rcoords : Lcoords);

                    if (action.Right) rightCharacters++;
                    else leftCharacters++;
                }
                break;

            case StoryScene.Sentence.Action.Type.HIDE:
                if (sprites.ContainsKey(action.speaker))
                {
                    controller = sprites[action.speaker];
                    controller.Hide();
                    sprites.Remove(action.speaker);

                    if (action.Right == false) leftCharacters--;
                    else rightCharacters--;
                }
                break;

            case StoryScene.Sentence.Action.Type.NONE:
                if (sprites.ContainsKey(action.speaker))
                {
                    controller = sprites[action.speaker];
                }
                break;
        }

        if (controller != null) controller.SwitchSprite(action.speaker.sprites[action.spriteIndex]);
    }
}