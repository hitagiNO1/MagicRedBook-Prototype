using UnityEngine;

public class CursorController : MonoBehaviour
{
    [Header("鼠標圖片設定")]
    public Texture2D defaultCursor; // 一般狀態的圖
    public Texture2D clickCursor;   // 點擊時的圖 (按住不放時顯示)

    [Header("熱點設定 (0,0)為左上角")]
    public Vector2 hotSpot = Vector2.zero; // 依據你的圖案調整重心

    void Start()
    {
        // 遊戲開始時，先設為預設鼠標
        SetCursor(defaultCursor);
    }

    void Update()
    {
        // 當滑鼠左鍵「按下」的瞬間
        if (Input.GetMouseButtonDown(0))
        {
            SetCursor(clickCursor);
        }
        // 當滑鼠左鍵「放開」的瞬間
        else if (Input.GetMouseButtonUp(0))
        {
            SetCursor(defaultCursor);
        }
    }

    // 封裝一個方法來切換，程式碼比較整潔
    void SetCursor(Texture2D cursorTexture)
    {
        Cursor.SetCursor(cursorTexture, hotSpot, CursorMode.Auto);
    }
}