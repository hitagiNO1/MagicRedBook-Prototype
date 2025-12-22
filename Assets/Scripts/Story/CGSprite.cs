using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CGSprite : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
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
            spriteRenderer.sprite = speaker.sprites[spriteIndex];

            // 如果原本是隱藏的，確保它顯示出來 (可選)
            // gameObject.SetActive(true); 
        }
        else
        {
            Debug.LogWarning($"ChangeSprite: 索引 {spriteIndex} 超出 {speaker.speakerName} 的圖片範圍！");
        }
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
