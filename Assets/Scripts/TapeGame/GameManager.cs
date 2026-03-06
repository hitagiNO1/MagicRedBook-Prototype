using System.Collections;
using UnityEngine;
using UnityEngine.UI; // 為了使用 Image 和 Text
using UnityEngine.SceneManagement; // 為了場景卸載

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("UI 連接")]
    public Text scoreText;
    public Text timeText;
    public Text hpText;
    public GameObject winPanel;
    public GameObject losePanel;

    // --- 【整合】跳轉與淡出設定 (從 usercontrol 搬來的) ---
    [Header("場景跳轉設定")]
    public string mainSceneName;      // 要回去的主場景名稱
    public GameScene currentScene;    // 下一段要播的劇情
    public Image fadeImage;           // 黑色遮罩圖片
    public float fadeDuration = 1f;   // 淡出時間
    // ----------------------------------------------------

    [Header("遊戲規則設定")]
    public int maxHealth = 3;
    public float gameTime = 30f;

    // 內部狀態
    private int score = 0;
    private int currentHealth;
    private float currentTime;
    public bool isGameOver = false;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // 初始化
        currentHealth = maxHealth;
        currentTime = gameTime;
        score = 0;
        Time.timeScale = 1;

        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);

        // 確保淡出圖片一開始是關閉的
        if (fadeImage != null) fadeImage.gameObject.SetActive(false);

        UpdateUI();
    }

    void Update()
    {
        if (isGameOver) return;

        currentTime -= Time.deltaTime;

        if (timeText != null)
            timeText.text = "Time: " + currentTime.ToString("F0");

        if (currentTime <= 0) WinGame();
    }

    // --- 分數與血量邏輯 ---
    public void AddScore(int amount) { score += amount; UpdateUI(); }
    public void TakeDamage(int amount) { if (isGameOver) return; currentHealth -= amount; UpdateUI(); if (currentHealth <= 0) LoseGame(); }

    void UpdateUI()
    {
        if (scoreText != null) scoreText.text = "Score: " + score;
        if (hpText != null) hpText.text = "HP: " + currentHealth;
    }

    void WinGame()
    {
        isGameOver = true;
        Time.timeScale = 0;
        if (winPanel != null) winPanel.SetActive(true);
    }

    void LoseGame()
    {
        isGameOver = true;
        Time.timeScale = 0;
        if (winPanel != null) winPanel.SetActive(true);

        if (EquipmentManager.Instance != null)
        {
            EquipmentManager.Instance.AddEquipment("膠帶拳套");
            Debug.Log("躲避球小遊戲勝利！成功獲得：膠帶拳套");
        }
        else
        {
            Debug.LogWarning("找不到 EquipmentManager！請確認它在切換場景時沒有被銷毀。");
        }
    }

    // =========================================================
    //  【整合】場景跳轉功能
    //  現在 usercontrol 只要呼叫這個 function，不需要傳參數
    // =========================================================
    public void TriggerSceneTransition()
    {
        StartCoroutine(ProcessReturnRoutine());
    }

    private IEnumerator ProcessReturnRoutine()
    {
        // 1. 執行變黑動畫 (這部分沒問題)
        yield return StartCoroutine(FadeOutAnimation());

        // 2. 等待緩衝 -- 【請修改這行】 --
        // 改用 WaitForSecondsRealtime，這樣就算 Time.timeScale 為 0 也能繼續執行
        yield return new WaitForSecondsRealtime(0.5f);

        // 3. 恢復主場景
        if (!string.IsNullOrEmpty(mainSceneName))
        {
            // 記得確保這裡的 SceneSwapper 有處理 Time.timeScale = 1 的邏輯
            // 如果沒有，建議在這裡先恢復時間：
            Time.timeScale = 1;

            SceneSwapper.RestoreMainScene(mainSceneName);
        }
        else
        {
            Debug.LogError("GameManager 未設定 Main Scene Name！");
        }

        // 4. 播放劇情 (使用此腳本自己的變數 currentScene)
        if (GameController.Instance != null && currentScene != null)
        {
            GameController.Instance.PlayScene(currentScene as StoryScene);
        }

        yield return null;

        // 5. 卸載目前的小遊戲場景
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
                // 使用 unscaledDeltaTime 以便在 TimeScale=0 時仍能運作
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Clamp01(elapsed / fadeDuration);
                fadeImage.color = new Color(0, 0, 0, alpha);
                yield return null;
            }
            fadeImage.color = new Color(0, 0, 0, 1); // 確保全黑
        }
    }
}