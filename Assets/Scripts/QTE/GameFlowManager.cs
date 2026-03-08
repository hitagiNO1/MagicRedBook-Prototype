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
    public GameObject track;
    private AudioSource audioSource;
    public AudioClip newClip;
    void Start()
    {
        ShowMainMenu();
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.volume = 0.08f;
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
        track.SetActive(true);

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
        // 顯示最終得分
        audioSource.clip = newClip;
        StartCoroutine(FadeOutAndStop());
    }
    private IEnumerator FadeOutAndStop()
    {
        float t = 0;
        float startVolume = audioSource.volume;

        while (t < 1f)
        {
            t += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0, t / 1f);
            yield return null;
        }

        audioSource.Stop();
        audioSource.volume = startVolume;
    }

}
