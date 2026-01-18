using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;   // 必須
using UnityEngine.UI;

public class usercontrol : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler   // 用來偵測滑鼠進出（可選）
{
    private List<HoleState> holeStates = new List<HoleState>();

    public bool gameover = false;
    public float timer = 60f;
    private int mouseClickCount = 0;
    private const int requiredClicks = 15;
    private bool gameWon = false;

    [Header("UI")]
    public TextMeshProUGUI resault;
    public boxgameflow gameflow;
    public GameObject backbutton;


    [Header("Return Settings")]
    public string mainSceneName;
    public GameScene currentScene;
    public Image fadeImage;
    public float fadeDuration = 1f;

    private mouseborn mouseborn;

    //=====================================================================
    //  初始化
    //=====================================================================
    void Start()
    {
        mouseborn = FindObjectOfType<mouseborn>();
        if (mouseborn == null)
        {
            Debug.LogError("未找到 mouseborn 組件！");
            enabled = false;
            return;
        }

        // 註冊事件
        mouseborn.OnHoleSpawned += RegisterHoleState;
        mouseborn.OnHoleRemoved += UnregisterHoleState;

        CleanUpHoleList();
    }

    void OnDestroy()
    {
        if (mouseborn != null)
        {
            mouseborn.OnHoleSpawned -= RegisterHoleState;
            mouseborn.OnHoleRemoved -= UnregisterHoleState;
        }
    }

    //=====================================================================
    //  遊戲主迴圈
    //=====================================================================
    void Update()
    {
        if (gameover || gameWon) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            Debug.Log("時間到！未達標，遊戲結束！");
            gameover = true;
            gamelose();
            gameflow.ShowResult();
        }
    }

    //=====================================================================
    //  事件回呼：新地洞生成 / 移除
    //=====================================================================
    private void RegisterHoleState(HoleState state)
    {
        if (state != null && !holeStates.Contains(state))
            holeStates.Add(state);
    }

    private void UnregisterHoleState(HoleState state)
    {
        holeStates.Remove(state);
    }

    public void CleanUpHoleList()
    {
        holeStates.RemoveAll(h => h == null || h.gameObject == null);
    }

    public bool IsGameActive() => !gameover && !gameWon;

    //=====================================================================
    //  UI 點擊處理（由 HoleState 呼叫）
    //=====================================================================
    public void OnHoleClicked(HoleState clickedState)
    {
        if (gameover || gameWon) return;

        // 直接從 list 移除
        holeStates.Remove(clickedState);

        if (clickedState.IsMouse)
        {
            // ==== 點到老鼠 ====
            mouseClickCount++;
            Debug.Log($"點到老鼠！目前 {mouseClickCount}/{requiredClicks}");

            if (mouseClickCount >= requiredClicks)
            {
                gameWon = true;
                gameWin();
                backbutton.SetActive(true);
                gameflow.ShowResult();
            }
        }
        else
        {
            // ==== 點到 C4 ====
            Debug.Log("點到 C4，遊戲結束！");
            gameover = true;
            getC4();
            gameflow.ShowResult();
        }

        // 通知 mouseborn 做重置
        if (mouseborn != null)
            mouseborn.OnObjectClicked(clickedState.Hole);
    }

    //=====================================================================
    //  結果文字
    //=====================================================================
    public void getC4() => resault.text = "你點錯了!";
    public void gamelose() => resault.text = "沒在時間內達成目標!";
    public void gameWin() => resault.text = "達成目標!";

    //=====================================================================
    //  重新開始 / 返回
    //=====================================================================
    public void RestartGame()
    {
        gameover = false;
        gameWon = false;
        mouseClickCount = 0;
        timer = 60f;
        holeStates.Clear();
        backbutton.SetActive(false);

        if (mouseborn != null)
            mouseborn.Restart();
    }

    public void EndMiniGame()
    {
        StartCoroutine(ReturnToMainScene());
    }

    private IEnumerator ReturnToMainScene()
    {
        yield return StartCoroutine(FadeOutAnimation());
        yield return new WaitForSeconds(0.5f);
        SceneSwapper.RestoreMainScene(mainSceneName);
        GameController.Instance.PlayScene(currentScene as StoryScene);
        yield return null;
        yield return SceneManager.UnloadSceneAsync(gameObject.scene);
    }
    // 1. 變黑 (Fade Out to Black)
    private IEnumerator FadeOutAnimation()
    {
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            fadeImage.color = new Color(0, 0, 0, 0); // 從透明開始

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Clamp01(elapsed / fadeDuration);
                fadeImage.color = new Color(0, 0, 0, alpha);
                yield return null;
            }
            fadeImage.color = new Color(0, 0, 0, 1); // 確保全黑
        }
    }

    //=====================================================================
    //  可選：滑鼠進出時切換 Hover 圖片（若要保留 hover 效果）
    //=====================================================================
    public void OnPointerEnter(PointerEventData eventData)
    {
        // 這裡可以寫 hover 特效（例如放大），目前留空
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 這裡可以寫離開特效
    }
}