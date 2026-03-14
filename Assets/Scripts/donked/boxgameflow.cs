using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class boxgameflow : MonoBehaviour
{
    [Header("Canvas")]
    public GameObject mainCanvas;
    public GameObject gameCanvas;      // 只控制遊戲 UI，不控制背景
    public GameObject resultCanvas;

    [Header("Buttons")]
    public GameObject startbutton;
    public usercontrol usercontrol;
    public GameObject Backbutton;
    public GameObject Restartbutton;

    public PlayableDirector director;
    private AudioSource audioSource;
    public AudioClip VicClip;
    public AudioClip LosClip;
    private bool isWaitingForInput = false;
    void Start()
    {
        ShowMainMenu();
        director = GetComponent <PlayableDirector>();
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;
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
        StartCoroutine(FadeToNewClip(VicClip));
        mainCanvas.SetActive(false);
        gameCanvas.SetActive(false);     // 遊戲 UI 隱藏
        resultCanvas.SetActive(true);
        Backbutton.SetActive(true);
        Restartbutton.SetActive(false);
    }
    public void ShowResultLOSS()
    {
        StartCoroutine(FadeToNewClip(LosClip));
        mainCanvas.SetActive(false);
        gameCanvas.SetActive(false);     // 遊戲 UI 隱藏
        resultCanvas.SetActive(true);
        Backbutton.SetActive(false);
        Restartbutton.SetActive(true);
    }
    private IEnumerator FadeToNewClip(AudioClip newClip)
    {
        yield return new WaitForSeconds(0.05f);
        audioSource.Stop();
        audioSource.clip = newClip;
        audioSource.Play();
        audioSource.loop = false;
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