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
    public Animator animator;
    public GameObject inventoryUI;

    [Header("MAP")]
    public Transform player; // 玩家的 Transform
    public float thresholdX = 574.7f; // 臨界值 (例如 0)
    public Sprite spriteA;   // 圖片 A
    public Sprite spriteB;   // 圖片 B
    public Image mapRenderer;
    public GameObject ZoneA;
    public GameObject ZoneB;
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
    }

    private void HandleInput()
    {
        // Tab 切換紅書
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (GameStateManager.Instance.IsState(GameState.Playing))
            {
                GameStateManager.Instance.SetState(GameState.Redbook);
                OpenRedBook(GameState.Playing);
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
            // 如果目前圖片不是 A，才換成 A (節省效能)
            if (mapRenderer.sprite != spriteA)
            {
                mapRenderer.sprite = spriteA;
                ZoneA.SetActive(true);
                ZoneB.SetActive(false);
            }
        }
        else
        {
            // 如果目前圖片不是 B，才換成 B
            if (mapRenderer.sprite != spriteB)
            {
                mapRenderer.sprite = spriteB;
                ZoneA.SetActive(false);
                ZoneB.SetActive(true);
            }
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
        ShowPage(3); // 預設顯示 Monster
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
}