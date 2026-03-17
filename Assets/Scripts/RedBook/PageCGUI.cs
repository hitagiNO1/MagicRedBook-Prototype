using System.Collections.Generic;
using UnityEngine;

public class PageCGUI : MonoBehaviour
{
    [Header("頁面 Panels (五個)")]
    [SerializeField] private GameObject[] pages;

    [Header("頁面 Prefabs 對應 panels")]
    [SerializeField] private GameObject[] pagesPrefabs;

    public static PageCGUI Instance;

    private int currentPage = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        ShowPage(currentPage); // 初始化顯示第一頁
    }

    /// <summary>
    /// 在指定頁面替換內容
    /// </summary>
    /// <param name="pageIndex">頁面索引</param>
    public void AddPage(int pageIndex)
    {
        if (pageIndex < 0 || pageIndex >= pages.Length)
        {
            Debug.LogWarning("AddPage: pageIndex 超出範圍");
            return;
        }

        Transform pageTransform = pages[pageIndex].transform;

        // 如果有子物件，先刪掉
        if (pageTransform.childCount > 0)
        {
            Destroy(pageTransform.GetChild(0).gameObject);
        }

        // Instantiate Prefab
        if (pageIndex < pagesPrefabs.Length && pagesPrefabs[pageIndex] != null)
        {
            GameObject newPage = Instantiate(pagesPrefabs[pageIndex], pageTransform);
            newPage.transform.localPosition = Vector3.zero;
            newPage.transform.localRotation = Quaternion.identity;
            newPage.transform.localScale = Vector3.one;
        }
        else
        {
            Debug.LogWarning("AddPage: Prefab 未設定或索引超出範圍");
        }
    }

    public void GoToPreviousPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            ShowPage(currentPage);
        }
    }

    public void GoToNextPage()
    {
        if (currentPage < pages.Length - 1)
        {
            currentPage++;
            ShowPage(currentPage);
        }
    }

    private void ShowPage(int pageIndex)
    {
        for (int i = 0; i < pages.Length; i++)
        {
            pages[i].SetActive(i == pageIndex);
        }
    }
}
