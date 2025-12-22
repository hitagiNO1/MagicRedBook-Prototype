using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class EffectManager : MonoBehaviour
{
    public static EffectManager Instance; // 單例靜態

    [System.Serializable]
    public class EffectEntry
    {
        public string effectName; // 特效名稱（用來查找）
        public GameObject prefab; // 特效的Prefab
        public Vector2 leftPosition;
        public Vector2 rightPosition;
    }
    public List<EffectEntry> effects = new List<EffectEntry>(); // Inspector 上設定的特效列表

    private Dictionary<string, GameObject> effectDict; // 用來快速查找特效的字典
    public GameObject targetCanvas;
    private bool lastEffectOnLeft = true; // 記錄最後一個特效生成在哪邊

    [Header("受傷表情Sprites、Image")]
    public Image BlackHead;   //腳色變紅
    public Sprite hurtFace;
    public Image LylaHead;   //腳色變紅
    public Sprite LylahurtFace;

    public Image Black;   //外框
    public Image Lyla;   //
    public Sprite BlackBall;

    void Awake()
    {
        // 單例 EffectManager
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitDictionary(); // 初始化字典
        }
        else
        {
            Destroy(gameObject);
        }
    }
    void InitDictionary()
    {
        // 將 Inspector 上設定的特效列表轉成字典，方便快速查找
        effectDict = new Dictionary<string, GameObject>();
        foreach (var e in effects)
        {
            if (!effectDict.ContainsKey(e.effectName))
                effectDict.Add(e.effectName, e.prefab);
        }
    }

    /*
    // 立即播放特效（無延遲、無縮放）
    public void PlayEffect(string effectName, Vector3 position) //名稱 位置
    {
        PlayEffect(effectName, position, 0f, 1f); // 呼叫有延遲與縮放參數的版本
    }

    // 播放特效（設定延遲與縮放）
    public void PlayEffect(string effectName, Vector3 position, float delay, float scale) //名稱 位置 延遲秒數 縮放倍率
    {
        // 確認特效存在字典中
        if (effectDict.TryGetValue(effectName, out GameObject prefab))
        {
            // 啟動協程來處理延遲播放
            StartCoroutine(PlayEffectCoroutine(prefab, position, delay, scale));
        }
        else
        {
            Debug.LogWarning($"Effect '{effectName}' not found!"); // 找不到特效警告
        }
    }
    
    private IEnumerator PlayEffectCoroutine(GameObject prefab, Vector3 position, float delay, float scale)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);
        GameObject fx = Instantiate(prefab, position, Quaternion.identity); // 在指定位置生成特效
        fx.transform.localScale *= scale; // （等比縮放 X/Y/Z）
    }
    */

    // 播放 UI 特效（生成在 Canvas）
    public void PlayUIEffect(string effectName, Vector2 uiPosition)
    {
        PlayUIEffect(effectName, uiPosition, 0f, 1f);
        Debug.Log($"播放特效 {effectName}");
    }
    // 播放 UI 特效（延遲+縮放）
    public void PlayUIEffect(string effectName, Vector2 uiPosition, float delay, float scale)
    {
        
        if (effectDict.TryGetValue(effectName, out GameObject prefab))
        {
            StartCoroutine(PlayUIEffectCoroutine(prefab, uiPosition, delay, scale));
        }
        else
        {
            Debug.LogWarning($"UI Effect '{effectName}' not found!");
        }
    }
    // 生成 UI 特效
    private IEnumerator PlayUIEffectCoroutine(GameObject prefab, Vector2 uiPosition, float delay, float scale)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);
        GameObject fx = Instantiate(prefab, targetCanvas.transform);       // 在 Canvas 內生成特效
        fx.GetComponent<RectTransform>().anchoredPosition = uiPosition;    // 設定特效的 UI 座標
        fx.transform.localScale *= scale;    // 縮放
    }

    public void PlayUIEffectOneSide(string effectName, bool playLeft = true)
    {
        // 找到對應特效資料
        EffectEntry entry = effects.Find(e => e.effectName == effectName);
        if (entry == null)
        {
            Debug.LogWarning($"未找到特效 {effectName} 的位置設定！");
            return;
        }

        // 查找 prefab
        if (!effectDict.TryGetValue(effectName, out GameObject prefab))
        {
            Debug.LogWarning($"特效 {effectName} 未在字典中註冊！");
            return;
        }

        // 決定播放位置
        Vector2 pos = playLeft ? entry.leftPosition : entry.rightPosition;

        // 生成特效
        GameObject fx = Instantiate(prefab, targetCanvas.transform);
        fx.GetComponent<RectTransform>().anchoredPosition = pos;

        // 設定縮放方向（左邊的特效要水平反轉）
        Vector3 scale = Vector3.one;
        if (playLeft)
            scale.x = -1f;  // 左邊翻轉
        fx.transform.localScale = scale;
        // 記錄最後一個特效方向
        lastEffectOnLeft = playLeft;
        //Debug.Log($"播放特效 {effectName} ({(playLeft ? "左" : "右")}) at {pos}, scale={scale}");
    }


    // 簡單的晃動：使用 Noise Profile
    public void ShakeCamera(float amplitude, float frequency, float duration)
    {
        // 取得目前啟用的 VCam
        CinemachineBrain brain = Camera.main.GetComponent<CinemachineBrain>();
        var vcam = brain.ActiveVirtualCamera as CinemachineVirtualCamera;

        if (vcam != null)
        {
            var noise = vcam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            if (noise != null)
            {
                StartCoroutine(DoShake(noise, amplitude, frequency, duration));
            }
        }
    }
    private IEnumerator DoShake(
        CinemachineBasicMultiChannelPerlin noise,
        float amplitude, float frequency, float duration)
    {
        // 設定晃動強度
        noise.m_AmplitudeGain = amplitude;
        noise.m_FrequencyGain = frequency;

        yield return new WaitForSeconds(duration);

        // 恢復成 0
        noise.m_AmplitudeGain = 0;
        noise.m_FrequencyGain = 0;
    }


    //閃紅
    public void FlashRed(float duration)
    {
        bool leftSide = lastEffectOnLeft;
        Image targetImage = leftSide ? LylaHead : BlackHead;
        Sprite hurtSprite = leftSide ? LylahurtFace : hurtFace;
        StartCoroutine(DoFlashRedSide(targetImage, hurtSprite, duration));
    }

    private IEnumerator DoFlashRedSide(Image targetImage, Sprite hurtSprite, float duration)
    {
        if (targetImage == null || hurtSprite == null) yield break;

        Color originalColor = targetImage.color;
        Sprite originalSprite = targetImage.sprite;

        targetImage.color = new Color32(255, 163, 163, 255);
        targetImage.sprite = hurtSprite;

        yield return new WaitForSeconds(duration);

        targetImage.color = originalColor;
        targetImage.sprite = originalSprite;
    }
    public void FlashRedAll(float duration)
    {
        // 左右角色都閃紅
        if (BlackHead != null && hurtFace != null)
            StartCoroutine(DoFlashRed(BlackHead, hurtFace, duration));

        if (LylaHead != null && LylahurtFace != null)
            StartCoroutine(DoFlashRed(LylaHead, LylahurtFace, duration));
    }

    private IEnumerator DoFlashRed(Image targetImage, Sprite hurtSprite, float duration)
    {
        if (targetImage == null || hurtSprite == null) yield break;

        Color originalColor = targetImage.color;
        Sprite originalSprite = targetImage.sprite;

        // 閃紅
        targetImage.color = new Color32(255, 163, 163, 255);
        targetImage.sprite = hurtSprite;

        yield return new WaitForSeconds(duration);

        // 回復原狀
        targetImage.color = originalColor;
        targetImage.sprite = originalSprite;
    }
    public void Def()
    {
    }
    public void Def4()
    {
        StartCoroutine(PlayBounceAndSwitch(BlackBall));
    } 
    private IEnumerator PlayBounceAndSwitch(Sprite newSprite)
    {
        // 取得原始位置
        Vector3 startPos = BlackHead.rectTransform.anchoredPosition;

        // 跳動高度
        float jumpHeight = 20f;

        // 動畫時間
        float duration = 0.2f;
        float half = duration * 0.5f;

        float t = 0f;

        // ----------- 第一段：向上跳 -----------
        while (t < half)
        {
            t += Time.deltaTime;
            float percent = t / half;

            // EaseOut (跳起來快→慢)
            float height = Mathf.Lerp(0, jumpHeight, percent);

            BlackHead.rectTransform.anchoredPosition =
                startPos + Vector3.up * height;

            yield return null;
        }

        // 切換頭像
        BlackHead.sprite = newSprite;

        // 重置 t
        t = 0f;

        // ----------- 第二段：落下 -----------
        while (t < half)
        {
            t += Time.deltaTime;
            float percent = t / half;

            // EaseIn (落下慢→快)
            float height = Mathf.Lerp(jumpHeight, 0, percent);

            BlackHead.rectTransform.anchoredPosition =
                startPos + Vector3.up * height;

            yield return null;
        }

        // 最後保證回原位置
        BlackHead.rectTransform.anchoredPosition = startPos;
    }
    // 讓 Black 跳
    public void BlackJump()
    {
        // 傳入 Black 的 RectTransform
        StartCoroutine(PlayBounce(BlackHead.rectTransform));
    }

    // 讓 Lyla 跳
    public void LylaJump()
    {
        // 傳入 Lyla 的 RectTransform
        StartCoroutine(PlayBounce(LylaHead.rectTransform));
    }

    // 通用跳躍函式：傳入「誰」要跳 (target)
    private IEnumerator PlayBounce(RectTransform target)
    {
        if (target == null) yield break;

        // 1. 先記住這個人原本站在哪裡
        Vector3 startPos = target.anchoredPosition;

        float jumpHeight = 20f;
        float duration = 0.2f;
        float half = duration * 0.5f;
        float t = 0f;

        // ----------- 第一段：向上跳 -----------
        while (t < half)
        {
            t += Time.deltaTime;
            float percent = t / half;

            // EaseOut
            float height = Mathf.Lerp(0, jumpHeight, percent);

            // 【關鍵修正】這裡改成移動傳進來的 target，而不是寫死的 BlackHead
            target.anchoredPosition = startPos + Vector3.up * height;

            yield return null;
        }

        t = 0f;

        // ----------- 第二段：落下 -----------
        while (t < half)
        {
            t += Time.deltaTime;
            float percent = t / half;

            // EaseIn
            float height = Mathf.Lerp(jumpHeight, 0, percent);

            // 【關鍵修正】移動 target
            target.anchoredPosition = startPos + Vector3.up * height;

            yield return null;
        }

        // 最後確保回到原本的位置
        target.anchoredPosition = startPos;
    }
    public void LylaMove()
    {
        StartCoroutine(LeftRightTwice(LylaHead.rectTransform, 40f, 0.15f));
    }
    public IEnumerator LeftRightTwice(RectTransform target, float distance, float stepTime)
    {
        Vector3 center = target.anchoredPosition;
        Vector3 left = center + Vector3.left * distance;
        Vector3 right = center + Vector3.right * distance;

        // 動畫序列：左、中、右、中、左、中、右、中
        Vector3[] path = new Vector3[]
        {
        left,  center,
        right, center,
        left,  center,
        right, center
        };

        foreach (var pos in path)
        {
            yield return MoveTo(target, pos, stepTime);
        }

        // 最終確保回到中間
        target.anchoredPosition = center;
    }

    private IEnumerator MoveTo(RectTransform target, Vector3 targetPos, float duration)
    {
        Vector3 startPos = target.anchoredPosition;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float percent = t / duration;

            // 順滑曲線（EaseInOut）
            float curve = percent * percent * (3f - 2f * percent);

            target.anchoredPosition = Vector3.Lerp(startPos, targetPos, curve);

            yield return null;
        }

        target.anchoredPosition = targetPos;
    }

}
