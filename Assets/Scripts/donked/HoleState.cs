using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class HoleState : MonoBehaviour,
    IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [HideInInspector] public Holenumber Hole;
    [HideInInspector] public bool IsMouse;
    [HideInInspector] public mouseborn Mouseborn;

    private usercontrol userControl;
    private Image img;
    private Sprite originalSprite;
    private bool isClicked = false;  // 新增：點擊後鎖定

    public void Initialize(Holenumber hole, Transform root, bool isMouse, mouseborn mb)
    {
        Hole = hole;
        IsMouse = isMouse;
        Mouseborn = mb;
        isClicked = false;  // 重置

        userControl = FindObjectOfType<usercontrol>();
        img = GetComponent<Image>();

        if (img != null)
        {
            img.raycastTarget = true;
            originalSprite = img.sprite;  // 記住地洞圖
        }
    }

    // 點擊
    public void OnPointerClick(PointerEventData eventData)
    {
        if (isClicked || userControl == null) return;

        isClicked = true;  // 鎖定：不再換圖
        userControl.OnHoleClicked(this);
    }

    // 滑鼠進入 → 換 Hover 圖
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isClicked || img == null || Mouseborn == null) return;

        Sprite hover = Mouseborn.GetHoverSprite(Hole);
        if (hover != null)
            img.sprite = hover;
    }

    // 滑鼠離開 → 換回原本圖
    public void OnPointerExit(PointerEventData eventData)
    {
        if (isClicked || img == null || Mouseborn == null) return;

        Sprite normal = Mouseborn.GetSpawnedSprite(Hole);
        img.sprite = normal != null ? normal : originalSprite;
    }


    // 重要：被 mouseborn 銷毀時，不要覆蓋 spriteA
    private void OnDisable()
    {
        // 移除這段！否則會蓋掉 ResetHole 的 spriteA
        // if (img != null && Mouseborn != null) { ... }
    }

    private void OnDestroy()
    {
        // 什麼都不做，讓 mouseborn 完全控制圖片
    }
}