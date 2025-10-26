using UnityEngine;

public class HoleState : MonoBehaviour
{
    public Holenumber Hole { get; set; }
    public Transform Parent { get; set; }
    public bool IsMouse { get; set; } // true 表示老鼠，false 表示 C4
    private mouseborn manager;
    private Sprite originalSprite; // 記錄原始 Sprite（生成時的 spriteB 或 C4）
    private SpriteRenderer sr;

    public void Initialize(Holenumber hole, Transform parent, bool isMouse, mouseborn manager)
    {
        Hole = hole;
        Parent = parent;
        IsMouse = isMouse;
        this.manager = manager;
        sr = GetComponent<SpriteRenderer>();
        originalSprite = sr.sprite; // 記錄當前 Sprite 作為原始圖
    }

    // 滑鼠進入：切換到 Hover Sprite
    void OnMouseEnter()
    {
        if (sr != null)
        {
            // 根據類型切換 Hover Sprite（需從 manager 獲取）
            Sprite hoverSprite = manager.GetHoverSprite(Hole);
            if (hoverSprite != null)
            {
                sr.sprite = hoverSprite;
            }
        }
    }

    // 滑鼠離開：恢復原始 Sprite
    void OnMouseExit()
    {
        if (sr != null && originalSprite != null)
        {
            sr.sprite = originalSprite;
        }
    }

    // 可選：點擊處理
    public void OnClick()
    {
        manager.OnObjectClicked(Hole);
    }
}