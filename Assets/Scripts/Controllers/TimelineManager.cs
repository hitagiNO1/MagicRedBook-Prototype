using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class TimelineManager : MonoBehaviour
{
    // 單例模式：讓其他腳本能隨時取得 TimelineManager 實例
    public static TimelineManager Instance { get; private set; }

    [Header("Timeline 資產清單（請在 Inspector 指派）")]
    public List<PlayableAsset> timelineList;
    public PlayableDirector director;

    // 紀錄目前是否正在播放動畫
    public bool IsPlaying { get; private set; }
    private bool isWaitingForInput = false;

    // 用來記錄播放過哪些 Timeline（以名稱為主）
    private HashSet<string> playedTimelines = new HashSet<string>();

    private void Awake()
    {
        // 設定 Instance（單例模式）
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    // 註冊播放結束事件
    private void OnEnable()
    {
        if (director != null)
            director.stopped += OnTimelineStopped;
    }

    // 移除事件監聽
    private void OnDisable()
    {
        if (director != null)
            director.stopped -= OnTimelineStopped;
    }

    //當Timeline 播放結束時被觸發
    private void OnTimelineStopped(PlayableDirector dir)
    {
        IsPlaying = false; //重設播放狀態
        GameStateManager.Instance.RevertToPreviousState();
        //如果正在播放某個 Timeline，將其名稱記錄到已播放清單
        if (director.playableAsset != null)
        {
            string name = director.playableAsset.name;
            playedTimelines.Add(name);
        }
    }

    //根據 List 的索引來播放 Timeline
    public void PlayTimelineByIndex(int index)
    {
        if (index < 0 || index >= timelineList.Count)
        {
            Debug.LogWarning($"Timeline index {index} 不存在");
            return;
        }
        PlayTimeline(timelineList[index]);
    }

    //播放指定的 Timeline 資產
    public void PlayTimeline(PlayableAsset asset)
    {

            if (asset == null || director == null)
            {
                Debug.LogWarning("PlayableAsset 或 Director 為 null");
                return;
            }

            Debug.Log("開始播放 Timeline：" + asset.name);
            GameStateManager.Instance.SetState(GameState.Mangaing);
            director.playableAsset = asset;
            director.time = 0;
            IsPlaying = true;
            director.Play();
    }

    //查詢某個 Timeline 名稱是否已經播放過
    public bool HasPlayed(string timelineName)
    {
        return playedTimelines.Contains(timelineName);
    }
    public void WaitForInputToContinue()
    {
        if (director != null && director.state == PlayState.Playing)
        {
            director.Pause();             // 暫停 Timeline
            isWaitingForInput = true;     // 等待輸入
        }
    }
    private void Update()
    {
        if (isWaitingForInput && Input.GetKeyDown(KeyCode.Space))
        {
            isWaitingForInput = false;
            director.Play();              // 繼續播放 Timeline
        }
    }
}
