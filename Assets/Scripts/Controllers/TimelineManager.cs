using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class TimelineManager : MonoBehaviour
{
    // 單例模式：讓其他腳本能隨時取得 TimelineManager 實例
    public static TimelineManager Instance { get; private set; }
    public PlayableDirector director;

    [Header("【強制切換DCAT BGM 設定】")]
    public AudioClip newBGMClip;


    [Header("Signal控制")]
    public ParticleSystem Magic1;
    public ParticleSystem Magic2;
    public ParticleSystem Magic3;
    public ParticleSystem BookMagic;
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
            // 2. 清除 Director 上綁定的動畫資產 (PlayableAsset)
            dir.playableAsset = null;
        }
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
    public void PlayCG(PlayableAsset asset)
    {

        if (asset == null || director == null)
        {
            Debug.LogWarning("PlayableAsset 或 Director 為 null");
            return;
        }

        Debug.Log("開始播放 Timeline：" + asset.name);
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
    // 🌟 這是給 Timeline Signal 呼叫的「降低音量」方法 (無參數)
    public void Signal_DuckBGM()
    {
        if (BGMManager.Instance != null)
        {
            BGMManager.Instance.DuckVolume();
        }
    }

    // 🌟 這是給 Timeline Signal 呼叫的「恢復音量」方法 (無參數)
    public void Signal_RestoreBGM()
    {
        if (BGMManager.Instance != null)
        {
            BGMManager.Instance.RestoreVolume();
        }
    }
    // --- 🌟 新增：給 Timeline Signal 呼叫的「切換音樂」方法 ---
    public void Signal_ChangeBGM()
    {
        if (BGMManager.Instance != null && newBGMClip != null)
        {
            BGMManager.Instance.ForceChangeBGM(newBGMClip);
        }
    }
    private void Update()
    {
        // 判斷：正在等待輸入 且 (按下了空白鍵 或 按下了滑鼠左鍵)
        if (isWaitingForInput && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)))
        {
            isWaitingForInput = false;
            director.Play(); // 繼續播放 Timeline
        }
    }

    /// <summary>
    /// 播放粒子效果（Timeline Signal 呼叫）
    /// </summary>
    public void PlayMagic1()
    {
        if (Magic1 != null)
        {
            Magic1.Play();
        }
    }

    public void PlayMagic2()
    {
        if (Magic2 != null)
        {
            Magic2.Play();
        }
    }
    public void PlayMagic3()
    {
        if (Magic3 != null)
        {
            Magic3.Play();
        }
    }
    public void PlayBookMagic()
    {
        if (BookMagic != null)
        {
            BookMagic.Play();
        }
    }
    public void ClearBookMagic()
    {
        if (BookMagic != null)
        {
            BookMagic.Clear();
        }
    }
    /// <summary>
    /// 停止粒子效果（Timeline Signal 呼叫）
    /// </summary>
    public void StopParticle()
    {
        if (Magic1 != null)
        {
            Magic1.Stop();
        }
    }

    /// <summary>
    /// 粒子完全清除（可選：有時候需要瞬間消掉）
    /// </summary>
    public void ClearParticle()
    {
        if (Magic1 != null)
        {
            Magic1.Clear();
        }
    }

    public void SetGameStateManga()
    {
        GameStateManager.Instance.SetState(GameState.Mangaing);
    }
    public void SetGameStatePlay()
    {
        GameStateManager.Instance.SetState(GameState.Playing);
    }
    public void SetGameStateBattle()
    {
        GameStateManager.Instance.SetState(GameState.Battle);
    }

    public void SwitchScene()
    {
        SceneManager.LoadScene("CH1");
    }
    public void GoCH2()
    {
        SceneManager.LoadScene("CH2");
    }
    public void GoCG6()
    {
        SceneManager.LoadScene("CG6");
    }

    public void GoTapeGame()
    {
        SceneManager.LoadScene("Game_Tape");
    }
    public void GoBOXGame()
    {
        SceneManager.LoadScene("Game_BOX");
    }
    public  void Page1Pick()
    {
        PageCGUI.Instance.AddPage(0);
    }
    public void Page2Pick()
    {
        PageCGUI.Instance.AddPage(1);
    }
    public void Page3Pick()
    {
        PageCGUI.Instance.AddPage(2);
    }
    public void Page4Pick()
    {
        PageCGUI.Instance.AddPage(3);
    }
}
