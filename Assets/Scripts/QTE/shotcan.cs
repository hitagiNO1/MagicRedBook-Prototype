using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class shotcan : MonoBehaviour
{

    public GameObject canPrefab;
    public Transform spawnArea;

    public int totalRows = 15; // 15 行
    public int totalCols = 3;  // 3 列（左中右）
    public float spacingY = 1.5f;
    public float spacingX = 2.0f;
    public float gameDuration = 30f;

    private Cans[,] canGrid;
    private float timeRemaining;
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

        // 射擊偵測
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                Cans can = hit.collider.GetComponent<Cans>();
                if (can != null)
                {
                    can.OnShot();
                }
            }
        }
    }

    void StartGame()
    {
        timeRemaining = gameDuration;
        isGameActive = true;
        SpawnCans();
        uiManager.UpdateCanCounter(totalRows); // 顯示剩餘罐頭數（或根據實際數量）
    }

    void SpawnCans()
    {
        canGrid = new Cans[totalRows, totalCols];
        Vector3 startPos = spawnArea.position;
        int spawnCount = 0;

        for (int row = totalRows - 1; row >= 0; row--) // 從最上面往下生成
        {
            int col = Random.Range(0, totalCols); // 隨機選左中右（0~2）
            float x = startPos.x + (col - 1) * spacingX;
            float y = startPos.y + row * spacingY;
            Vector3 pos = new Vector3(x, y, startPos.z);

            GameObject go = Instantiate(canPrefab, pos, Quaternion.identity);
            Cans can = go.GetComponent<Cans>();
            can.row = row;
            can.col = col;
            can.manager = this;

            canGrid[row, col] = can;
            spawnCount++;
        }

        uiManager.UpdateCanCounter(spawnCount);
    }

    public void HandleCanShot(int row, int col)
    {
        if (!isGameActive) return;

        // 清空整個第 row 行（row = 0 時，代表最下面那行）
        for (int c = 0; c < totalCols; c++)
        {
            if (canGrid[row, c] != null)
            {
                Destroy(canGrid[row, c].gameObject);
                canGrid[row, c] = null;
            }
        }

        // 從上面行往下移動
        for (int r = row + 1; r < totalRows; r++)
        {
            for (int c = 0; c < totalCols; c++)
            {
                Cans upperCan = canGrid[r, c];
                if (upperCan != null)
                {
                    // 移到下一行
                    canGrid[r - 1, c] = upperCan;
                    canGrid[r, c] = null;

                    upperCan.row -= 1;
                    Vector3 pos = upperCan.transform.position;
                    upperCan.transform.position = new Vector3(pos.x, pos.y - spacingY, pos.z);
                }
            }
        }

        // 最上面一行清空（移動後第 14 行沒東西）
        for (int c = 0; c < totalCols; c++)
        {
            canGrid[totalRows - 1, c] = null;
        }

        // 檢查遊戲是否結束
        if (IsAllCansCleared())
        {
            EndGame(true);
        }
        else
        {
            uiManager.UpdateCanCounter(GetRemainingCanCount());
        }
    }

    bool IsAllCansCleared()
    {
        for (int row = 0; row < totalRows; row++)
        {
            for (int col = 0; col < totalCols; col++)
            {
                if (canGrid[row, col] != null)
                    return false;
            }
        }
        return true;
    }

    int GetRemainingCanCount()
    {
        int count = 0;
        foreach (var can in canGrid)
        {
            if (can != null)
                count++;
        }
        return count;
    }

    void EndGame(bool success)
    {
        isGameActive = false;
        uiManager.ShowResult(success);
    }
}
