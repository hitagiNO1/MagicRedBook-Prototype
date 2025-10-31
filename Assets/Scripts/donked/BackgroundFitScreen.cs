using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BackgroundFitScreen : MonoBehaviour
{
    void Update()
    {
        FitSpriteToScreen();
    }

    void FitSpriteToScreen()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr.sprite == null) return;

        // 關閉自動適應（我們自己算）
        transform.localScale = Vector3.one;

        // 取得 Sprite 的寬高（單位：像素）
        float spriteWidth = sr.sprite.rect.width;
        float spriteHeight = sr.sprite.rect.height;

        // 取得螢幕的寬高（單位：世界單位）
        float screenHeight = Camera.main.orthographicSize * 2;
        float screenWidth = screenHeight * Screen.width / Screen.height;

        // 計算縮放比例
        float scaleX = screenWidth / spriteWidth;
        float scaleY = screenHeight / spriteHeight;
        float scale = Mathf.Max(scaleX, scaleY); // 選較大者，確保完全覆蓋

        // 套用縮放
        transform.localScale = new Vector3(scale, scale, 1);

        // 確保在最底部（Z 軸）
        transform.position = new Vector3(Camera.main.transform.position.x,
                                        Camera.main.transform.position.y,
                                        10); // Z = 10（在 Camera 後面）
    }
}