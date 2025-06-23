using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class shotcan : MonoBehaviour
{
    public GameObject CanPrefab;
    public int totalCans = 10;
    public float spawnInterval = 1f;
    public float gameTime = 30f;

    public TextMeshProUGUI timerText;
    public TextMeshProUGUI statusText;

    private int cansSpawned = 0;
    private int cansShot = 0;
    private float timeRemaining;
    private bool gameOver = false;

    void Start()
    {
        timeRemaining = gameTime;
        StartCoroutine(SpawnCans());
    }

    void Update()
    {
        if (gameOver) return;

        timeRemaining -= Time.deltaTime;
        timerText.text = "Time: " + Mathf.CeilToInt(timeRemaining);

        if (timeRemaining <= 0)
        {
            EndGame(false); // 時間到未通關
        }
    }

    IEnumerator SpawnCans()
    {
        while (cansSpawned < totalCans)
        {
            Vector3 pos = new Vector3(Random.Range(-6f, 6f), 0, Random.Range(3f, 7f));
            Instantiate(CanPrefab, pos, Quaternion.identity);
            cansSpawned++;
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    public void OnCansShot()
    {
        cansShot++;
        if (cansShot >= totalCans)
        {
            EndGame(true); // 全部擊殺，過關
        }
    }

    void EndGame(bool success)
    {
        gameOver = true;
        statusText.text = success ? " 你贏了！" : " 失敗了...";
        Time.timeScale = 0; // 暫停遊戲
    }
}
