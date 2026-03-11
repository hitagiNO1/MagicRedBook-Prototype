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
    public float Volume = 0.35f;
    private Dictionary<GameState, AudioClip> stateBgmDict;

    private AudioSource audioSource;
    private GameState lastState;
    // ==========================================
    // 新增：紀錄是否被 Timeline 強制接管了 BGM
    // ==========================================
    private bool isTimelineOverride = false;
    // 記錄當前區域常駐音樂
    private AudioClip currentAreaBGM;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.volume = Volume;

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
    public void ForceChangeBGM(AudioClip newClip)
    {
        if (newClip == null || currentAreaBGM == newClip) return;

        currentAreaBGM = newClip;
        isTimelineOverride = true; //告訴系統不要再管劇情清單了
        SwitchBGM(newClip);
    }
    private void Update()
    {
        // ==========================================
        // 1. 根據「已經播放過的劇情進度」決定當前區域音樂
        // ==========================================
        if (GameController.Instance != null)
        {
            AudioClip latestUnlockedBGM = null;

            if (stateBgmDict.ContainsKey(GameState.Playing))
            {
                latestUnlockedBGM = stateBgmDict[GameState.Playing];
            }

            foreach (var entry in bgmList)
            {
                if (entry.triggerType == BGMTriggerType.ByStoryScene && entry.storyScene != null)
                {
                    if (GameController.Instance.ScenePlayedState.ContainsKey(entry.storyScene))
                    {
                        latestUnlockedBGM = entry.clip;
                    }
                }
            }

            // 🌟 關鍵攔截點 1：Timeline 介入時鎖死劇情音樂
            if (isTimelineOverride)
            {
                latestUnlockedBGM = currentAreaBGM;
            }

            if (latestUnlockedBGM != null && currentAreaBGM != latestUnlockedBGM)
            {
                currentAreaBGM = latestUnlockedBGM;

                GameState currentState = GameStateManager.Instance.currentState;
                if (currentState == GameState.Playing || currentState == GameState.Story || currentState == GameState.StoryNotMove)
                {
                    SwitchBGM(currentAreaBGM);
                }
            }
        }

        // ==========================================
        // 2. 處理遊戲狀態改變 (修復戰鬥音樂蓋過去的 Bug)
        // ==========================================
        GameState currentGameState = GameStateManager.Instance.currentState;
        if (currentGameState != lastState)
        {
            // 🌟 關鍵攔截點 2：如果 Timeline 已經霸佔了 BGM，不管狀態怎麼切換，都強制鎖定
            if (isTimelineOverride)
            {
                SwitchBGM(currentAreaBGM);
            }
            else
            {
                // 如果 Timeline 沒有介入，才照常理判斷狀態
                bool isGameplayState = (currentGameState == GameState.Playing ||
                                        currentGameState == GameState.Story ||
                                        currentGameState == GameState.StoryNotMove);

                if (isGameplayState && currentAreaBGM != null)
                {
                    SwitchBGM(currentAreaBGM);
                }
                else if (stateBgmDict.ContainsKey(currentGameState))
                {
                    SwitchBGM(stateBgmDict[currentGameState]);
                }
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
    private Coroutine volumeFadeCoroutine;

    // 呼叫此方法讓音樂變小聲
    public void DuckVolume()
    {
        float targetVolume = 0.1f;
        float duration = 0.5f;
        // 記住當前的音量（可能是玩家用滑桿調過的）
        Volume = audioSource.volume;

        if (volumeFadeCoroutine != null) StopCoroutine(volumeFadeCoroutine);
        volumeFadeCoroutine = StartCoroutine(FadeVolumeOnly(targetVolume));
    }

    // 呼叫此方法恢復原本的音量
    public void RestoreVolume(float duration = 0.5f)
    {
        // 如果有綁定滑桿，就以滑桿的值為準，否則用剛剛記住的音量
        float target = volumeSlider != null ? volumeSlider.value : Volume;

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