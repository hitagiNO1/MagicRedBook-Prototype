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

    public int targetSuccessCount = 15;
    private int currentSuccessCount = 0;
    public bool gameEnded = false;
    private int blackTileGenerated = 0;
    public GameObject startButton;
    private List<Tile> activeTiles = new List<Tile>();
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI resultText;
    public GameObject restartButton;

    void Start()
    {
       
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

        // Tile 移動
        foreach (var tile in activeTiles)
        {
            tile.transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);
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

            tile.transform.position = spawnPoint.position + new Vector3((col - 1) * 2f, 0f, 0f);

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

        resultText.text = "成功！\n你點滿了 15 個黑塊！";
        resultText.gameObject.SetActive(true);
        restartButton.SetActive(true);
        Debug.Log("遊戲成功！");
    }
    public void StartGame()
    {
        if (gameEnded) return;

        InvokeRepeating(nameof(SpawnRow), 0.5f, 0.6f);

        // 隱藏按鈕
        startButton.SetActive(false);
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void UpdateScoreText()
    {
        scoreText.text = $"{currentSuccessCount} / {targetSuccessCount}";
    }
}
