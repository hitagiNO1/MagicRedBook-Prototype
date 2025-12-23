using UnityEngine;

// 不需要繼承 MonoBehaviour，這只是一個用來存資料的靜態類別
public static class GameContext
{
    // 存儲即將要顯示的圖片 (Sprite)
    public static Sprite TargetSprite;

    // 如果你需要知道點了哪個 ID，也可以存 ID
    public static string CurrentItemID;
}
