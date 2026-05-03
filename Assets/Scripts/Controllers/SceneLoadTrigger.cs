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

    [Header("Audio Settings (Optional)")]
    public AudioSource bgmSource;        // 可選：要淡出的背景音樂
    public float audioFadeDuration = 1f; // 音樂淡出/淡入時間
    private float originalVolume;        // 紀錄最一開始的音量

    private bool isLoading = false;
    private bool playerInRange = false;
    private bool isFirstTime = true; // 紀錄是否為第一次啟動
    void Awake()
    {
        // 在 Awake 先記錄好初始音量，避免 OnEnable 執行時抓到 0
        if (bgmSource != null)
        {
            originalVolume = bgmSource.volume;
        }
    }

    void Start()
    {
        if (InteractiveUI != null) InteractiveUI.SetActive(false);

        // 遊戲剛開始執行時，確保黑幕是關閉的
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(false);
            fadeImage.color = new Color(0, 0, 0, 0);
        }
    }

    // --- 修改：當場景被重新喚醒時 (從 CG 回來) ---
    void OnEnable()
    {
        // ★ 如果是遊戲剛開始的第一次載入，把開關關掉，然後直接跳出，不播動畫！
        if (isFirstTime)
        {
            isFirstTime = false;
            return;
        }

        // ---------------- 以下是從別的場景「切回來」時才會執行的邏輯 ----------------

        // 檢查：如果黑幕是開著的，而且顏色是黑的 (Alpha > 0.9)
        if (fadeImage != null && fadeImage.gameObject.activeSelf && fadeImage.color.a > 0.9f)
        {
            // 執行「由黑變透明」的動畫
            StartCoroutine(FadeInAnimation());
        }

        // 音樂淡入：如果我們有設定音樂，回來時把它漸進式播回來
        if (bgmSource != null && !isLoading)
        {
            StartCoroutine(FadeInAudio());
        }

        playerInRange = false;
        isLoading = false; // 確保狀態重置
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

    // 1. 變黑 (Fade Out to Black)
    private IEnumerator FadeOutAnimation()
    {
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            fadeImage.color = new Color(0, 0, 0, 0);

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Clamp01(elapsed / fadeDuration);
                fadeImage.color = new Color(0, 0, 0, alpha);
                yield return null;
            }
            fadeImage.color = new Color(0, 0, 0, 1);
        }
    }

    // 2. 變透明 (Fade In to Clear)
    private IEnumerator FadeInAnimation()
    {
        if (fadeImage != null)
        {
            fadeImage.color = new Color(0, 0, 0, 1);
            fadeImage.gameObject.SetActive(true);

            float elapsed = 0f;
            while (elapsed < 2F)
            {
                elapsed += Time.deltaTime;
                float alpha = 1f - Mathf.Clamp01(elapsed / 2F);
                fadeImage.color = new Color(0, 0, 0, alpha);
                yield return null;
            }

            fadeImage.color = new Color(0, 0, 0, 0);
            fadeImage.gameObject.SetActive(false);
        }
    }

    // --- ★ 新增：音樂淡出 (Fade Out Audio) ---
    private IEnumerator FadeOutAudio()
    {
        if (bgmSource != null && bgmSource.isPlaying)
        {
            float startVolume = bgmSource.volume;
            float elapsed = 0f;

            while (elapsed < audioFadeDuration)
            {
                elapsed += Time.deltaTime;
                // 將音量平滑降到 0
                bgmSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / audioFadeDuration);
                yield return null;
            }

            bgmSource.volume = 0f;
            bgmSource.Pause(); // 暫停播放，這樣回來時音樂不會從頭開始
        }
    }

    // --- ★ 新增：音樂淡入 (Fade In Audio) ---
    private IEnumerator FadeInAudio()
    {
        if (bgmSource != null)
        {
            if (!bgmSource.isPlaying) bgmSource.Play(); // 如果暫停了就繼續播

            float startVolume = bgmSource.volume;
            float elapsed = 0f;

            while (elapsed < audioFadeDuration)
            {
                elapsed += Time.deltaTime;
                // 將音量平滑恢復到原始音量
                bgmSource.volume = Mathf.Lerp(startVolume, originalVolume, elapsed / audioFadeDuration);
                yield return null;
            }

            bgmSource.volume = originalVolume;
        }
    }

    public void TriggerTransitionFromTimeline()
    {
        if (isLoading) return;
        isLoading = true;

        if (InteractiveUI != null) InteractiveUI.SetActive(false);

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        StartCoroutine(SwitchToMiniGame());
    }

    private IEnumerator SwitchToMiniGame()
    {
        // ★ 1. 同時啟動音樂淡出與黑幕淡出
        // 不加 yield return，讓音樂淡出在背景執行，這樣轉場會跟畫面完美同步！
        if (bgmSource != null)
        {
            StartCoroutine(FadeOutAudio());
        }

        // 等待畫面全黑
        yield return StartCoroutine(FadeOutAnimation());

        // 2. 載入場景
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(miniGameSceneName, LoadSceneMode.Additive);
        while (!loadOp.isDone)
        {
            yield return null;
        }

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