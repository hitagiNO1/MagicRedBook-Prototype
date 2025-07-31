using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QTEUI : MonoBehaviour
{
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI canCountText;
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;

    public void UpdateTimer(float time)
    {
        timerText.text = "Time: " + Mathf.Ceil(time).ToString();
    }

    public void UpdateCanCounter(int count)
    {
        canCountText.text = "Cans Left: " + count;
    }

    public void ShowResult(bool win)
    {
        resultPanel.SetActive(true);
        resultText.text = win ? "You Win!" : "Time's Up!";
    }
}
