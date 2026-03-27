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
    public const int requiredClicks = 10;
    private bool gameWon = false;

    [Header("UI - Game Info")]
    public TextMeshProUGUI timerText;  // 新增：顯示剩餘時間的 UI
    public TextMeshProUGUI scoreText;  // 新增：顯示擊中數量的 UI

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

        // 初始化分數 UI
        UpdateScoreUI();
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

        // ---- 更新時間 UI ----
        if (timerText != null)
        {
            // 使用 Mathf.Max 確保時間不會顯示負數，並用 CeilToInt 無條件進位顯示整數秒
            timerText.text = $"時間: {Mathf.CeilToInt(Mathf.Max(0, timer))}";
        }
        // --------------------

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

            // ---- 更新分數 UI ----
            UpdateScoreUI();
            // --------------------

            Debug.Log($"點到老鼠！目前 {mouseClickCount}/{requiredClicks}");

            if (mouseClickCount >= requiredClicks)
            {
                gameWon = true;
                gameWin();

                // ================= 新增在這裡：發放盔甲 =================
                if (EquipmentManager.Instance != null)
                {
                    EquipmentManager.Instance.AddEquipment("穿戴式紙箱盔甲");
                    Debug.Log("小遊戲勝利！成功獲得：穿戴式紙箱盔甲");
                }
                else
                {
                    Debug.LogWarning("找不到 EquipmentManager！請確保它在切換場景時沒有被銷毀。");
                }
                // ========================================================

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
            gameflow.ShowResultLOSS();
        }

        // 通知 mouseborn 做重置
        if (mouseborn != null)
            mouseborn.OnObjectClicked(clickedState.Hole);
    }

    // 獨立出一個更新分數 UI 的方法，方便重複呼叫
    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"分數: {mouseClickCount} / {requiredClicks}";
        }
    }

    //=====================================================================
    //  結果文字
    //=====================================================================
    public void getC4() => resault.text = "這趟旅途不能沒有布萊克\n再試試吧！";
    public void gamelose() => resault.text = "這趟旅途不能沒有布萊克\n再試試吧！";
    public void gameWin() => resault.text = "成功捕獲布萊克！";

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

        // ---- 重新開始時重置 UI ----
        UpdateScoreUI();
        if (timerText != null) timerText.text = $"時間: {timer}";
        // -------------------------

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

    public void OnPointerEnter(PointerEventData eventData) { }
    public void OnPointerExit(PointerEventData eventData) { }
}