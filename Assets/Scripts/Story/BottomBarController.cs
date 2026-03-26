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

    // 【新增】用來記錄強制清理的協程，避免重複觸發
    private Coroutine cleanupCoroutine;

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

            if (leftCharacters > 0) animator.SetTrigger("LheadHide");
            if (rightCharacters > 0) animator.SetTrigger("RheadHide");

            animator.SetTrigger("Hide");

            foreach (var pair in sprites)
            {
                if (pair.Value != null)
                {
                    pair.Value.Disappear();
                }
            }

            leftCharacters = 0;
            rightCharacters = 0;

            // 【新增】啟動終極保險：2.5秒後強制掃蕩畫面上殘留的頭像
            if (cleanupCoroutine != null) StopCoroutine(cleanupCoroutine);
            cleanupCoroutine = StartCoroutine(ForceCleanupSprites());
        }
    }

    // 【新增】終極掃蕩防呆機制
    private IEnumerator ForceCleanupSprites()
    {
        // 等待 2.5 秒，讓正常的 Disappear 動畫和角色自己的銷毀程序先跑完
        yield return new WaitForSeconds(2.5f);

        // 如果在這 2.5 秒內，玩家又觸發了新的對話 (對話框又跑出來了)，就取消掃蕩，避免誤殺新角色
        if (!isHidden) yield break;

        // 暴力清除：不管字典裡有沒有紀錄，直接砍掉 spritesPrefab 下面的所有子物件
        if (spritesPrefab != null)
        {
            foreach (Transform child in spritesPrefab.transform)
            {
                if (child != null)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        // 確保字典也是乾淨的
        sprites.Clear();
        Debug.Log("已執行頭像殘留掃蕩機制。");
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
        state = State.PLAYING;
        StartCoroutine(PlaySentenceCoroutine());
    }

    private IEnumerator PlaySentenceCoroutine()
    {
        if (currentScene == null || currentScene.sentences.Count == 0)
        {
            Debug.LogError("當前場景或句子列表為空，無法播放下一句。");
            yield break;
        }

        sentenceIndex++;
        if (sentenceIndex >= currentScene.sentences.Count)
        {
            Debug.Log("已經播放完所有句子。");
            yield break;
        }
        var sentence = currentScene.sentences[sentenceIndex];

        bool hasTimeline = false;
        foreach (var timeline in sentence.timline)
        {
            if (timeline.animation && timeline.myPlayableAsset != null &&
                timeline.timing == StoryScene.Sentence.TimeLine.TriggerTiming.BeforeText)
            {
                TimelineManager.Instance.PlayTimeline(timeline.myPlayableAsset);
                yield return new WaitUntil(() => !TimelineManager.Instance.IsPlaying);
            }
        }

        if (sentenceIndex == 0)
        {
            if (hasTimeline)
                yield return new WaitForSeconds(0.3f);
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

        if (sentence.timline.Exists(t => t.timing == StoryScene.Sentence.TimeLine.TriggerTiming.AfterClick))
        {
            yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0));

            foreach (var timeline in sentence.timline)
            {
                if (timeline.animation && timeline.myPlayableAsset != null &&
                    timeline.timing == StoryScene.Sentence.TimeLine.TriggerTiming.AfterClick)
                {
                    TimelineManager.Instance.PlayTimeline(timeline.myPlayableAsset);
                    yield return new WaitUntil(() => !TimelineManager.Instance.IsPlaying);
                }
            }
        }
    }

    public bool IsCompleted()
    {
        return state == State.COMPLETED;
    }

    public bool IsLastSentence()
    {
        return sentenceIndex + 1 == currentScene.sentences.Count;
    }

    private IEnumerator TypeText(string text)
    {
        barText.text = "";
        int wordIndex = 0;

        while (state == State.PLAYING && wordIndex < text.Length)
        {
            barText.text += text[wordIndex];
            yield return new WaitForSeconds(0.05f);
            wordIndex++;
        }

        barText.text = text;
        state = State.COMPLETED;
    }

    public void SkipTyping()
    {
        if (state == State.PLAYING)
        {
            state = State.COMPLETED;
        }
    }

    private void ActSpeakers()
    {
        List<StoryScene.Sentence.Action> actions = currentScene.sentences[sentenceIndex].actions;
        for (int i = 0; i < actions.Count; i++)
        {
            ActSpeaker(actions[i]);
        }
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
                else
                {
                    controller = sprites[action.speaker];
                }
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
                    if (action.Right == false)
                        controller.Move(Lcoords);
                    else
                        controller.Move(Rcoords);
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

                    if (action.Right == false)
                    {
                        leftCharacters--;
                    }
                    else
                    {
                        rightCharacters--;
                    }
                }
                break;

            case StoryScene.Sentence.Action.Type.NONE:
                if (sprites.ContainsKey(action.speaker))
                {
                    controller = sprites[action.speaker];
                }
                break;
        }

        if (controller != null)
        {
            controller.SwitchSprite(action.speaker.sprites[action.spriteIndex]);
        }
    }
}