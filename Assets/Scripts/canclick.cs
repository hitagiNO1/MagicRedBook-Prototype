using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class canclick : MonoBehaviour
{
    public GameObject tileWhitePrefab;
    public GameObject tileBlackPrefab;
    public Transform spawnPoint;
    public float fallSpeed = 2f;
    public int maxTiles = 15;
    public int targetSuccessCount = 15;
    private int currentSuccessCount = 0;
    private bool gameEnded = false;
    public int blackTileGenerated = 0;


    private List<Tile> activeTiles = new List<Tile>();

    void Start()
    {
        InvokeRepeating(nameof(SpawnRow), 0.5f, 0.6f); // 每 0.6 秒生成一排

    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("滑鼠點擊偵測到");

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                Debug.Log("命中：" + hit.collider.name);

                Tile tile = hit.collider.GetComponent<Tile>();
                if (tile != null)
                {
                    Debug.Log("點到 tile，是否黑色: " + tile.isBlack);
                    tile.OnClicked();
                }
            }
            else
            {
                Debug.Log("Raycast 沒打中任何物件");
            }
        }
        // 所有 tile 向下移動
        if (gameEnded) return; // 停止一切邏輯

        foreach (var tile in activeTiles)
        {
            tile.transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);
        }

        // 判斷最底 tile 是否掉到底
        if (activeTiles.Count > 0)
        {
            var bottomTile = activeTiles[0];
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
        Debug.Log($"正確點擊：{currentSuccessCount} / {targetSuccessCount}");

        if (currentSuccessCount >= targetSuccessCount)
        {
            GameClear();
        }
    }

    public void OnWrongTileClicked(Tile tile)
    {
        if (gameEnded) return;

        GameOver("你點到了白塊！");
    }

    void GameOver(string reason)
    {
        if (gameEnded) return;

        gameEnded = true;

        CancelInvoke(nameof(SpawnRow)); // 停止繼續產生 tile
        Debug.Log(" 遊戲結束：" + reason);
        // TODO: 顯示失敗畫面
    }

    void GameClear()
    {
        gameEnded = true;
        CancelInvoke(nameof(SpawnRow));
        Debug.Log(" 遊戲成功！你點滿 15 個黑 tile！");
        // TODO: 顯示 UI：勝利畫面 / 回主選單
    }

}
