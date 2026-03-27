using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using Cinemachine;

public class RedBook : MonoBehaviour
{
    public GameObject menuUI;
    public GameObject[] pages; // 0: Party, 1: Inventory, 2: Map, 3: Monster
    public GameObject PageTag;
    public Sprite[] image;
    public CharStateUI BlackState;
    public CharStateUI LylaState;
    public GoalController Goal;
    private int currentPage = 0;
    public bool isOpen = false;
    private Animator animator;
    public GameObject inventoryUI;
    private bool isTriggered = false;
    [Header("紅書教學開啟，請放1-1-1")]
    public StoryScene storylimit;
    [Header("MAP")]
    public Transform player; // 玩家的 Transform
    public float thresholdX = 574.7f; // 臨界值 (例如 0)
    public Sprite spriteA;   // 圖片 A
    public Sprite spriteB;   // 圖片 B
    public Image mapRenderer;
    public GameObject ZoneA;
    public GameObject ZoneB;
    public GameObject ZoneC;
    public StoryScene sceneA;
    public StoryScene sceneB;
    /*
    [Header("限制：這些劇情都播過且結束才能播放 Timeline")]
    public List<GameScene> LimitScenes;

    [Header("Lyla Visuals")]
    public Image LylaImage; // 指向 UI 上的 Image 組件
    public Sprite LylaAfterEventSprite; // 劇情後的圖片
    */
    //public CinemachineVirtualCamera BookCamera = null;
    private void Start()
    {
        animator = GetComponent<Animator>();
        //BookCamera.enabled = false;
    }

    private void Update()
    {
        HandleInput();
        UpdateHUD();
        UpdateMap();
        /*
        if (!isTriggered && AllLimitScenesPlayed())
        {
            //LylaImage.sprite = LylaAfterEventSprite;
            Debug.Log("劇情觸發：Lyla 的圖片已更新！");
            isTriggered = true;
        }
        */
    }
    /*
    private bool AllLimitScenesPlayed()
    {
        foreach (GameScene scene in LimitScenes)
        {
            bool played;
            // 如果沒紀錄，或值不是 true → 還沒完整播完
            if (!GameController.Instance.ScenePlayedState.TryGetValue(scene, out played) || !played)
            {
                return false;
            }
        }
        return true;
    }
    */
    private void HandleInput()
    {
        // Tab 切換紅書
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (GameStateManager.Instance.IsState(GameState.Playing))
            {
                // 【修改處】新增判斷：只有當限制的劇情播完後，才能叫出紅書
                if (AllLimitScenesPlayed())
                {
                    GameStateManager.Instance.SetState(GameState.Redbook);
                    OpenRedBook(GameState.Playing);
                }
                else
                {
                    // (選擇性) 你可以在這裡加上提示音效或是 Debug.Log 告訴玩家「目前還無法使用」
                    Debug.Log("劇情尚未解鎖，無法開啟紅書！");
                }
            }
            else if (isOpen)
            {
                CloseRedBook();
            }
        }

        // ESC 關閉紅書
        if (Input.GetKeyDown(KeyCode.Escape) && isOpen)
        {
            CloseRedBook();
        }
    }

    private void UpdateHUD()
    {
        if (isOpen)
        {
            BlackState.SetHUD();
            LylaState.SetHUD();
        }
    }
    public void UpdateMap()
    {
        if (player == null || mapRenderer == null) return;

        // 判斷邏輯
        if (player.position.x < thresholdX)
        {
            // ================= 【地圖一】 =================
            if (mapRenderer.sprite != spriteA)
            {
                mapRenderer.sprite = spriteA;
            }

            // 開啟 ZoneA，關閉 B 和 C
            SetActiveOptimized(ZoneA, true);
            SetActiveOptimized(ZoneB, false);
            SetActiveOptimized(ZoneC, false);
        }
        else
        {
            // ================= 【地圖二】 =================
            if (mapRenderer.sprite != spriteB)
            {
                mapRenderer.sprite = spriteB;
            }

            SetActiveOptimized(ZoneA, false); // 關閉地圖一的圖示

            // 判斷劇情進度
            bool playedA = HasPlayedScene(sceneA);
            bool playedB = HasPlayedScene(sceneB);

            // 如果「已經觸發A」且「還沒觸發B」
            if (playedA && !playedB)
            {
                SetActiveOptimized(ZoneB, false);
                SetActiveOptimized(ZoneC, true);  // 打開 ZoneC
            }
            else
            {
                // 其他情況 (尚未觸發A，或者已經觸發B代表結束了)
                SetActiveOptimized(ZoneB, true);  // 打開 ZoneB
                SetActiveOptimized(ZoneC, false);
            }
        }
    }

    // --- 以下為新增的輔助方法 ---

    // 檢查特定劇情是否已經播過
    private bool HasPlayedScene(StoryScene scene)
    {
        if (scene == null) return false; // 防呆

        bool played;
        if (GameController.Instance.ScenePlayedState.TryGetValue(scene, out played))
        {
            return played;
        }
        return false;
    }

    // 優化效能用的 SetActive：只有在狀態不同時才執行，避免每幀呼叫浪費效能
    private void SetActiveOptimized(GameObject obj, bool state)
    {
        if (obj != null && obj.activeSelf != state)
        {
            obj.SetActive(state);
        }
    }

    public void OpenRedBook(GameState previousState)
    {
        StartCoroutine(OpenBookCoroutine(previousState));
    }

    public void CloseRedBook()
    {
        isOpen = false;
        animator.SetTrigger("DOWN");
        StartCoroutine(CloseBookCoroutine());
    }

    private IEnumerator OpenBookCoroutine(GameState previousState)
    {
        yield return new WaitForSeconds(0.5f);
        isOpen = true;
        animator.SetTrigger("UP");
        menuUI.SetActive(true);
        ShowPage(0);
        Goal.UpdateObjective();

        if (inventoryUI != null)
            inventoryUI.GetComponent<InventoryUI>().RefreshUI();
    }

    private IEnumerator CloseBookCoroutine()
    {
        yield return new WaitForSeconds(0.7f);
        menuUI.SetActive(false);

        // 恢復到之前的狀態
        if (GameStateManager.Instance.IsState(GameState.Battle))
        {
            GameStateManager.Instance.SetState(GameState.Battle);
        }
        else
        {
            GameStateManager.Instance.SetState(GameState.Playing);
        }
    }

    public IEnumerator CloseBook()
    {
        // 等待 Animator 進入 DOWN 狀態
        yield return new WaitForSeconds(0.7f);
        // 動畫播放完畢，才關閉 UI 與切換狀態
        menuUI.SetActive(false);
    }

    public IEnumerator OpenBook()
    {
        //延遲紅書上升動畫攝影機抬頭
        yield return new WaitForSeconds(0.5f);
        isOpen = true;
        //BookCamera.enabled = true;
        animator.SetTrigger("UP");
        menuUI.SetActive(true);
        ShowPage(2); 
        Goal.UpdateObjective();
    }

    public void ShowPage(int index)
    {
        currentPage = index;
        for (int i = 0; i < pages.Length; i++)
        {
            pages[i].SetActive(i == index);
            PageTag.GetComponent<Image>().sprite = image[index];
        }
    }

    private bool AllLimitScenesPlayed()
    {
        // 【新增防呆機制】如果沒有設定 storylimit (為 null)，直接視為「已解鎖」，允許開啟紅書
        if (storylimit == null)
        {
            return true;
        }

        bool played;
        // 如果有設定 storylimit，再去檢查字典裡有沒有紀錄，或值是否為 true
        if (!GameController.Instance.ScenePlayedState.TryGetValue(storylimit, out played) || !played)
        {
            return false;
        }

        return true;
    }
}