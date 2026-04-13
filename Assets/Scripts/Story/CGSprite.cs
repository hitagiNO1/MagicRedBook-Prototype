using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CGSprite : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    // --- 新增變數：用來處理淡出過渡 ---
    private SpriteRenderer fadeRenderer;
    private Coroutine fadeCoroutine;

    [Header("過渡設定")]
    public float fadeDuration = 0.5f; // 淡入淡出時間，可在 Inspector 調整

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        // 自動建立一個子物件來處理淡出
        GameObject fadeObj = new GameObject("FadeTransition");
        fadeObj.transform.SetParent(this.transform, false);
        fadeRenderer = fadeObj.AddComponent<SpriteRenderer>();

        // 複製主圖的圖層設定，並將它「往前放一層」，用來遮擋並淡出舊圖
        fadeRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
        fadeRenderer.sortingLayerName = spriteRenderer.sortingLayerName;
        fadeRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;
        fadeRenderer.color = new Color(1, 1, 1, 0); // 預設透明
    }

    // 這就是你要的主要功能：接收 Speaker 和 Index 來換圖
    public void ChangeSprite(Speaker speaker, int spriteIndex)
    {
        // 防呆檢查
        if (speaker == null)
        {
            Debug.LogWarning("ChangeSprite: Speaker 為空！");
            return;
        }

        if (speaker.sprites == null || speaker.sprites.Count == 0)
        {
            Debug.LogWarning($"ChangeSprite: {speaker.speakerName} 沒有設定 Sprite 清單！");
            return;
        }

        // 檢查索引是否超出範圍
        if (spriteIndex >= 0 && spriteIndex < speaker.sprites.Count)
        {
            Sprite targetSprite = speaker.sprites[spriteIndex];

            // 如果原本是隱藏的，確保它顯示出來
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            // 取代原本瞬間換圖的寫法，改為呼叫淡入淡出協程
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            fadeCoroutine = StartCoroutine(CrossFadeRoutine(targetSprite));
        }
        else
        {
            Debug.LogWarning($"ChangeSprite: 索引 {spriteIndex} 超出 {speaker.speakerName} 的圖片範圍！");
        }
    }

    // --- 修改後的淡入淡出協程 ---
    private IEnumerator CrossFadeRoutine(Sprite newSprite)
    {
        // 情況 A：如果目前沒有圖片 (例如剛開場第一次顯示角色) -> 取消漸隱，直接瞬間顯示
        if (spriteRenderer.sprite == null)
        {
            spriteRenderer.sprite = newSprite;
            spriteRenderer.color = Color.white; // 直接把透明度設為 1 (完全顯示)
        }
        // 情況 B：標準換圖 -> 舊圖淡出，新圖在底下墊著
        else
        {
            // 1. 把「舊圖片」交給上層的 fadeRenderer
            fadeRenderer.sprite = spriteRenderer.sprite;
            fadeRenderer.color = Color.white;

            // 2. 底層的 spriteRenderer 瞬間換成「新圖片」
            spriteRenderer.sprite = newSprite;
            spriteRenderer.color = Color.white;

            // 3. 讓上層的 fadeRenderer 逐漸變透明 (淡出舊圖)
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                // 計算 1 到 0 的透明度比例
                float alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
                fadeRenderer.color = new Color(1, 1, 1, alpha);
                yield return null;
            }

            // 4. 動畫結束，重置透明物件
            fadeRenderer.color = new Color(1, 1, 1, 0);
            fadeRenderer.sprite = null;
        }

        fadeCoroutine = null;
    }

    // 額外功能：處理位置移動 (對應你的 Action 結構)
    public void SetPosition(Vector2 coords)
    {
        transform.localPosition = coords;
    }

    // 額外功能：隱藏/顯示
    public void SetVisibility(bool isVisible)
    {
        gameObject.SetActive(isVisible);
    }
}