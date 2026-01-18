using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class boxgameflow : MonoBehaviour
{
    [Header("Canvas")]
    public GameObject mainCanvas;
    public GameObject gameCanvas;      // 只控制遊戲 UI，不控制背景
    public GameObject resultCanvas;

    [Header("Buttons")]
    public GameObject startbutton;
    public usercontrol usercontrol;

    void Start()
    {
        ShowMainMenu();
    }

    public void ShowMainMenu()
    {
        mainCanvas.SetActive(true);
        gameCanvas.SetActive(false);     // 遊戲 UI 隱藏
        resultCanvas.SetActive(false);
    }

    public void StartGame()
    {
        mainCanvas.SetActive(false);
        gameCanvas.SetActive(true);      // 只顯示遊戲 UI
        resultCanvas.SetActive(false);
        usercontrol.RestartGame();
        startbutton.SetActive(true);
    }

    public void ShowResult()
    {
        mainCanvas.SetActive(false);
        gameCanvas.SetActive(false);     // 遊戲 UI 隱藏
        resultCanvas.SetActive(true);
    }
}