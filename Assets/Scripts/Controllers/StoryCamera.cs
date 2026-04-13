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
        // 情況 1：明確進入劇情狀態
        if (newState == GameState.Story || newState == GameState.StoryNotMove)
        {
            storyCamera.Priority = 20;
            playerCamera.Priority = 11;
            Debug.Log("[CameraSwitcher] 已切換至：劇情對話鏡頭");
        }

        // ★ 情況 2：進入過場動畫 (Mangaing)
        else if (newState == GameState.Mangaing)
        {
            // 拿出你 GameStateManager 裡的法寶：確認「上一個狀態」是什麼
            GameState prevState = GameStateManager.Instance.previousState;

            // 如果是從「劇情」途中切進動畫的 -> 維持劇情鏡頭
            if (prevState == GameState.Story || prevState == GameState.StoryNotMove)
            {
                storyCamera.Priority = 20;
                playerCamera.Priority = 11;
                Debug.Log("[CameraSwitcher] 這是劇情中的動畫，維持：劇情對話鏡頭");
            }
            // 如果是從「遊玩」(Playing) 或其他狀態切進動畫的 -> 維持玩家鏡頭
            else
            {
                storyCamera.Priority = 10;
                playerCamera.Priority = 11;
                Debug.Log("[CameraSwitcher] 這裡是一般動畫，維持：平常探索鏡頭");
            }
        }

        // 情況 3：明確回到自由探索狀態
        else if (newState == GameState.Playing)
        {
            storyCamera.Priority = 10;
            playerCamera.Priority = 11;
            Debug.Log("[CameraSwitcher] 已切換回：平常探索鏡頭");
        }
    }
}