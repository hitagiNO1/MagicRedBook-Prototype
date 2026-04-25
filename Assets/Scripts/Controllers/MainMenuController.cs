using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("場景設定")]
    public string gameSceneName = "GameScene"; // 第一關或初始劇情的場景名稱
    public string saveKey = "HasBeatenGame";   // 儲存進度的 Key

    [Header("UI 漸變元件")]
    public Image screenFaderIn;
    public Image screenFaderOut;
    public Image targetObject;

    [Header("音效設定")]
    public AudioSource bgmSource;
    public float targetVolume = 1f;

    [Header("時間設定")]
    public float fadeDuration = 1.0f;
    public float delayTime = 1.0f;

    private bool isTransitioning = false;

    private void Awake()
    {
        // 【新增功能】在場景還沒開始跑 Start 之前就先判斷
        // 如果 PlayerPrefs 裡的數值是 0 (代表未破關)，就直接載入遊戲
        if (PlayerPrefs.GetInt(saveKey, 0) == 0)
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }

    private void Start()
    {
        // 只有在已破關 (HasBeatenGame == 1) 的情況下，才會執行到這裡
        InitUI();

        if (bgmSource != null)
        {
            bgmSource.volume = 0f;
            bgmSource.Play();
        }

        StartCoroutine(EnterSceneSequence());
    }

    private void InitUI()
    {
        if (screenFaderIn != null) { SetImageAlpha(screenFaderIn, 1f); screenFaderIn.raycastTarget = true; }
        if (screenFaderOut != null) { SetImageAlpha(screenFaderOut, 0f); screenFaderOut.raycastTarget = false; }
        if (targetObject != null) SetImageAlpha(targetObject, 0f);
    }

    private IEnumerator EnterSceneSequence()
    {
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / fadeDuration;
            if (screenFaderIn != null) SetImageAlpha(screenFaderIn, 1f - progress);
            if (bgmSource != null) bgmSource.volume = progress * targetVolume;
            yield return null;
        }
        if (screenFaderIn != null) { SetImageAlpha(screenFaderIn, 0f); screenFaderIn.raycastTarget = false; }

        yield return new WaitForSeconds(delayTime);

        timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            if (targetObject != null) SetImageAlpha(targetObject, timer / fadeDuration);
            yield return null;
        }
    }

    // --- 外部呼叫的功能 ---

    // 【重要】請在你的遊戲最後一關、劇情結束時，呼叫這個方法
    public void MarkGameAsFinished()
    {
        PlayerPrefs.SetInt(saveKey, 1); // 設為 1 代表已破關
        PlayerPrefs.Save();             // 強制儲存到硬碟
    }

    // 用於開發測試：重置進度
    public void ResetProgress()
    {
        PlayerPrefs.SetInt(saveKey, 0);
        PlayerPrefs.Save();
        Debug.Log("進度已重置");
    }

    public void StartGame()
    {
        if (isTransitioning) return;
        StartCoroutine(ExitSceneSequence(false));
    }

    public void QuitGame()
    {
        if (isTransitioning) return;
        StartCoroutine(ExitSceneSequence(true));
    }

    private IEnumerator ExitSceneSequence(bool isQuitting)
    {
        isTransitioning = true;
        float timer = 0f;
        if (screenFaderOut != null) screenFaderOut.raycastTarget = true;
        float startVolume = bgmSource != null ? bgmSource.volume : 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / fadeDuration;
            if (screenFaderOut != null) SetImageAlpha(screenFaderOut, progress);
            if (bgmSource != null) bgmSource.volume = Mathf.Lerp(startVolume, 0f, progress);
            yield return null;
        }

        if (isQuitting) Application.Quit();
        else SceneManager.LoadScene(gameSceneName);
    }

    private void SetImageAlpha(Image img, float alpha)
    {
        if (img == null) return;
        Color c = img.color;
        c.a = alpha;
        img.color = c;
    }
}