using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Tree : MonoBehaviour
{
    [Header("Settings")]
    public string miniGameSceneName;
    public float fadeDuration = 1f;

    private bool isLoading = false;
    private bool playerInRange = false;
    public GameObject InteractiveUI;
    public Image fadeImage;
    private TreeManager manager;
    [HideInInspector] public Transform currentPoint; // 記錄目前所在的點位

    // 接收 Manager 傳來的初始設定
    public void Setup(TreeManager mgr, Transform point, Image fade)
    {
        fadeImage = fade;

        if (InteractiveUI != null) InteractiveUI.SetActive(false);

        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(false);
            fadeImage.color = new Color(0, 0, 0, 0);
        }

        manager = mgr;
        currentPoint = point;
    }
    public void Update()
    {
        if (playerInRange && !isLoading && Input.GetKeyDown(KeyCode.E))
        {
            isLoading = true;
            InteractiveUI.SetActive(false);
            GetComponent<Collider>().enabled = false; // 防止重複觸發
            StartCoroutine(SwitchToMiniGame());
        }
    }

    // --- 修改：將變黑與變亮拆成兩個獨立的 Coroutine ---

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

    // 2. 變透明 (Fade In to Clear) - 用於剛回來時
    private IEnumerator FadeInAnimation()
    {
        if (fadeImage != null)
        {
            // 設定為全黑
            fadeImage.color = new Color(0, 0, 0, 1);
            fadeImage.gameObject.SetActive(true);

            float elapsed = 0f;
            while (elapsed < 2F)
            {
                elapsed += Time.deltaTime;
                // 這裡是 1 減去進度，讓 Alpha 從 1 變 0
                float alpha = 1f - Mathf.Clamp01(elapsed / 2F);
                fadeImage.color = new Color(0, 0, 0, alpha);
                yield return null;
            }

            // 最後確保完全透明並關閉物件 (節省效能)
            fadeImage.color = new Color(0, 0, 0, 0);
            fadeImage.gameObject.SetActive(false);
        }
    }
    // --- 新增：給 Timeline 呼叫的專用入口 ---
    public void TriggerTransitionFromTimeline()
    {
        // 防止重複觸發
        if (isLoading) return;

        isLoading = true;

        // 如果 UI 剛好開著，要強制關閉
        if (InteractiveUI != null) InteractiveUI.SetActive(false);

        // 為了保險，把 Collider 關掉，避免 Timeline 跑到一半玩家誤觸
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // 啟動原本的轉場邏輯
        StartCoroutine(SwitchToMiniGame());
    }
    private IEnumerator SwitchToMiniGame()
    {
        // 1. 先變黑
        yield return StartCoroutine(FadeOutAnimation());

        // 2. 載入場景
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(miniGameSceneName, LoadSceneMode.Additive);
        while (!loadOp.isDone)
        {
            yield return null;
        }

        // --- 加入 Debug 來抓出問題 ---
        if (manager != null)
        {
            Debug.Log($"<color=yellow>[移動測試]</color> {gameObject.name} 準備移動。原座標: {transform.position}");
            manager.RelocateObject(this);
            Debug.Log($"<color=green>[移動測試]</color> {gameObject.name} 執行移動完畢。新座標: {transform.position}");
        }
        else
        {
            Debug.LogError($"<color=red>[嚴重錯誤]</color> {gameObject.name} 的 manager 是空的！請刪除場景上手動擺放的樹，讓 TreeManager 自動生成它！");
        }
        // -----------------------------

        // 3. 呼叫紀錄功能
        SceneSwapper.RecordAndHideMainScene(gameObject);

        // 4. 設定新場景為 Active
        Scene miniGameScene = SceneManager.GetSceneByName(miniGameSceneName);
        if (miniGameScene.IsValid())
        {
            SceneManager.SetActiveScene(miniGameScene);
        }

        // 5. 任務完成，安心上路
        gameObject.SetActive(false);
    }

    // OnTriggerEnter/Exit 保持不變...
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            InteractiveUI.SetActive(true);
        }
    }
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            InteractiveUI.SetActive(false);
        }
    }
    void OnEnable()
    {
        // 檢查：如果黑幕是開著的，而且顏色是黑的 (Alpha > 0.9)
        if (fadeImage != null && fadeImage.gameObject.activeSelf && fadeImage.color.a > 0.9f)
        {
            // 執行「由黑變透明」的動畫
            StartCoroutine(FadeInAnimation());
        }

        // --- 修復：重置所有狀態，確保物件能被再次觸發 ---
        isLoading = false;
        playerInRange = false;

        // 重新啟用碰撞器，讓 OnTriggerEnter 能再次偵測到玩家
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = true;
        }
    }
}