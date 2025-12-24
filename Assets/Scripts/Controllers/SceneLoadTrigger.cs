using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneLoadTrigger : MonoBehaviour
{
    [Header("Settings")]
    public string miniGameSceneName;
    public GameObject InteractiveUI;
    public Image fadeImage;
    public float fadeDuration = 1f;

    private bool isLoading = false;
    private bool playerInRange = false;

    void Start()
    {
        if (InteractiveUI != null) InteractiveUI.SetActive(false);

        // 遊戲剛開始執行時，確保黑幕是關閉的
        // 注意：OnEnable 會比 Start 早執行，所以這裡的設定很重要
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(false);
            fadeImage.color = new Color(0, 0, 0, 0);
        }
    }

    // --- 新增：當場景被重新喚醒時 (從 CG 回來) ---
    void OnEnable()
    {
        // 檢查：如果黑幕是開著的，而且顏色是黑的 (Alpha > 0.9)
        if (fadeImage != null && fadeImage.gameObject.activeSelf && fadeImage.color.a > 0.9f)
        {
            // 執行「由黑變透明」的動畫
            StartCoroutine(FadeInAnimation());
        }

        // 重置載入狀態，讓玩家以後還能再次觸發
        isLoading = false;
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

        // 3. ★ 呼叫紀錄功能，但排除自己 (gameObject)
        // 這樣做之後，其他物件都關了，唯獨這個 Script 還活著，可以繼續往下跑
        SceneSwapper.RecordAndHideMainScene(gameObject);

        // 4. 設定新場景為 Active
        Scene miniGameScene = SceneManager.GetSceneByName(miniGameSceneName);
        if (miniGameScene.IsValid())
        {
            SceneManager.SetActiveScene(miniGameScene);
        }

        // 5. ★ 任務完成，安心上路
        // 這行執行後，這個物件會變成 Inactive，Coroutine 會停止。
        // 但因為 SceneSwapper 已經紀錄了我們的 ID，
        // 所以等一下呼叫 RestoreMainScene 時，我們又會被復活，觸發 OnEnable！
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
}