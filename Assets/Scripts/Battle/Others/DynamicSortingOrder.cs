using UnityEngine;

[ExecuteInEditMode] // 讓腳本在編輯模式下運行
public class DynamicSortingOrderEditor : MonoBehaviour
{
    public int sortingOrderBase = 0; // 基礎排序層級
    public float offset = 0.1f;      // Z 軸與排序比例偏移
    public bool autoUpdate = true;  // 是否在編輯模式下自動更新

    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        // 獲取 SpriteRenderer 組件
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("Missing SpriteRenderer osn " + gameObject.name);
        }
    }

    void Update()
    {
        if (!autoUpdate || spriteRenderer == null) return;

        // 計算並設置排序層級
        spriteRenderer.sortingOrder = sortingOrderBase - Mathf.RoundToInt(transform.position.z / offset);
    }

    // 用於手動更新排序層級（例如在編輯器中點擊按鈕觸發）
    public void UpdateSortingOrder()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = sortingOrderBase - Mathf.RoundToInt(transform.position.z / offset);
        }
    }
}
