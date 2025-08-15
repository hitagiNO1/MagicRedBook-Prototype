using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class EffectManager : MonoBehaviour
{
    public static EffectManager Instance; // 單例靜態

    [System.Serializable]
    public class EffectEntry
    {
        public string effectName; // 特效名稱（用來查找）
        public GameObject prefab; // 特效的Prefab
    }
    public List<EffectEntry> effects = new List<EffectEntry>(); // Inspector 上設定的特效列表

    private Dictionary<string, GameObject> effectDict; // 用來快速查找特效的字典
    public GameObject targetCanvas;
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
}
