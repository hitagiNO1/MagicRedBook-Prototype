using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class SceneManagerController : MonoBehaviour
{
    public static SceneManagerController Instance { get; private set; }
    [SerializeField] private Image fadeImage; // 全螢幕黑圖
    [SerializeField] private float fadeDuration = 1f; // 淡入淡出時間

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (fadeImage != null)
        {
            // 開始時保證黑圖是透明
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
        }
    }

    // 同步切換場景 (含淡入淡出)
    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    //非同步切換場景 (含淡入淡出)
    public void LoadSceneAsync(string sceneName)
    {
        StartCoroutine(LoadSceneAsyncRoutine(sceneName));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        yield return StartCoroutine(FadeIn()); // 黑幕蓋上
        SceneManager.LoadScene(sceneName);
        //yield return StartCoroutine(FadeOut()); // 黑幕淡出
    }

    private IEnumerator LoadSceneAsyncRoutine(string sceneName)
    {
        yield return StartCoroutine(FadeIn()); // 黑幕蓋上

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        while (op.progress < 0.9f)
        {
            Debug.Log($"Loading progress: {op.progress * 100}%");
            yield return null;
        }

        op.allowSceneActivation = true;
        yield return null; // 確保場景已完成切換

        yield return StartCoroutine(FadeOut()); // 黑幕淡出
    }

    // 淡入 (黑幕出現)
    private IEnumerator FadeIn()
    {
        if (fadeImage == null) yield break;
        fadeImage.enabled = true;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Clamp01(t / fadeDuration);
            Color c = fadeImage.color;
            c.a = alpha;
            fadeImage.color = c;
            yield return null;
        }
    }

    // 淡出 (黑幕消失)
    private IEnumerator FadeOut()
    {
        if (fadeImage == null) yield break;

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float alpha = 1f - Mathf.Clamp01(t / fadeDuration);
            Color c = fadeImage.color;
            c.a = alpha;
            fadeImage.color = c;
            yield return null;
        }
    }
}
