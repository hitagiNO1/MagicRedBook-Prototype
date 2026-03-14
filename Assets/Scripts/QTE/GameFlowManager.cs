using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class GameFlowManager : MonoBehaviour
{
    public GameObject mainCanvas;
    public GameObject gameCanvas;
    public GameObject resultCanvas;
    public GameObject startbutton;
    public canclick canclick;
    public GameObject track;

    public PlayableDirector director;
    private bool isWaitingForInput = false;
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
        canclick.StartGame();
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
        track.SetActive(false);
    }
    void Update()
    {
        if (isWaitingForInput && Input.anyKeyDown)
        {
            director.Resume();            // 恢復 Timeline
            isWaitingForInput = false;    // 不再等待輸入
        }
    }
    public void WaitForInputToContinue()
    {
        if (director != null && director.state == PlayState.Playing)
        {
            director.Pause();             // 暫停 Timeline
            isWaitingForInput = true;     // 等待輸入
        }
    }
}
