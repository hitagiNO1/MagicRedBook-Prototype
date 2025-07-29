using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameFlowManager : MonoBehaviour
{
    public GameObject mainCanvas;
    public GameObject gameCanvas;
    public GameObject resultCanvas;
    public GameObject startbutton;
    public canclick canclick;
   
  

    void Start()
    {
        ShowMainMenu();
    }

    public void ShowMainMenu()
    {
        mainCanvas.SetActive(true);
        gameCanvas.SetActive(false);
        resultCanvas.SetActive(false);
    }

    public void StartGame()
    {
        mainCanvas.SetActive(false);
        gameCanvas.SetActive(true);
        resultCanvas.SetActive(false);
        startbutton.SetActive(true);
        canclick.RestartGame();

    }

    /*public void Restart()
    {
       canclick.RestartGame();
    }*/

    public void ShowResult()
    {
        mainCanvas.SetActive(false);
        gameCanvas.SetActive(false);
        resultCanvas.SetActive(true);

        // 顯示最終得分
        
    }

    
}
