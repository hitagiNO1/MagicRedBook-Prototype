using UnityEngine;
using UnityEngine.UI;

public class MiniMapController : MonoBehaviour
{
    public static MiniMapController instance; // 單例，讓Trigger方便呼叫

    // 記錄當前正亮著的那個圖示
    private GameObject currentActiveIcon;

    void Awake()
    {
        instance = this;
    }

    // 這是給 Trigger 呼叫的方法
    // 參數 iconToShow：代表玩家踩到的那個區域對應的 UI 圖示
    public void UpdateLocation(GameObject iconToShow)
    {

        // 1. 如果有舊的圖示亮著，先把它關掉
        if (currentActiveIcon != null)
        {
            currentActiveIcon.SetActive(false);
        }

        // 2. 把傳進來的這個新圖示打開
        if (iconToShow != null)
        {
            iconToShow.SetActive(true);
            Debug.Log("MiniMapController: Activated icon " + iconToShow.name);
            // 3. 記錄下來，下次才能關掉它
            currentActiveIcon = iconToShow;
        }
    }
}