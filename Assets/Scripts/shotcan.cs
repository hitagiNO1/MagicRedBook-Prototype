using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class shotcan : MonoBehaviour
{
    public Vector3 spawnAreaSize = new Vector3(10f, 3f, 4f);
    public GameObject canPrefab;
    public Transform spawnArea;
    public int totalCans = 15;
    public float gameDuration = 30f;

    private float timeRemaining;
    private int cansLeft;
    private bool isGameActive = false;

    public QTEUI uiManager;

    void Start()
    {
        StartGame();
    }

    void Update()
    {
        if (!isGameActive) return;

        timeRemaining -= Time.deltaTime;
        uiManager.UpdateTimer(timeRemaining);

        if (timeRemaining <= 0f)
        {
            EndGame(false);
        }
    }

    public void StartGame()
    {
        timeRemaining = gameDuration;
        cansLeft = totalCans;
        isGameActive = true;
        SpawnCans();
        uiManager.UpdateCanCounter(cansLeft);
    }

    void SpawnCans()
    {
        int totalCans = 15;
        float spacingY = 1.5f;   // 每排的垂直間距（你也可以改成 Z）
        float[] xPositions = { -2f, 0f, 2f }; // 左、中、右 對應的 X 座標

        Vector3 startPos = spawnArea.position;

        for (int i = 0; i < totalCans; i++)
        {
            int side = Random.Range(0, 3); // 0: 左, 1: 中, 2: 右

            float x = startPos.x + xPositions[side]; // 根據位置決定 X
            float y = startPos.y + i * spacingY;     // 每排往上排
            float z = startPos.z;

            Vector3 spawnPos = new Vector3(x, y, z);
            Instantiate(canPrefab, spawnPos, Quaternion.identity);
        }
    }

    public void OnCanShot()
    {
        if (!isGameActive) return;

        cansLeft--;
        uiManager.UpdateCanCounter(cansLeft);

        if (cansLeft <= 0)
        {
            EndGame(true);
        }
    }

    void EndGame(bool success)
    {
        isGameActive = false;
        uiManager.ShowResult(success);
    }
}
