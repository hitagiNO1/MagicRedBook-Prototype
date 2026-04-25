using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class RedBookMain : MonoBehaviour
{
    [Header("UI 引用")]
    public GameObject menuUI;
    public GameObject[] pages;      // 0: Party, 1: Inventory, 2: Map, 3: Monster
    public GameObject PageTag;      // 顯示當前標籤的 Image 物件
    public Sprite[] tagImages;      // 各分頁對應的標籤圖片

    [Header("狀態與動畫")]
    public bool isOpen = false;
    private Animator animator;

    [Header("頁面 Panels (五個)")]
    [SerializeField] private GameObject[] CGpages;
    private int currentPage = 4;
    private void Start()
    {
        animator = GetComponent<Animator>();
        menuUI.SetActive(false); // 確保一開始是關閉的
    }

    private void Update()
    {
        // Tab 鍵：開關紅書
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (!isOpen)
                OpenRedBook();
            else
                CloseRedBook();
        }

        // ESC 鍵：如果開啟中，則關閉
        if (Input.GetKeyDown(KeyCode.Escape) && isOpen)
        {
            CloseRedBook();
        }
    }

    // --- 開啟與關閉邏輯 ---

    public void OpenRedBook()
    {
        isOpen = true;
        menuUI.SetActive(true);
        animator.SetTrigger("UP");

        // 依照要求，開啟時預設顯示第 3 頁 (索引 3)
        ShowPage(3);

        // 如果有遊戲狀態管理器，記得切換狀態 (選用)
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.SetState(GameState.Redbook);
    }

    public void CloseRedBook()
    {
        isOpen = false;
        animator.SetTrigger("DOWN");

        // 延遲關閉 UI，讓動畫播完 (時間依照你原本的 0.7s)
        StartCoroutine(DisableUIAfterAnimation());

        if (GameStateManager.Instance != null)
            GameStateManager.Instance.SetState(GameState.Playing);
    }

    private IEnumerator DisableUIAfterAnimation()
    {
        yield return new WaitForSeconds(0.7f);
        if (!isOpen) // 再次確認，避免在延遲期間又被打開
            menuUI.SetActive(false);
    }

    // --- 分頁切換 ---

    public void ShowPage(int index)
    {
        if (index < 0 || index >= pages.Length) return;

        for (int i = 0; i < pages.Length; i++)
        {
            // 切換頁面物件顯示
            pages[i].SetActive(i == index);
        }

        // 更新標籤圖片 (PageTag)
        if (PageTag != null && tagImages.Length > index)
        {
            PageTag.GetComponent<Image>().sprite = tagImages[index];
        }
    }
    public void GoToPreviousPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            ShowPageCG(currentPage);
        }
    }

    public void GoToNextPage()
    {
        if (currentPage < CGpages.Length - 1)
        {
            currentPage++;
            ShowPageCG(currentPage);
        }
    }

    private void ShowPageCG(int pageIndex)
    {
        for (int i = 0; i < CGpages.Length; i++)
        {
            CGpages[i].SetActive(i == pageIndex);
        }
    }
}