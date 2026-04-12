using UnityEngine;
using Cinemachine; // 記得引入 Cinemachine 命名空間

public class StoryCamerar : MonoBehaviour
{
    [Header("Cinemachine 虛擬攝影機設定")]
    public CinemachineVirtualCamera playerCamera; // 平常自由探索時的鏡頭
    public CinemachineVirtualCamera storyCamera;  // 劇情對話時的專用鏡頭

    // 用來記錄上一個影格的狀態，用來比對是否發生了「切換」
    private GameState lastKnownState;

    private void Start()
    {
        // 初始化時先同步一次目前的狀態
        if (GameStateManager.Instance != null)
        {
            lastKnownState = GameStateManager.Instance.currentState;
            ApplyCameraState(lastKnownState); // 開場先設定一次正確的鏡頭
        }
    }

    private void Update()
    {
        // 防呆保護
        if (GameStateManager.Instance == null) return;

        GameState currentState = GameStateManager.Instance.currentState;

        // 核心偵測：如果現在的狀態跟紀錄的不同，代表狀態發生了切換！
        if (currentState != lastKnownState)
        {
            ApplyCameraState(currentState); // 執行鏡頭切換
            lastKnownState = currentState;  // 更新紀錄，等待下一次改變
        }
    }

    private void ApplyCameraState(GameState newState)
    {
        // 判斷是否為劇情狀態
        if (newState == GameState.Story || newState == GameState.StoryNotMove)
        {
            // 切換為劇情鏡頭 (提高優先度)
            storyCamera.Priority = 20;
            playerCamera.Priority = 11;
            Debug.Log("[CameraSwitcher] 已切換至：劇情對話鏡頭");
        }
        else
        {
            // 只要不是劇情狀態，就切回玩家主鏡頭
            storyCamera.Priority = 10;
            playerCamera.Priority = 11;
            Debug.Log("[CameraSwitcher] 已切換回：平常探索鏡頭");
        }

        // 💡 擴充提示：
        // 如果之後需要讓 Blake 打開魔導紅書（GameState.Redbook）時，
        // 也有一個專屬的 UI 特寫鏡頭，只要在這裡多加一個 else if 就能輕鬆達成。
    }
}