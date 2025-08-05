using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CGText : MonoBehaviour
{
    // 對話內容文字（顯示在對話框中）
    public TextMeshProUGUI barText;
    // 說話者名稱文字（顯示在名稱框中）
    public TextMeshProUGUI personNameText;
    // 目前對話句子的索引
    private int sentenceIndex = -1;
    // 當前播放的對話場景（外部指派）
    public StoryScene currentScene;
    // 狀態列舉，用來標示目前是否正在播放文字
    private enum State { PLAYING, COMPLETED }
    private State state = State.COMPLETED;
    private Animator animator; // 對話框動畫控制器

    private void Start()
    {
        animator = GetComponent<Animator>();
        StartCoroutine(PlayScene());
    }
    private void Update()
    {
        // 每次按下空白鍵並且當前句子播放完畢，才允許播放下一句
        if (Input.GetKeyDown(KeyCode.Space) && IsCompleted() && currentScene != null)
        {
            if (!IsLastSentence())
            {
                // 播放下一句對話
                PlayNextSentence();
            }
            else
            {
                animator.SetTrigger("Hide");
                // 劇情播放結束，條件這加
                Debug.Log("對話已結束。");
                ClearText();
            }
        }
    }

    // 播放整個對話場景
    private IEnumerator PlayScene()
    {
        yield return new WaitForSeconds(2f);
        animator.SetTrigger("Show");
        sentenceIndex = -1; // 從第一句開始
        PlayNextSentence();
    }

    // 播放下一句
    public void PlayNextSentence()
    {
        StartCoroutine(PlaySentenceCoroutine());
    }

    // 協程：播放句子並逐字顯示
    private IEnumerator PlaySentenceCoroutine()
    {
        // 檢查場景或句子是否為空
        if (currentScene == null || currentScene.sentences.Count == 0)
        {
            Debug.LogError("目前場景或句子為空！");
            yield break;
        }

        sentenceIndex++; // 移至下一句
        if (sentenceIndex >= currentScene.sentences.Count)
        {
            yield break; // 超出句子數，結束播放
        }

        // 取得目前要顯示的句子
        var sentence = currentScene.sentences[sentenceIndex];

        // 顯示角色名稱與顏色
        personNameText.text = sentence.speaker.speakerName;
        personNameText.color = sentence.speaker.textColor;

        // 播放文字打字動畫
        yield return TypeText(sentence.text);
    }

    // 協程：逐字顯示文字
    private IEnumerator TypeText(string text)
    {
        barText.text = ""; // 清空現有文字
        state = State.PLAYING; // 標記為播放中

        for (int i = 0; i < text.Length; i++)
        {
            barText.text += text[i]; // 一次加一個字
            yield return new WaitForSeconds(0.05f); // 每字延遲
        }

        state = State.COMPLETED; // 播放完成
    }

    // 清除文字顯示
    private void ClearText()
    {
        barText.text = "";
        personNameText.text = "";
    }

    // 回傳目前是否播放完成
    private bool IsCompleted()
    {
        return state == State.COMPLETED;
    }

    // 檢查是否已經是最後一句
    private bool IsLastSentence()
    {
        return sentenceIndex + 1 >= currentScene.sentences.Count;
    }
}
