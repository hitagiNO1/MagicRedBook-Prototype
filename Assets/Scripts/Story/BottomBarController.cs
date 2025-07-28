using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class BottomBarController : MonoBehaviour
{
    // 對話文字與人物名字 UI 元件
    public TextMeshProUGUI barText;
    public TextMeshProUGUI personNameText;
    private int sentenceIndex = -1; // 當前對話句子索引
    private StoryScene currentScene; // 當前故事場景
    private State state = State.COMPLETED; // 對話狀態（是否正在播放）
    private Animator animator; // 對話框動畫控制器
    private bool isHidden = false; // 是否隱藏中
    // 記錄角色位置的列表
    private List<bool> previousSpeakerPositions; // true 代表右側，false 代表左側
    // 左右人物顯示的座標
    public Vector2 Lcoords;
    public Vector2 Rcoords;
    // 左右對話框
    public Sprite LBar;
    public Sprite RBar;
    private Image image;
    public Image LBack;
    public Image RBack;
    private int leftCharacters = 0;
    private int rightCharacters = 0;
    // 管理場景中所有角色的 Sprite
    public Dictionary<Speaker, SpriteController> sprites;
    public GameObject spritesPrefab; // Sprite 父物件，用來放角色物件
    bool playedTimeline = false;
    private enum State
    {
        PLAYING, COMPLETED
    }

    private void Start()
    {
        sprites = new Dictionary<Speaker, SpriteController>();
        animator = GetComponent<Animator>();
        image = GetComponent<Image>();
        previousSpeakerPositions = new List<bool>(); // 初始化列表
    }
    private void Update()
    {
        // 如果 Timeline 正在播放，就不接受玩家輸入
        if (TimelineManager.Instance != null && TimelineManager.Instance.IsPlaying)
            return;

        if (Input.GetKeyDown(KeyCode.Space) && IsCompleted() && currentScene != null)
        {
            if (!IsLastSentence())
            {
                PlayNextSentence();
            }
            else
            {
                Debug.Log("故事播放完畢！");
                Hide();
            }
        }
    }

    // 隱藏對話框與所有角色圖像
    public void Hide()
    {
        if (!isHidden)
        {
            isHidden = true;
            bool hasLeft = false;
            bool hasRight = false;
            // 檢查之前的角色位置
            foreach (var position in previousSpeakerPositions)
            {
                if (!position) hasLeft = true; // 有左側角色
                if (position) hasRight = true; // 有右側角色
            }
            // 檢查當前句子的角色位置
            if (currentScene.sentences[sentenceIndex].actions.Count > 0)
            {
                var action = currentScene.sentences[sentenceIndex].actions[0];
                if (action.Right == false)
                {
                    hasLeft = true; // 當前句子有左側角色
                }
                else
                {
                    hasRight = true; // 當前句子有右側角色
                }
            }

            //Debug.Log($"Hide: hasLeft={hasLeft}, hasRight={hasRight}");
            // 根據角色的存在情況觸發動畫
            if (hasLeft && !hasRight)
            {
                animator.SetTrigger("LheadHide");
            }
            else if (!hasLeft && hasRight)
            {
                animator.SetTrigger("RheadHide");
            }
            else if (hasLeft && hasRight)
            {
                animator.SetTrigger("LheadHide");
                animator.SetTrigger("RheadHide");
            }
            animator.SetTrigger("Hide");
            // 同時隱藏所有角色圖像
            foreach (var pair in sprites)
            {
                pair.Value.Disappear();
            }
        }
    }
    // 顯示對話框
    public void Show()
    {
        animator.SetTrigger("Show");
        isHidden = false;
    }

    // 清空對話文字
    public void ClearText()
    {
        barText.text = "";
    }

    // 開始播放一整段場景對話
    public void PlayScene(StoryScene scene)
    {
        currentScene = scene;
        sentenceIndex = -1;
        previousSpeakerPositions.Clear(); // 清空之前的角色位置
        //初始化左右兩邊存在的腳色數量
        leftCharacters = 0;
        rightCharacters = 0;
        sprites.Clear();
        PlayNextSentence();
    }

    // 播放下一句話
    public void PlayNextSentence()
    {
        StartCoroutine(PlaySentenceCoroutine());
    }
    private IEnumerator PlaySentenceCoroutine()
    {
        if (currentScene == null || currentScene.sentences.Count == 0)
        {
            Debug.LogError("當前場景或句子列表為空，無法播放下一句。");
            yield break;
        }
        //紀錄前一句角色位置
        if (sentenceIndex >= 0 && sentenceIndex < currentScene.sentences.Count)
        {
            var prevSentence = currentScene.sentences[sentenceIndex];
            if (prevSentence.actions != null && prevSentence.actions.Count > 0)
            {
                var prevAction = prevSentence.actions[0];
                previousSpeakerPositions.Add(prevAction.Right);
            }
        }

        sentenceIndex++;
        if (sentenceIndex >= currentScene.sentences.Count)
        {
            Debug.Log("已經播放完所有句子。");
            yield break;
        }
        var sentence = currentScene.sentences[sentenceIndex];
        //播放 Timeline 動畫（如果有）
        bool hasTimeline = false;
        foreach (var timeline in sentence.timline)
        {
            if (timeline.animation && timeline.myPlayableAsset != null)
            {
                hasTimeline = true;

                /* 對話框暫時隱藏
                if (!isHidden)
                {
                    Hide();
                }
                */
                TimelineManager.Instance.PlayTimeline(timeline.myPlayableAsset);
                yield return new WaitUntil(() => !TimelineManager.Instance.IsPlaying);

                /* Timeline 播完再顯示
                if (sentenceIndex == 0 || sentence.text != "")
                {
                    Show(); //若該句是有內容的，播完動畫再顯示回來
                }
                */
            }
        }
        //只在第一句動畫播完後顯示對話框
        if (sentenceIndex == 0)
        {
            if (hasTimeline)
                yield return new WaitForSeconds(0.3f); // 微延遲
            Show();
        }

        // 顯示講話者資訊
        personNameText.text = sentence.speaker.speakerName;
        personNameText.color = sentence.speaker.textColor;

        // 切換對話框左右與背景
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
        // 執行角色動作
        ActSpeakers();
        // 顯示文字動畫
        yield return TypeText(sentence.text);
    }


    // 檢查是否已經完成當前句子的輸出
    public bool IsCompleted()
    {
        return state == State.COMPLETED;
    }

    // 檢查是否已經播放到最後一句
    public bool IsLastSentence()
    {
        return sentenceIndex + 1 == currentScene.sentences.Count;
    }

    // 逐字輸出對話內容的協程
    private IEnumerator TypeText(string text)
    {
        barText.text = "";
        state = State.PLAYING;
        int wordIndex = 0;

        while (state != State.COMPLETED)
        {
            barText.text += text[wordIndex];
            yield return new WaitForSeconds(0.05f);

            if (++wordIndex == text.Length)
            {
                state = State.COMPLETED;
                break;
            }
        }
    }

    // 執行角色的所有動作
    private void ActSpeakers()
    {
        List<StoryScene.Sentence.Action> actions = currentScene.sentences[sentenceIndex].actions;

        for (int i = 0; i < actions.Count; i++)
        {
            ActSpeaker(actions[i]);
        }
    }

    // 根據指定的動作控制角色出現、移動、消失等
    private void ActSpeaker(StoryScene.Sentence.Action action)
    {
        SpriteController controller = null;

        switch (action.actionType)
        {
            case StoryScene.Sentence.Action.Type.APPEAR: //帶動畫的出現，生成新的
                // 如果該角色尚未生成，則創建並加入字典
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
                // 設定角色圖片與位置
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
                return; // 注意這裡直接 return，不會執行下面的 SwitchSprite

            case StoryScene.Sentence.Action.Type.MOVE:  //已出現的腳色，換到對面
                if (sprites.ContainsKey(action.speaker))
                {
                    controller = sprites[action.speaker];
                    if (action.Right == false)
                        controller.Move(Lcoords);
                    else
                        controller.Move(Rcoords);
                }
                break;

            case StoryScene.Sentence.Action.Type.DISAPPEAR: //已出現的腳色退場，直接刪除
                if (sprites.ContainsKey(action.speaker))
                {
                    controller = sprites[action.speaker];
                    controller.Disappear();
                    if (action.Right == false)
                    {
                        leftCharacters--;
                        if (leftCharacters == 0) animator.SetTrigger("LheadHide");
                    }
                    else
                    {
                        rightCharacters--;
                        if (rightCharacters == 0) animator.SetTrigger("RheadHide");
                    }
                }
                break;

            case StoryScene.Sentence.Action.Type.SHOW:   //已生成過的腳色再出現，將隱藏的打開，不帶動畫
                if (sprites.ContainsKey(action.speaker))
                {
                    controller = sprites[action.speaker];
                    controller.Show();
                }
                break;

            case StoryScene.Sentence.Action.Type.HIDE:   //已出現的腳色暫時消失，隱藏
                if (sprites.ContainsKey(action.speaker))
                {
                    controller = sprites[action.speaker];
                    controller.Hide();
                }
                break;

            case StoryScene.Sentence.Action.Type.NONE:   //不動，僅變表情
                if (sprites.ContainsKey(action.speaker))
                {
                    controller = sprites[action.speaker];
                }
                break;
        }

        // 切換角色的圖片
        if (controller != null)
        {
            controller.SwitchSprite(action.speaker.sprites[action.spriteIndex]);
        }
    }
}
