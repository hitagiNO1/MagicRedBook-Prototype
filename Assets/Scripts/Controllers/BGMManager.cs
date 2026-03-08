using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance;

    public enum BGMTriggerType
    {
        ByGameState,
        ByStoryScene
    }

    [System.Serializable]
    public class BGMEntry
    {
        public BGMTriggerType triggerType;
        public AudioClip clip;
        [Header("如果類型是 ByGameState 才需要填")]
        public GameState state;
        [Header("如果類型是 ByStoryScene 才需要填")]
        public StoryScene storyScene;

    }

    [Header("⚠️ 請依『劇情發生先後順序』由上往下排列")]
    public List<BGMEntry> bgmList;
    public float fadeDuration = 1.0f;
    public Slider volumeSlider;

    private Dictionary<GameState, AudioClip> stateBgmDict;

    private AudioSource audioSource;
    private GameState lastState;

    // 記錄當前區域常駐音樂
    private AudioClip currentAreaBGM;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
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

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.volume = 0.35f;

        stateBgmDict = new Dictionary<GameState, AudioClip>();

        foreach (var entry in bgmList)
        {
            if (entry.triggerType == BGMTriggerType.ByGameState)
            {
                if (!stateBgmDict.ContainsKey(entry.state))
                    stateBgmDict.Add(entry.state, entry.clip);
            }
        }

        // 初始化預設的遊玩音樂
        if (stateBgmDict.ContainsKey(GameState.Playing))
        {
            currentAreaBGM = stateBgmDict[GameState.Playing];
        }

        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.AddListener(SetVolume);
            volumeSlider.value = audioSource.volume;
        }
    }

    private void Update()
    {
        // ==========================================
        // 1. 根據「已經播放過的劇情進度」決定當前區域音樂
        // ==========================================
        if (GameController.Instance != null)
        {
            AudioClip latestUnlockedBGM = null;

            // 先給予最基礎的預設遊玩音樂
            if (stateBgmDict.ContainsKey(GameState.Playing))
            {
                latestUnlockedBGM = stateBgmDict[GameState.Playing];
            }

            // 遍歷所有設定，尋找玩家已經觸發過的劇情
            // (只要越後面的劇情有被播放過，就會蓋掉前面設定的音樂)
            foreach (var entry in bgmList)
            {
                if (entry.triggerType == BGMTriggerType.ByStoryScene && entry.storyScene != null)
                {
                    // 🌟 依照你的邏輯：比對是否已經在 ScenePlayedState 字典中
                    if (GameController.Instance.ScenePlayedState.ContainsKey(entry.storyScene))
                    {
                        latestUnlockedBGM = entry.clip;
                    }
                }
            }

            // 如果發現「進度推進了」，且對應的音樂與目前播放的不同，就準備切換
            if (latestUnlockedBGM != null && currentAreaBGM != latestUnlockedBGM)
            {
                currentAreaBGM = latestUnlockedBGM;

                // 如果當下是處於「遊玩」或「對話」狀態，就立刻切換這首新解鎖的 BGM
                GameState currentState = GameStateManager.Instance.currentState;
                if (currentState == GameState.Playing || currentState == GameState.Story || currentState == GameState.StoryNotMove)
                {
                    SwitchBGM(currentAreaBGM);
                }
            }
        }

        // ==========================================
        // 2. 處理遊戲狀態改變 (例如打開暫停選單)
        // ==========================================
        GameState currentGameState = GameStateManager.Instance.currentState;
        if (currentGameState != lastState)
        {
            bool isGameplayState = (currentGameState == GameState.Playing ||
                                    currentGameState == GameState.Story ||
                                    currentGameState == GameState.StoryNotMove);

            if (isGameplayState && currentAreaBGM != null)
            {
                // 回到遊玩畫面時，播放當下進度專屬的區域音樂
                SwitchBGM(currentAreaBGM);
            }
            else if (stateBgmDict.ContainsKey(currentGameState))
            {
                // 若是特殊狀態(如 Menu)，則播放對應的音樂
                SwitchBGM(stateBgmDict[currentGameState]);
            }

            lastState = currentGameState;
        }
    }

    // --- 以下為音效控制與淡入淡出 ---
    private void SwitchBGM(AudioClip newClip)
    {
        if (audioSource.clip == newClip) return;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeToNewClip(newClip));
    }

    private IEnumerator FadeToNewClip(AudioClip newClip)
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
        audioSource.clip = newClip;
        audioSource.Play();

        t = 0;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0, startVolume, t / fadeDuration);
            yield return null;
        }

        audioSource.volume = startVolume;
    }

    public void StopBGM(bool fade = true)
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        if (fade)
            fadeCoroutine = StartCoroutine(FadeOutAndStop());
        else
            audioSource.Stop();
    }

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

    public void SetVolume(float volume)
    {
        audioSource.volume = Mathf.Clamp01(volume);
    }

    // ==========================================
    // 擴充功能：Timeline 播放時的音量閃避 (Audio Ducking)
    // ==========================================
    private float savedVolume = 0.35f; // 用來記住閃避前的原始音量
    private Coroutine volumeFadeCoroutine;

    // 呼叫此方法讓音樂變小聲
    public void DuckVolume()
    {
        float targetVolume = 0.1f;
        float duration = 0.5f;
        // 記住當前的音量（可能是玩家用滑桿調過的）
        savedVolume = audioSource.volume;

        if (volumeFadeCoroutine != null) StopCoroutine(volumeFadeCoroutine);
        volumeFadeCoroutine = StartCoroutine(FadeVolumeOnly(targetVolume));
    }

    // 呼叫此方法恢復原本的音量
    public void RestoreVolume(float duration = 0.5f)
    {
        // 如果有綁定滑桿，就以滑桿的值為準，否則用剛剛記住的音量
        float target = volumeSlider != null ? volumeSlider.value : savedVolume;

        if (volumeFadeCoroutine != null) StopCoroutine(volumeFadeCoroutine);
        volumeFadeCoroutine = StartCoroutine(FadeVolumeOnly(target));
    }

    // 只改變音量，不改變音樂片段的協程
    private IEnumerator FadeVolumeOnly(float targetVolume)
    {
        float t = 0;
        float startVolume = audioSource.volume;
        float duration = 0.5f;
        while (t < duration)
        {
            t += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, t / duration);
            yield return null;
        }

        audioSource.volume = targetVolume;
    }
}