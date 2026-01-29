using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("高度設定")]
    public float groundY = -0.5f; // 地面軌道的 Y 座標 (請依照你的場景調整)
    public float airY = 1.5f;     // 空中軌道的 Y 座標

    [Header("按鍵設定")]
    public KeyCode groundKey = KeyCode.F; // 地面攻擊/移動鍵
    public KeyCode airKey = KeyCode.J;    // 空中攻擊/移動鍵

    // 紀錄角色目前在哪個軌道 (0 = 地面, 1 = 空中)
    private int currentLane = 0;

    void Update()
    {
        // 偵測輸入
        if (Input.GetKeyDown(groundKey))
        {
            MoveToLane(0); // 去地面
            // 這裡之後可以加入：播放地面攻擊動畫、判定打擊
        }
        else if (Input.GetKeyDown(airKey))
        {
            MoveToLane(1); // 去空中
            // 這裡之後可以加入：播放空中攻擊動畫、判定打擊
        }
    }

    void MoveToLane(int laneIndex)
    {
        currentLane = laneIndex;

        // 取得目前的 X 和 Z，只改變 Y
        float newY = (laneIndex == 0) ? groundY : airY;

        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
}