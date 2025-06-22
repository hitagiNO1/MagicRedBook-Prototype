using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance;

    [System.Serializable]
    public class BGMEntry
    {
        public GameState state;       // 對應的遊戲狀態
        public AudioClip clip;        // 該狀態應播放的音樂
    }

    public List<BGMEntry> bgmList;     // 在 Inspector 指定狀態與音樂對應
    public float fadeDuration = 1.0f;  // 淡入淡出時間（秒）
    public Slider volumeSlider;        // 音量滑桿（在 Inspector 中綁定）

    private Dictionary<GameState, AudioClip> bgmDict;
    private AudioSource audioSource;
    private GameState lastState;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        // 確保單例
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 初始化 AudioSource
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.volume = 0.35f;

        // 將 list 轉換為 dictionary 方便查找
        bgmDict = new Dictionary<GameState, AudioClip>();
        foreach (var entry in bgmList)
        {
            if (!bgmDict.ContainsKey(entry.state))
            {
                bgmDict.Add(entry.state, entry.clip);
            }
        }

        // 綁定滑桿事件（如果滑桿存在）
        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.AddListener(SetVolume);
            volumeSlider.value = audioSource.volume; // 初始化滑桿顯示值
        }
    }

    private void Update()
    {
        // 監聽遊戲狀態變化，自動切換背景音樂
        GameState currentState = GameStateManager.Instance.currentState;
        if (currentState != lastState)
        {
            SwitchBGM(currentState);
            lastState = currentState;
        }
    }

    // 切換到對應狀態的音樂（若不同）
    private void SwitchBGM(GameState state)
    {
        if (!bgmDict.ContainsKey(state)) return;

        AudioClip newClip = bgmDict[state];
        if (audioSource.clip == newClip) return; // 同曲目不重播

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeToNewClip(newClip));
    }

    // 淡出 → 播放新曲 → 淡入
    private IEnumerator FadeToNewClip(AudioClip newClip)
    {
        float t = 0;
        float startVolume = audioSource.volume;

        // 淡出
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0, t / fadeDuration);
            yield return null;
        }

        audioSource.Stop();
        audioSource.clip = newClip;
        audioSource.Play();

        // 淡入
        t = 0;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0, startVolume, t / fadeDuration);
            yield return null;
        }

        audioSource.volume = startVolume;
    }

    // 供外部呼叫停止背景音樂，可選是否淡出
    public void StopBGM(bool fade = true)
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        if (fade)
        {
            fadeCoroutine = StartCoroutine(FadeOutAndStop());
        }
        else
        {
            audioSource.Stop();
        }
    }

    // 淡出後停止播放
    private IEnumerator FadeOutAndStop()
    {
        float t = 0;
        float startVolume = audioSource.volume;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0, t / fadeDuration);
            yield return null;
        }

        audioSource.Stop();
        audioSource.volume = startVolume;
    }

    // 設定背景音量（從滑桿或外部呼叫）
    public void SetVolume(float volume)
    {
        audioSource.volume = Mathf.Clamp01(volume);
    }

    // 給 UI 的滑桿用（Slider 的 onValueChanged 綁這個）
    public void SetVolumeFromSlider()
    {
        if (volumeSlider != null)
            SetVolume(volumeSlider.value);
    }
}
