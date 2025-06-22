using UnityEngine;
public enum GameState
{
    Playing,   // 自由探索中 CharControlles
    Story,     // 劇情對話中 GameController
    StoryNotMove, // 不能移動的重要劇情對話 GameController
    Mangaing, //過場中 TimelineManager
    Battle,   // 戰鬥中 BattleSystem
    Redbook   //紅書中 RedBook
}
public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance;
    //可以被任何程式碼讀取」（get 是 public），但「只能在這個類別（GameStateManager）裡面修改」
    public GameState currentState { get; private set; } = GameState.Playing;
    private GameState previousState;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void SetState(GameState newState)
    {
        // 可加入狀態切換邏輯或限制
        previousState = currentState; //切換前先記住目前狀態
        Debug.Log($"狀態切換為：{newState}");
        currentState = newState;
        
    }

    //傳入一個 GameState，如果目前 currentState 等於傳入的狀態，回傳 true，否則回傳 false
    public bool IsState(GameState state)
    {
        return currentState == state;
    }
    public void RevertToPreviousState()
    {
        Debug.Log($"還原狀態：{currentState} → {previousState}");
        SetState(previousState);
    }
}
