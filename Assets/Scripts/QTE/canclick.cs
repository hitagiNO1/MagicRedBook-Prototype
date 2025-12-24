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
    public float columnSpacing = 10f;
    public int targetSuccessCount = 15;
    private int currentSuccessCount = 0;
    public bool gameEnded = false;
    private int blackTileGenerated = 0;
    public GameObject startButton;
    private List<Tile> activeTiles = new List<Tile>();
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI resulItem;
    public GameObject BackButton;
    public float columnAngle = 0f;
    public GameFlowManager gameflow;
    public float fallSpeed = 5f;

    [Header("Return Settings")]
    public string mainSceneName;

    // Sprite 設置
    public Sprite[] columnSpecificWhiteSprites;
    public Sprite commonWhiteSprite;
    public float commonSpriteChance = 0.5f;
    public Sprite[] columnSpecificBlackSprites;

    [Header("Item Settings")]
    public ItemData[] itemOptions;

    // ★ 修改 1: 定義一個結構來儲存 "道具+數量"
    struct RewardInfo
    {
        public ItemData item;
        public int amount;
    }

    // ★ 修改 2: 使用 List 來儲存多個選中的獎勵，而不是單個變數
    private List<RewardInfo> selectedRewards = new List<RewardInfo>();

    void Start()
    {
        if (itemOptions.Length == 0)
        {
            Debug.LogWarning("請在 Inspector 設定 itemOptions！");
            return;
        }
        // ... (省略 Sprite 檢查代碼，維持原樣) ...

        // ★ 修改 3: 隨機選取 1 到 3 個不同的道具，數量各為 1 或 2
        GenerateRandomRewards();
    }

    // ★ 新增方法: 專門處理隨機獎勵邏輯
    void GenerateRandomRewards()
    {
        selectedRewards.Clear();

        // 1. 建立一個暫存清單 (為了避免抽到重複的道具)
        List<ItemData> tempPool = new List<ItemData>(itemOptions);

        // 2. 決定要抽幾種道具 (1 ~ 3 種)
        // 注意: 如果池子裡的道具少於 3 種，就最多只抽池子的大小
        int typesCount = Random.Range(1, 4); // 隨機回傳 1, 2, 3
        int finalCount = Mathf.Min(typesCount, tempPool.Count);

        Debug.Log($"本次將抽出 {finalCount} 種道具");

        for (int i = 0; i < finalCount; i++)
        {
            // 從池子隨機選一個
            int randomIndex = Random.Range(0, tempPool.Count);
            ItemData pickedItem = tempPool[randomIndex];

            // 隨機數量 1 或 2
            int randomAmt = Random.Range(1, 3); // 隨機回傳 1, 2

            // 加入獎勵清單
            selectedRewards.Add(new RewardInfo { item = pickedItem, amount = randomAmt });

            // 從暫存池移除，確保下次不會重複抽到同一個道具
            tempPool.RemoveAt(randomIndex);

            Debug.Log($"-- 選中獎勵: {pickedItem.itemName}, 數量: {randomAmt}");
        }
    }

    void Update()
    {
        // ... (Update 內容完全不用動，維持原樣) ...
        if (gameEnded) return;

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                Tile tile = hit.collider.GetComponent<Tile>();
                if (tile != null)
                {
                    tile.OnClicked();
                }
            }
        }

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

    // ... (SpawnRow, OnCorrectTileClicked, OnWrongTileClicked, GameOver 維持原樣) ...

    void SpawnRow()
    {
        // 這裡省略以節省篇幅，內容不需要改動
        if (blackTileGenerated >= targetSuccessCount) return;
        int blackCol = Random.Range(0, 3);
        for (int col = 0; col < 3; col++)
        {
            GameObject prefab = (col == blackCol) ? tileBlackPrefab : tileWhitePrefab;
            GameObject tile = Instantiate(prefab);
            tile.name = $"{((col == blackCol) ? "BlackTile" : "WhiteTile")}_col{col}_row{blackTileGenerated}";
            tile.transform.position = spawnPoint.position + new Vector3((col - 1) * columnSpacing, 0f, 0f);
            Tile tileScript = tile.GetComponent<Tile>();
            // ... 省略中間賦值 ...
            tileScript.isBlack = (col == blackCol);
            tileScript.col = col;
            tileScript.canclick = this;

            SpriteRenderer spriteRenderer = tile.GetComponent<SpriteRenderer>();
            BoxCollider2D collider = tile.GetComponent<BoxCollider2D>();

            // Sprite 邏輯保持不變
            if (tileScript.isBlack)
            {
                if (columnSpecificBlackSprites.Length > col) spriteRenderer.sprite = columnSpecificBlackSprites[col];
            }
            else
            {
                bool useCommonSprite = Random.value < commonSpriteChance;
                if (useCommonSprite) spriteRenderer.sprite = commonWhiteSprite;
                else if (columnSpecificWhiteSprites.Length > col) spriteRenderer.sprite = columnSpecificWhiteSprites[col];
            }

            if (collider != null)
            {
                collider.size = spriteRenderer.sprite.bounds.size;
                collider.offset = Vector2.zero;
            }
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
        foreach (var tile in activeTiles)
        {
            Destroy(tile.gameObject);
        }
        activeTiles.Clear();
        gameflow.ShowResult();
        resultText.text = "失敗";
        resultText.gameObject.SetActive(true);
    }

    void GameClear()
    {
        gameEnded = true;
        CancelInvoke(nameof(SpawnRow));

        // 清理場景上剩餘的方塊
        foreach (var tile in activeTiles)
        {
            Destroy(tile.gameObject);
        }
        activeTiles.Clear();

        if (gameflow != null)
        {
            gameflow.ShowResult();
        }

        // 1. 準備文字
        string finalMessage = "成功！";
        string finalItem = "";

        // 2. 遍歷獎勵清單
        foreach (RewardInfo reward in selectedRewards)
        {
            if (reward.item != null)
            {
                // ★ 修改 B: 使用 "+=" 來串接文字，而不是覆蓋
                finalItem += $"{reward.item.itemName} x{reward.amount}\n";

                // B. 加入背包
                if (InventoryManager.Instance != null)
                {
                    InventoryManager.Instance.AddItem(reward.item, reward.amount);
                }
            }
        }

        // 3. 檢查背包系統
        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("【注意】找不到 InventoryManager，物品僅顯示於文字，未實際加入背包。");
        }

        // 4. 顯示 UI
        if (resulItem != null)
        {
            resulItem.text = finalItem;
            resulItem.gameObject.SetActive(true);
        }

        if (resultText != null)
        {
            resultText.text = finalMessage;
            resultText.gameObject.SetActive(true);
        }

        Debug.Log("遊戲結算完成，訊息已更新。");
    }

    public void StartGame()
    {
        if (gameEnded) return;
        StartCoroutine(SpawnRowCoroutine());
        startButton.SetActive(false);
    }

    IEnumerator SpawnRowCoroutine()
    {
        float delay = 0.6f;
        while (!gameEnded && blackTileGenerated < targetSuccessCount)
        {
            SpawnRow();
            yield return new WaitForSeconds(delay);
        }
    }

    public void EndMiniGame()
    {
        StartCoroutine(ReturnToMainScene());
    }

    private IEnumerator ReturnToMainScene()
    {
        SceneSwapper.RestoreMainScene(mainSceneName);
        yield return null;
        yield return SceneManager.UnloadSceneAsync(gameObject.scene);
    }

    public void RestartGame()
    {
        gameEnded = false;
        currentSuccessCount = 0;
        blackTileGenerated = 0;
        resultText.gameObject.SetActive(false);
        startButton.SetActive(true);
        UpdateScoreText();
        foreach (var tile in activeTiles)
        {
            Destroy(tile.gameObject);
        }
        activeTiles.Clear();

        // ★ 修改 5: 重玩時，重新隨機產生新的獎勵組合 (看你想不想重玩時換獎勵)
        // 如果希望獎勵固定，這行可以拿掉
        GenerateRandomRewards();
    }

    void UpdateScoreText()
    {
        scoreText.text = $"{currentSuccessCount}/{targetSuccessCount}";
    }
}