using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class canclick : MonoBehaviour
{
    public GameObject tileWhitePrefab;
    public GameObject tileBlackPrefab;
    public Transform spawnPoint;
    public float fallSpeed = 2f;
    public float columnSpacing = 10f; // 設定軌道間距
    public int targetSuccessCount = 15;
    private int currentSuccessCount = 0;
    public bool gameEnded = false;
    private int blackTileGenerated = 0;
    public GameObject startButton;
    private List<Tile> activeTiles = new List<Tile>();
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI resultText;
    public GameObject restartButton;
    public GameObject BackButton;
    public float columnAngle = 0f; // 3條軌道左右的夾角，可從 Inspector 調整
    public GameFlowManager gameflow;
    public ItemData[] itemOptions; // 在 Inspector 拖入 4 個 ItemData 資料
    private ItemData selectedItem;
    public int randomAmount;

    void Start()
    {
        if (itemOptions.Length == 0)
        {
            Debug.LogWarning("請在 Inspector 設定 itemOptions！");
            return;
        }

        int randomIndex = Random.Range(0, itemOptions.Length); // 取得 0~3 間的整數
        selectedItem = itemOptions[randomIndex];
        randomAmount = Random.Range(1, 4);


        Debug.Log($"選中：{selectedItem.itemName}，數量：{randomAmount}");
    }

    void Update()
    {
        if (gameEnded) return;

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Tile tile = hit.collider.GetComponent<Tile>();
                if (tile != null)
                {
                    tile.OnClicked();
                }
            }
        }

        

        // 判斷最底層的 tile 是否掉到底
        if (activeTiles.Count > 0)
        {
            Tile bottomTile = activeTiles[0];
            if (bottomTile.transform.position.y < -5f)
            {
                if (bottomTile.isBlack)
                {
                    GameOver("漏點黑 tile！");
                }
                else
                {
                    activeTiles.RemoveAt(0);
                    Destroy(bottomTile.gameObject);
                }
            }
        }



       
    }

    void SpawnRow()
    {
        if (blackTileGenerated >= targetSuccessCount) return;

        int blackCol = Random.Range(0, 3);

        for (int col = 0; col < 3; col++)
        {
            GameObject prefab = (col == blackCol) ? tileBlackPrefab : tileWhitePrefab;
            GameObject tile = Instantiate(prefab);

            tile.transform.position = spawnPoint.position + new Vector3((col - 1) * columnSpacing, 0f, 0f);

            Tile tileScript = tile.GetComponent<Tile>();
            tileScript.isBlack = (col == blackCol);
            tileScript.col = col;
            tileScript.canclick = this;

            activeTiles.Add(tileScript);
        }

        blackTileGenerated++;
    }

    public void OnCorrectTileClicked(Tile tile)
    {
        if (gameEnded) return;

        activeTiles.Remove(tile);
        Destroy(tile.gameObject);

        currentSuccessCount++;
        UpdateScoreText();

        Debug.Log($"正確點擊：{currentSuccessCount} / {targetSuccessCount}");

        if (currentSuccessCount >= targetSuccessCount)
        {
            GameClear();
        }
    }

    public void OnWrongTileClicked(Tile tile)
    {
        if (gameEnded) return;

        GameOver("你點錯了！");
    }

    public void GameOver(string reason)
    {
        if (gameEnded) return;

        gameEnded = true;
        CancelInvoke(nameof(SpawnRow));

        // 刪除所有現有 tile
        foreach (var tile in activeTiles)
        {
            Destroy(tile.gameObject);
        }
        activeTiles.Clear();

        // 顯示結果文字
        gameflow.ShowResult();
        resultText.text = $"失敗！\n{reason}";
        resultText.gameObject.SetActive(true);
        restartButton.SetActive(true);
        Debug.Log("遊戲結束：" + reason);
    }

    void GameClear()
    {
        gameEnded = true;
        CancelInvoke(nameof(SpawnRow));

        foreach (var tile in activeTiles)
        {
            Destroy(tile.gameObject);
        }
        activeTiles.Clear();
        gameflow.ShowResult();
        resultText.text = "成功！\n你點滿了 15 個黑塊！";
        resultText.gameObject.SetActive(true);
        restartButton.SetActive(true);
        Debug.Log("遊戲成功！");
        selectedItem.amount += randomAmount;
    }
    public void StartGame()
    {
        if (gameEnded) return;

        StartCoroutine(SpawnRowCoroutine());

        // 隱藏按鈕
        startButton.SetActive(false);
    }

    IEnumerator SpawnRowCoroutine()
    {
        float delay = 1f; // 固定間隔

        while (!gameEnded && blackTileGenerated < targetSuccessCount)
        {
            SpawnRow();
            yield return new WaitForSeconds(delay);
        }
    }

    public void BackGame()
    {
        SceneManager.LoadScene("Prologue");
    }

    public void RestartGame()
    {
        // 重設變數
        gameEnded = false;
        currentSuccessCount = 0;
        blackTileGenerated = 0;
        resultText.gameObject.SetActive(false);
        restartButton.SetActive(false);
        startButton.SetActive(true);
        UpdateScoreText();

        // 清除舊的 tile
        foreach (var tile in activeTiles)
        {
            Destroy(tile.gameObject);
        }
        activeTiles.Clear();
    }

    void UpdateScoreText()
    {
        scoreText.text = $"{currentSuccessCount} / {targetSuccessCount}";
    }
}
