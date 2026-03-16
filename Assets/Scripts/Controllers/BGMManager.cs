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

    public float fadeDuration = 2.5f;

    public Slider volumeSlider;
    public float Volume = 0.1f;

    [Header("最一開始啟動遊戲時的預設音樂")]
    public AudioClip Firstclip;

    private Dictionary<GameState, AudioClip> stateBgmDict;
    private AudioSource audioSource;
    private GameState lastState;

    private HashSet<StoryScene> triggeredStories = new HashSet<StoryScene>();
    private bool isTimelineOverride = false;
    private AudioClip currentAreaBGM;
    private Coroutine fadeCoroutine;

    // === 戰鬥暫存變數 ===
    private bool isBattleOverride = false;
    private AudioClip preBattleClip;
    private AudioClip targetBGM;

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

        // 將清單中依據 GameState 觸發的音樂存入字典
        foreach (var entry in bgmList)
        {
            if (entry.triggerType == BGMTriggerType.ByGameState)
            {
                if (!stateBgmDict.ContainsKey(entry.state))
                    stateBgmDict.Add(entry.state, entry.clip);
            }
        }

        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.AddListener(SetVolume);
            volumeSlider.value = audioSource.volume;
        }
    }

    private void Start()
    {
        if (Firstclip != null)
        {
            currentAreaBGM = Firstclip;
            targetBGM = Firstclip;
            audioSource.clip = Firstclip;
            audioSource.Play();
        }
    }

    public void NotifyStoryStarted(StoryScene scene)
    {
        if (!triggeredStories.Contains(scene))
        {
            triggeredStories.Add(scene);
        }
    }

    public void ForceChangeBGM(AudioClip newClip)
    {
        if (newClip == null || currentAreaBGM == newClip) return;

        currentAreaBGM = newClip;
        isTimelineOverride = true;
        SwitchBGM(newClip);
    }

    private void Update()
    {
        // === 1. 背景處理劇情進度解鎖的區域音樂 ===
        if (GameController.Instance != null)
        {
            AudioClip latestUnlockedBGM = Firstclip;

            foreach (var entry in bgmList)
            {
                if (entry.triggerType == BGMTriggerType.ByStoryScene && entry.storyScene != null)
                {
                    if (GameController.Instance.ScenePlayedState.ContainsKey(entry.storyScene) ||
                        triggeredStories.Contains(entry.storyScene))
                    {
                        latestUnlockedBGM = entry.clip;
                    }
                }
            }

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
                    if (!isBattleOverride) SwitchBGM(currentAreaBGM);
                }
            }
        }

        // === 2. 核心：全自動監聽 GameState 的改變 ===
        GameState currentGameState = GameStateManager.Instance.currentState;
        if (currentGameState != lastState)
        {
            // 情況 A：剛進入戰鬥狀態
            if (currentGameState == GameState.Battle)
            {
                isBattleOverride = true;
                preBattleClip = targetBGM; // 記住進戰鬥前正在播的音樂

                // 如果字典裡有設定戰鬥音樂，就播戰鬥音樂
                if (stateBgmDict.ContainsKey(GameState.Battle))
                {
                    SwitchBGM(stateBgmDict[GameState.Battle]);
                }
            }
            // 情況 B：剛離開戰鬥狀態
            else if (lastState == GameState.Battle)
            {
                isBattleOverride = false;

                // 決定要恢復成哪首音樂（優先用戰鬥前存的，沒有就用當前區域音樂）
                AudioClip clipToRestore = preBattleClip != null ? preBattleClip : currentAreaBGM;

                // 如果離開戰鬥後，進入的不是一般遊玩狀態（例如進入紅書或過場），且該狀態有專屬音樂，則優先播專屬音樂
                bool isGameplayState = (currentGameState == GameState.Playing || currentGameState == GameState.Story || currentGameState == GameState.StoryNotMove);
                if (!isGameplayState && stateBgmDict.ContainsKey(currentGameState))
                {
                    clipToRestore = stateBgmDict[currentGameState];
                }

                if (clipToRestore != null)
                {
                    SwitchBGM(clipToRestore);
                }
            }
            // 情況 C：其他一般狀態切換 (非戰鬥相關)
            else if (!isBattleOverride)
            {
                if (isTimelineOverride)
                {
                    SwitchBGM(currentAreaBGM);
                }
                else
                {
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
            }

            lastState = currentGameState; // 更新最後狀態
        }
    }

    private void SwitchBGM(AudioClip newClip)
    {
        if (targetBGM == newClip) return;

        targetBGM = newClip;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeToNewClip(newClip));
    }

    private IEnumerator FadeToNewClip(AudioClip newClip)
    {
        float t = 0;
        float startVolume = audioSource.volume;
        float finalVolume = volumeSlider != null ? volumeSlider.value : Volume;

        // 淡出
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
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
            t += Time.unscaledDeltaTime;
            audioSource.volume = Mathf.Lerp(0, finalVolume, t / fadeDuration);
            yield return null;
        }

        audioSource.volume = finalVolume;
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
            t += Time.unscaledDeltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0, t / fadeDuration);
            yield return null;
        }

        audioSource.Stop();
        audioSource.volume = startVolume;
        targetBGM = null;
    }

    public void SetVolume(float volume)
    {
        audioSource.volume = Mathf.Clamp01(volume);
    }

    private Coroutine volumeFadeCoroutine;

    public void DuckVolume()
    {
        float duckTarget = 0.01f;
        Volume = audioSource.volume;

        if (volumeFadeCoroutine != null) StopCoroutine(volumeFadeCoroutine);
        volumeFadeCoroutine = StartCoroutine(FadeVolumeOnly(duckTarget));
    }

    public void RestoreVolume(float duration = 0.5f)
    {
        float target = volumeSlider != null ? volumeSlider.value : Volume;

        if (volumeFadeCoroutine != null) StopCoroutine(volumeFadeCoroutine);
        volumeFadeCoroutine = StartCoroutine(FadeVolumeOnly(target));
    }

    private IEnumerator FadeVolumeOnly(float targetVolume)
    {
        float t = 0;
        float startVolume = audioSource.volume;
        float duration = 0.5f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, t / duration);
            yield return null;
        }

        audioSource.volume = targetVolume;
    }
}