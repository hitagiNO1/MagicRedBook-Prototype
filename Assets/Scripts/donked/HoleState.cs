using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class HoleState : MonoBehaviour
{
    [HideInInspector] public Holenumber Hole;
    [HideInInspector] public bool IsMouse;
    [HideInInspector] public mouseborn Mouseborn;

    private usercontrol userControl;
    private SpriteRenderer sr;
    private Sprite originalSprite;
    private bool isClicked = false;

    public void Initialize(Holenumber hole, Transform root, bool isMouse, mouseborn mb)
    {
        Hole = hole;
        IsMouse = isMouse;
        Mouseborn = mb;
        isClicked = false;

        userControl = FindObjectOfType<usercontrol>();
        sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            originalSprite = sr.sprite;
        }
    }

    // 使用 OnMouse 系列事件（需要 Camera + Physics2DRaycaster）
    private void OnMouseDown()
    {
        if (isClicked || userControl == null) return;
        isClicked = true;
        userControl.OnHoleClicked(this);
    }

    private void OnMouseEnter()
    {
        if (isClicked || sr == null || Mouseborn == null) return;
        Sprite hover = Mouseborn.GetHoverSprite(Hole);
        if (hover != null)
            sr.sprite = hover;
    }

    private void OnMouseExit()
    {
        if (isClicked || sr == null || Mouseborn == null) return;
        Sprite normal = Mouseborn.GetSpawnedSprite(Hole);
        sr.sprite = normal != null ? normal : originalSprite;
    }

    // 保留空方法避免被誤移除
    private void OnDisable() { }
    private void OnDestroy() { }
}