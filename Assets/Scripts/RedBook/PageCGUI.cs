using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PageCGUI : MonoBehaviour
{
    public GameObject[] pages; // 頁面物件（五個 Panel）
    public static PageCGUI Instance;
    
    private int currentPage = 0;
    void Start()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        ShowPage(currentPage); // 初始化顯示第一頁
    }

    public void AddPage(GameObject NewPage, int Num)
    {
        Destroy(pages[Num].transform.GetChild(0).gameObject);
        Instantiate(NewPage, pages[Num].GetComponent<Transform>());
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
    void ShowPage(int pageIndex)
    {
        for (int i = 0; i < pages.Length; i++)
        {
            pages[i].SetActive(i == pageIndex);
        }
    }
}

