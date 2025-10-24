using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Playables;

public class SceneFadeIn : MonoBehaviour
{
    public Image fadeImage;
    public float fadeDuration = 1f;
    //public PlayableDirector timelineToPlay; // 指定 Timeline（可空）
    //public CGText scriptsToEnableAfterFade; // 要延後啟動的腳本們
    void Start()
    {
        StartCoroutine(FadeIn());
    }
    IEnumerator FadeIn()
    {
        fadeImage.gameObject.SetActive(true);
        Color color = fadeImage.color;
        color.a = 1;
        fadeImage.color = color;
        yield return new WaitForSeconds(1f);
        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            color.a = 1 - (t / fadeDuration);
            fadeImage.color = color;
            yield return null;
        }
        color.a = 0;
        fadeImage.color = color;
        fadeImage.gameObject.SetActive(false);

        /* 啟用腳本
        if (timelineToPlay)
            scriptsToEnableAfterFade.enabled = true;
        // 播 Timeline
        if (timelineToPlay)
        {
            timelineToPlay.enabled = true;
            timelineToPlay.Play();
        }
        */
    }
}
